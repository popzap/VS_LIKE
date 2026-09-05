using UnityEngine;

/// <summary>
/// 플레이어 이동 연출. 걷기 프레임 + 셰이더 스쿼시/바운스 + 이동 방향으로 기울이기.
///
/// 예전에는 <see cref="PlayerController"/> 의 <c>sr.flipX</c> 한 줄이 전부라
/// 좌우로 갈 때 그림이 뒤집히기만 하고 위아래로 갈 때는 아무 일도 없었다.
///
/// 색(피격 붉은 물듦/깜빡임)은 여전히 PlayerController 가 <c>sr.color</c> 로 다룬다.
/// 이쪽은 MaterialPropertyBlock 만 건드리므로 서로 덮어쓰지 않는다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerVisual : MonoBehaviour
{
    [Header("걷기 프레임")]
    [Tooltip("초당 넘길 프레임 수. 실제 이동 속도에 비례해 빨라진다.")]
    [SerializeField] private float framesPerSecond = 12f;
    [Tooltip("이 속도 미만이면 멈춘 것으로 보고 기본 프레임으로 돌아간다.")]
    [SerializeField] private float moveDeadzone = 0.15f;

    [Header("셰이더 연출")]
    [Tooltip("스쿼시/바운스 주기. 걸음 속도와 비슷해야 발이 겉돌지 않는다.")]
    [SerializeField] private float bounceSpeed = 13f;
    [Tooltip("이동 방향으로 기울이는 정도(전단). 0 이면 끔.")]
    [SerializeField] private float leanAmount = 0.10f;
    [Tooltip("기울기가 목표값까지 따라붙는 속도. 낮으면 흐느적거린다.")]
    [SerializeField] private float leanResponse = 12f;

    private SpriteRenderer        _sr;
    private Rigidbody2D           _rb;
    private MaterialPropertyBlock _mpb;

    private Sprite[] _frames;
    private Sprite   _idleFrame;
    private float    _frameTimer;
    private int      _frameIndex;
    private float    _lean;

    private static readonly int AnimSpeedId = Shader.PropertyToID("_AnimSpeed");
    private static readonly int AnimPhaseId = Shader.PropertyToID("_AnimPhase");
    private static readonly int LeanAmtId   = Shader.PropertyToID("_LeanAmt");
    private static readonly int LeanPivotId = Shader.PropertyToID("_LeanPivotY");

    // ── 버프 오라 (D67 · 사용자 요구 8) ──────────────────────────
    private static readonly int OutlineColorId   = Shader.PropertyToID("_OutlineColor");
    private static readonly int OutlineWidthId   = Shader.PropertyToID("_OutlineWidth");
    private static readonly int SpriteRectId     = Shader.PropertyToID("_SpriteRect");
    private static readonly int OutlineTexSizeId = Shader.PropertyToID("_OutlineTexSize");

    [Header("버프 오라 — 무적 / 공속 (D67)")]
    [Tooltip("무적 오라 색. 흰-하늘색 계열이라 '못 맞는다' 로 읽힌다.")]
    [SerializeField] private Color auraInvincible = new(0.62f, 0.88f, 1f, 1f);

    [Tooltip("공속 오라 색. 주황 계열이라 '빨라졌다' 로 읽힌다.")]
    [SerializeField] private Color auraHaste = new(1f, 0.68f, 0.22f, 1f);

    [Tooltip("이속 오라 색 (D68). 연두 계열 — 공속(주황)과 한눈에 갈린다.")]
    [SerializeField] private Color auraSwift = new(0.55f, 1f, 0.45f, 1f);

    [Tooltip("오라 굵기(텍셀). 셰이더가 _OutlineWidth / _OutlineTexSize 의 uv 거리로 쓴다. "
           + "🔴 14 는 너무 굵다 — 검처럼 얇은 부분이 양쪽 테두리에 끼여 원래 색이 안 보인다(B1 계열). "
           + "실제 플레이 배율(ortho 6)에서 캡처해 고른 값이 9 다.")]
    [SerializeField] private float auraWidth = 9f;

    [Tooltip("남은 시간이 이 값 아래로 내려가면 점등한다(초).")]
    [SerializeField] private float auraBlinkBelow = 1.5f;

    [Tooltip("점등 속도(초당 깜빡임 수).")]
    [SerializeField] private float auraBlinkPerSecond = 6f;

    /// <summary>_SpriteRect 를 마지막으로 넘긴 스프라이트. 바뀔 때만 다시 넘긴다.</summary>
    private Sprite _rectSprite;

    private PlayerStats _stats;

    private void Awake()
    {
        _sr  = GetComponent<SpriteRenderer>();
        _rb  = GetComponent<Rigidbody2D>();
        _mpb = new MaterialPropertyBlock();
        _idleFrame = _sr.sprite;

        // 🔴 GameManager 를 거치지 않는 같은 오브젝트의 컴포넌트라 Awake 에서 잡아도 된다.
        //    (I-8 은 GameManager.Instance.XxxMgr 얘기다 — 그건 Start 에서 채워진다)
        _stats = GetComponent<PlayerStats>();
    }

    /// <summary>직업이 정해질 때 <see cref="PlayerStats.ApplyClass"/> 가 호출한다.</summary>
    public void SetWalkFrames(Sprite[] frames)
    {
        // 시트가 없는 직업은 정지 그림 한 장으로 버틴다. 바운스/기울기는 그대로 동작한다.
        if (frames == null || frames.Length == 0) { _frames = null; return; }

        _frames     = frames;
        _idleFrame  = frames[0];
        _frameIndex = 0;
        _frameTimer = 0f;
        _sr.sprite  = _idleFrame;
    }

    private void LateUpdate()
    {
        Vector2 vel   = _rb != null ? _rb.linearVelocity : Vector2.zero;
        float   speed = vel.magnitude;
        bool    moving = speed > moveDeadzone;

        StepFrames(moving, speed);
        ApplySpriteRect(_sr.sprite);   // 🔴 PushShader 보다 먼저 — 둘 다 같은 _mpb 를 쓴다
        PushShader(moving, vel);
    }

    private void StepFrames(bool moving, float speed)
    {
        if (_frames == null) return;

        if (!moving)
        {
            // 멈추면 항상 같은 자세로 선다. 마지막 프레임에서 얼어붙으면 어색하다.
            _frameIndex = 0;
            _frameTimer = 0f;
            _sr.sprite  = _idleFrame;
            return;
        }

        // 빠를수록 발을 빨리 놀린다. 기준 속도 4(= Economy.csv 의 기본 MoveSpeed).
        _frameTimer += Time.deltaTime * framesPerSecond * Mathf.Clamp(speed / 4f, 0.5f, 2f);
        while (_frameTimer >= 1f)
        {
            _frameTimer -= 1f;
            _frameIndex = (_frameIndex + 1) % _frames.Length;
        }
        _sr.sprite = _frames[_frameIndex];
    }

    private void PushShader(bool moving, Vector2 vel)
    {
        // 멈춰 있을 땐 바운스를 끈다(_AnimSpeed 0 이면 셰이더가 버텍스를 건드리지 않는다).
        float target = 0f;
        if (moving && leanAmount > 0f)
        {
            // 🔴 예전에는 여기에 `* (_sr.flipX ? -1f : 1f)` 가 붙어 있었다. **틀렸다** (D65).
            //    flipX 는 메시 정점을 뒤집을 뿐 오브젝트 공간의 축은 그대로라,
            //    셰이더의 전단은 flipX 와 무관하게 항상 화면 오른쪽으로 민다.
            //    그래서 왼쪽 이동에서 부호가 두 번 뒤집혀(-1 × -1) **가는 쪽의 반대로 기울었다.**
            //    실측(D65): lean +0.4 → flipX 무관하게 +39.10 / +38.58 px, lean −0.4 → −38.57 / −39.10 px.
            float dir = Mathf.Abs(vel.x) > moveDeadzone ? Mathf.Sign(vel.x) : 0f;
            target = leanAmount * dir;
        }
        _lean = Mathf.MoveTowards(_lean, target, leanResponse * leanAmount * Time.deltaTime);

        _sr.GetPropertyBlock(_mpb);
        _mpb.SetFloat(AnimSpeedId, moving ? bounceSpeed : 0f);
        _mpb.SetFloat(AnimPhaseId, 0f);
        _mpb.SetFloat(LeanAmtId,   _lean);

        // 🔴 <b>D65 는 부호만 고쳤고 그것으로는 부족했다</b> (D79 · 사용자가 두 번 지적했다).
        //    전단의 기준선을 안 주면 피벗(= 스프라이트 한가운데)이 기준이 되어
        //    위가 오른쪽으로 밀리는 만큼 **아래(다리)가 왼쪽으로 밀린다.**
        //    화면에서 가장 크게 움직이는 건 다리라, 부호가 맞아도 **몸이 반대로 가 보인다.**
        //    밑변을 넣어 발을 고정한다 — 프레임마다 스프라이트가 바뀌므로 매번 읽는다.
        if (_sr.sprite != null) _mpb.SetFloat(LeanPivotId, _sr.sprite.bounds.min.y);
        PushAura(_mpb);
        _sr.SetPropertyBlock(_mpb);
    }

    /// <summary>
    /// 버프 오라 (D67 · 사용자 요구 8: *"무적·공속을 먹었을 때 겉모습 변화가 없어 알기 어려움.
    /// 오라색 + 제한시간 되면 점등되는 식으로"*).
    ///
    /// <para>🔑 <b>새 애셋도 새 오브젝트도 없다.</b> 이 스프라이트가 이미 쓰는
    /// <c>VS_LIKE/SpriteOutline</c> 셰이더에 외곽선 기능이 들어 있다 — 엘리트/보스 표시에 쓰던 것을
    /// 그대로 빌린다. 자식 오브젝트를 붙이면 정렬·풀링·바운스를 다 따로 맞춰야 한다.</para>
    ///
    /// <para>🔴 <b>피격 무적은 안 본다</b>(<c>IsInvincible</c> 대신 <c>IsBuffInvincible</c>).
    /// 예전에는 타이머가 하나라 이 구분이 불가능했고, 그래서 <c>D67</c> 에서 갈랐다 —
    /// 합쳐 두면 <b>맞을 때마다 오라가 번쩍여서</b> 아이템 신호가 아니라 피격 신호가 된다.</para>
    ///
    /// <para>둘 다 켜져 있으면 <b>무적을 먼저</b> 보여 준다 — 지금 죽지 않는다는 사실이
    /// 공격이 빠르다는 사실보다 중요하다.</para>
    /// </summary>
    private void PushAura(MaterialPropertyBlock mpb)
    {
        if (_stats == null) { mpb.SetFloat(OutlineWidthId, 0f); return; }

        bool inv = _stats.IsBuffInvincible;
        bool has = _stats.IsHasted;
        bool swi = _stats.IsSwift;
        if (!inv && !has && !swi) { mpb.SetFloat(OutlineWidthId, 0f); return; }

        // 우선순위: 무적 → 공속 → 이속. 지금 죽지 않는다는 사실이 제일 중요하고,
        // 그다음이 공격, 그다음이 이동이다.
        Color c; float left;
        if      (inv) { c = auraInvincible; left = _stats.BuffInvincibleRemaining; }
        else if (has) { c = auraHaste;      left = _stats.HasteRemaining; }
        else          { c = auraSwift;      left = _stats.SwiftRemaining; }

        // 🔑 굵기가 아니라 알파를 점등한다. 굵기를 흔들면 실루엣이 커졌다 작아져
        //    캐릭터가 물리적으로 변한 것처럼 보인다.
        if (left <= auraBlinkBelow && auraBlinkPerSecond > 0f)
        {
            float s = Mathf.Sin(Time.unscaledTime * Mathf.PI * auraBlinkPerSecond);
            c.a *= 0.35f + 0.65f * Mathf.Abs(s);
        }

        mpb.SetColor(OutlineColorId, c);
        mpb.SetFloat(OutlineWidthId, auraWidth);
    }

    /// <summary>
    /// 지금 그리는 프레임이 <b>텍스처의 어느 사각형인지</b>와 <b>그 텍스처가 몇 픽셀인지</b>를
    /// 넘긴다 (<c>B1</c> · <see cref="EnemyVisual"/> 와 같은 이유).
    ///
    /// <para>🔴 <b>오라를 켜는 순간 이게 필수가 됐다.</b> 지금까지 플레이어는 외곽선을 안 써서
    /// 없어도 됐지만, 걷기 시트는 4×4 짜리라 이 사각형이 없으면 외곽선이
    /// <b>옆 걷기 프레임의 알파를 빨아들인다</b> — 그게 <c>B1</c> 이었다.</para>
    ///
    /// <para><c>_OutlineTexSize</c> 도 같이 넘긴다. 굵기는 <c>_OutlineWidth / 이 값</c> 의 uv 거리라
    /// 셰이더 기본값 512 로 두면 1024 시트에서 <b>2배</b>가 된다.</para>
    /// </summary>
    private void ApplySpriteRect(Sprite s)
    {
        if (s == _rectSprite || _sr == null) return;
        _rectSprite = s;

        Vector4 r       = new Vector4(0f, 0f, 1f, 1f);
        float   texSize = 512f;
        if (s != null && s.texture != null)
        {
            Rect  tr = s.textureRect;
            float tw = s.texture.width, th = s.texture.height;
            if (tw > 0f && th > 0f)
            {
                r = new Vector4(tr.xMin / tw, tr.yMin / th, tr.xMax / tw, tr.yMax / th);
                texSize = Mathf.Max(tw, th);
            }
        }

        _sr.GetPropertyBlock(_mpb);
        _mpb.SetVector(SpriteRectId, r);
        _mpb.SetFloat(OutlineTexSizeId, texSize);
        _sr.SetPropertyBlock(_mpb);
    }
}
