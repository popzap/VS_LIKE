using UnityEngine;

/// <summary>
/// 적의 "살아 있어 보이는" 연출만 담당한다. 전투/이동 로직은 EnemyBase 소관.
///
/// 원래는 스프라이트가 전부 1프레임 정지 이미지라 셰이더 바운스로만 흉내 냈다.
/// I-58 에서 <c>EnemyData.WalkFrames</c> 가 생겨 진짜 프레임 애니메이션이 붙는다.
/// 시트가 없는 적은 예전처럼 정지 그림 한 장으로 버틴다 — 바운스는 양쪽 다 걸린다.
///
/// - 걷기 프레임 : 시트가 있을 때만. 개체마다 시작 프레임을 어긋나게 줘서 무리가
///                한 몸처럼 발을 맞추지 않게 한다 (바운스 위상과 같은 이유).
/// - 걷기 바운스 : VS_LIKE/SpriteOutline 의 버텍스 애니메이션. 개체마다 위상을 어긋나게
///                줘서 무리가 한 몸처럼 출렁이지 않게 한다.
/// - 좌우 방향   : SpriteRenderer.flipX. 원본 스프라이트는 "오른쪽을 본다"가 규약이다.
/// - 피격 플래시 : _FlashAmount 를 1로 올린 뒤 감쇠시킨다.
///
/// 스케일을 건드리지 않는 이유: 루트를 늘이면 CapsuleCollider2D 까지 늘어나 판정이 변한다.
/// </summary>
[DisallowMultipleComponent]
public class EnemyVisual : MonoBehaviour
{
    [Header("걷기 프레임 (WalkFrames 가 있을 때만)")]
    [Tooltip("초당 넘길 프레임 수. 이 적의 기본 이동 속도를 1배로 보고 실제 속도에 비례해 조절된다")]
    [SerializeField] private float framesPerSecond = 10f;
    [Tooltip("이 속도 미만이면 멈춘 것으로 보고 기본 프레임으로 돌아간다")]
    [SerializeField] private float moveDeadzone = 0.15f;

    [Header("걷기 바운스")]
    [Tooltip("초당 사이클. 값이 클수록 종종거린다")]
    [SerializeField] private float bounceSpeed = 9f;
    [Tooltip("이동 속도에 비례해 바운스를 빠르게 할지")]
    [SerializeField] private bool  scaleWithSpeed = true;

    [Header("좌우 방향")]
    [Tooltip("이 속도 미만이면 방향을 바꾸지 않는다. 경계에서 파르르 떠는 것 방지")]
    [SerializeField] private float flipDeadzone = 0.15f;

    [Header("피격 플래시")]
    [SerializeField] private float flashDuration = 0.12f;

    private static readonly int AnimSpeedId   = Shader.PropertyToID("_AnimSpeed");
    private static readonly int AnimPhaseId   = Shader.PropertyToID("_AnimPhase");
    private static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");

    private SpriteRenderer       _sr;
    private Rigidbody2D          _rb;
    private MaterialPropertyBlock _mpb;

    private float _flashTimer;
    private bool  _facingRight = true;

    private Sprite[] _frames;
    private Sprite   _idleFrame;
    private float    _frameTimer;
    private int      _frameIndex;
    private float    _speedRef = 1f;

    /// <summary>EnemyBase.OnInitialized 에서 호출. 풀에서 재사용될 때마다 다시 불린다.</summary>
    /// <param name="moveSpeed">등급 배율까지 곱해진 실제 이동 속도. 바운스와 걸음 속도의 기준값이 된다.</param>
    /// <param name="frames">걷기 시트. null 이거나 비면 정지 그림 한 장으로 동작한다.</param>
    public void Setup(SpriteRenderer sr, float moveSpeed, Sprite[] frames = null)
    {
        _sr = sr != null ? sr : GetComponent<SpriteRenderer>();
        if (_rb == null)  _rb  = GetComponent<Rigidbody2D>();
        if (_mpb == null) _mpb = new MaterialPropertyBlock();
        if (_sr == null) return;

        _flashTimer  = 0f;
        _facingRight = true;
        _sr.flipX    = false;

        // ⚠️ 풀 재사용 대비. 이전 생애가 시트를 쓰던 적이었다면 반드시 지워야 한다 —
        //    안 그러면 갓 스폰된 슬라임이 늑대 프레임으로 걷는다.
        _speedRef = Mathf.Max(0.1f, moveSpeed);
        if (frames == null || frames.Length == 0)
        {
            _frames = null;
        }
        else
        {
            _frames    = frames;
            _idleFrame = frames[0];
            // 무리가 발을 맞추지 않게 시작 프레임을 개체마다 어긋나게 준다.
            _frameIndex = Random.Range(0, frames.Length);
            _frameTimer = 0f;
            _sr.sprite  = frames[_frameIndex];
        }

        float speed = bounceSpeed * (scaleWithSpeed ? Mathf.Clamp(moveSpeed, 0.5f, 3f) : 1f);

        _sr.GetPropertyBlock(_mpb);
        _mpb.SetFloat(AnimSpeedId, speed);
        // 개체마다 다른 위상. 같은 프레임에 스폰된 무리가 한 몸처럼 움직이는 걸 막는다.
        _mpb.SetFloat(AnimPhaseId, Random.Range(0f, Mathf.PI * 2f));
        _mpb.SetFloat(FlashAmountId, 0f);
        _sr.SetPropertyBlock(_mpb);
    }

    /// <summary>피격 시 EnemyBase 가 호출.</summary>
    public void Flash()
    {
        _flashTimer = flashDuration;
    }

    private void LateUpdate()
    {
        if (_sr == null) return;

        UpdateFacing();
        StepFrames();
        UpdateFlash();
    }

    private void StepFrames()
    {
        if (_frames == null || _rb == null) return;

        float speed = _rb.linearVelocity.magnitude;
        if (speed <= moveDeadzone)
        {
            // 멈추면 항상 같은 자세로 선다. Charger 의 예고/경직이 여기 걸린다 —
            // 마지막 프레임에서 얼어붙으면 "멈췄다"가 아니라 "끊겼다"로 보인다.
            _frameIndex = 0;
            _frameTimer = 0f;
            _sr.sprite  = _idleFrame;
            return;
        }

        // 자기 기본 속도를 1배로 본다. 느린 오우거는 느리게, 돌진 중인 늑대는 빠르게 걷는다.
        _frameTimer += Time.deltaTime * framesPerSecond * Mathf.Clamp(speed / _speedRef, 0.5f, 2.5f);
        while (_frameTimer >= 1f)
        {
            _frameTimer -= 1f;
            _frameIndex = (_frameIndex + 1) % _frames.Length;
        }
        _sr.sprite = _frames[_frameIndex];
    }

    private void UpdateFacing()
    {
        if (_rb == null) return;

        float vx = _rb.linearVelocity.x;
        if (Mathf.Abs(vx) < flipDeadzone) return;

        bool right = vx > 0f;
        if (right == _facingRight) return;

        _facingRight = right;
        _sr.flipX    = !right;   // 원본이 오른쪽을 보므로 왼쪽으로 갈 때만 뒤집는다
    }

    private void UpdateFlash()
    {
        if (_flashTimer <= 0f) return;

        _flashTimer -= Time.deltaTime;
        float amount = Mathf.Clamp01(_flashTimer / flashDuration);

        _sr.GetPropertyBlock(_mpb);
        _mpb.SetFloat(FlashAmountId, amount);
        _sr.SetPropertyBlock(_mpb);
    }
}
