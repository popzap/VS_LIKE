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

    private void Awake()
    {
        _sr  = GetComponent<SpriteRenderer>();
        _rb  = GetComponent<Rigidbody2D>();
        _mpb = new MaterialPropertyBlock();
        _idleFrame = _sr.sprite;
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
            // flipX 는 메시 x 를 뒤집으므로 기울기 부호도 같이 뒤집어야
            // 화면상으로 "가는 쪽"으로 기운다.
            float dir = Mathf.Abs(vel.x) > moveDeadzone ? Mathf.Sign(vel.x) : 0f;
            target = leanAmount * dir * (_sr.flipX ? -1f : 1f);
        }
        _lean = Mathf.MoveTowards(_lean, target, leanResponse * leanAmount * Time.deltaTime);

        _sr.GetPropertyBlock(_mpb);
        _mpb.SetFloat(AnimSpeedId, moving ? bounceSpeed : 0f);
        _mpb.SetFloat(AnimPhaseId, 0f);
        _mpb.SetFloat(LeanAmtId,   _lean);
        _sr.SetPropertyBlock(_mpb);
    }
}
