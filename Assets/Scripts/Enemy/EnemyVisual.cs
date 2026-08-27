using UnityEngine;

/// <summary>
/// 적의 "살아 있어 보이는" 연출만 담당한다. 전투/이동 로직은 EnemyBase 소관.
///
/// 스프라이트가 전부 1프레임 정지 이미지라 그냥 두면 미끄러지듯 이동한다.
/// 프레임 애니메이션을 새로 뽑는 대신 셰이더로 흉내 낸다.
///
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

    /// <summary>EnemyBase.OnInitialized 에서 호출. 풀에서 재사용될 때마다 다시 불린다.</summary>
    public void Setup(SpriteRenderer sr, float moveSpeed)
    {
        _sr = sr != null ? sr : GetComponent<SpriteRenderer>();
        if (_rb == null)  _rb  = GetComponent<Rigidbody2D>();
        if (_mpb == null) _mpb = new MaterialPropertyBlock();
        if (_sr == null) return;

        _flashTimer  = 0f;
        _facingRight = true;
        _sr.flipX    = false;

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
        UpdateFlash();
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
