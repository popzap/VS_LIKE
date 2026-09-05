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
    private static readonly int SpriteRectId  = Shader.PropertyToID("_SpriteRect");
    private static readonly int OutlineTexSizeId = Shader.PropertyToID("_OutlineTexSize");

    private SpriteRenderer       _sr;
    private Rigidbody2D          _rb;
    private MaterialPropertyBlock _mpb;

    /// <summary>
    /// <b>쓸 때 만든다</b> (B14 · D84).
    ///
    /// <para>🔴 예전에는 <see cref="Setup"/> 에서만 만들었고, 쓰는 곳은
    /// <c>if (_sr == null) return;</c> <b>하나만</b> 봤다. 그런데 이 둘은 <b>같이 사라지지 않는다</b> —
    /// <b>플레이 중 스크립트가 다시 컴파일되면</b>(에디터 기본값이 "Recompile And Continue Playing")
    /// 도메인 리로드가 돌고, 그때 <c>_sr</c> 은 <b>UnityEngine.Object 참조라 복원되는데</b>
    /// <c>_mpb</c> 는 <b>순수 C# 객체라 null 이 된다.</b></para>
    ///
    /// <para>⇒ 가드를 통과한 채 <c>GetPropertyBlock(null)</c> 이 불려
    /// <c>ArgumentNullException: dest</c> 가 <b>매 프레임, 적마다</b> 터졌다
    /// (사용자 플레이 로그에서 8초에 47건).</para>
    ///
    /// <para>🔑 가드를 하나 더 다는 대신 <b>지연 생성</b>으로 바꿨다 —
    /// 리로드 뒤에도 <b>스스로 낫는다</b>. <see cref="EnemyBase"/> 는 이미 이 방식이었다.</para>
    /// </summary>
    private MaterialPropertyBlock Mpb
    {
        get
        {
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            return _mpb;
        }
    }

    private float _flashTimer;
    private bool  _facingRight = true;

    private Sprite[] _frames;
    private Sprite   _idleFrame;
    private Sprite   _rectSprite;   // _SpriteRect 를 마지막으로 넘긴 스프라이트
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
        if (_sr == null) return;   // _mpb 는 Mpb 프로퍼티가 알아서 만든다 (B14)

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

        _sr.GetPropertyBlock(Mpb);
        Mpb.SetFloat(AnimSpeedId, speed);
        // 개체마다 다른 위상. 같은 프레임에 스폰된 무리가 한 몸처럼 움직이는 걸 막는다.
        Mpb.SetFloat(AnimPhaseId, Random.Range(0f, Mathf.PI * 2f));
        Mpb.SetFloat(FlashAmountId, 0f);
        _sr.SetPropertyBlock(Mpb);

        // ⚠️ 풀 재사용 대비로 **항상** 다시 넘긴다. 이전 생애가 시트를 쓰던 적이었다면
        //    _SpriteRect 에 남의 칸이 남아 외곽선이 엉뚱하게 잘린다.
        _rectSprite = null;
        ApplySpriteRect(_sr.sprite);
    }

    /// <summary>
    /// 지금 그리는 프레임이 <b>텍스처의 어느 사각형인지</b>와 <b>그 텍스처가 몇 픽셀인지</b>를
    /// 셰이더에 넘긴다 (B1). 둘 다 외곽선이 제자리에 그려지기 위한 값이다.
    ///
    /// <para><c>_SpriteRect</c> — 외곽선은 자기 uv 주변을 훑는데, 시트에서는 그 주변이
    /// <b>옆 걷기 프레임</b>이다. 이 사각형이 없으면 남의 알파를 빨아들인다.</para>
    ///
    /// <para><c>_OutlineTexSize</c> — 선 굵기는 <c>_OutlineWidth / 이 값</c> 의 uv 거리다.
    /// 셰이더 기본값 512 로 두면 1024 시트에서 굵기가 <b>2배</b>가 되고, 프레임이
    /// 130~230텍셀뿐이라 실루엣이 통째로 덮인다. 실제 텍스처 크기를 넘겨야 뜻이 맞는다.</para>
    ///
    /// <para>스프라이트가 바뀔 때만 넘긴다 — <c>SetPropertyBlock</c> 은 매 프레임 부를 만큼
    /// 싸지 않고, 프레임은 초당 10장 남짓만 바뀐다.</para>
    /// </summary>
    private void ApplySpriteRect(Sprite s)
    {
        if (s == _rectSprite) return;
        _rectSprite = s;
        if (_sr == null) return;

        Vector4 r       = new Vector4(0f, 0f, 1f, 1f);
        float   texSize = 512f;
        if (s != null && s.texture != null)
        {
            Rect  tr = s.textureRect;
            float tw = s.texture.width;
            float th = s.texture.height;
            if (tw > 0f && th > 0f)
            {
                r = new Vector4(tr.xMin / tw, tr.yMin / th, tr.xMax / tw, tr.yMax / th);
                // uv 거리는 가로세로 같은 값을 쓰므로 기준도 하나여야 한다.
                // 정사각 텍스처가 규약이라 실제로는 tw == th 다.
                texSize = Mathf.Max(tw, th);
            }
        }

        _sr.GetPropertyBlock(Mpb);
        Mpb.SetVector(SpriteRectId, r);
        Mpb.SetFloat(OutlineTexSizeId, texSize);
        _sr.SetPropertyBlock(Mpb);
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
            ApplySpriteRect(_idleFrame);
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
        ApplySpriteRect(_frames[_frameIndex]);
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

        _sr.GetPropertyBlock(Mpb);
        Mpb.SetFloat(FlashAmountId, amount);
        _sr.SetPropertyBlock(Mpb);
    }
}
