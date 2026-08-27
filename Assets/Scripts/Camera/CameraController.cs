using UnityEngine;

/// <summary>
/// 플레이어를 부드럽게 추적하는 카메라.
///
/// 특징:
/// 1. SmoothDamp — 약간의 지연으로 이동 속도감을 시각적으로 전달
/// 2. Look-ahead  — 이동 방향 앞쪽을 미리 보여줌
/// 3. 화면 흔들기 — Shake() 호출로 타격감 연출
/// 4. 줌 인/아웃  — SetTargetZoom()으로 보스 등장 연출
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("추적 대상")]
    [SerializeField] private Transform target;

    [Header("부드러운 이동")]
    [SerializeField, Range(0.05f, 0.5f)]
    private float smoothTime = 0.18f;       // 낮을수록 빠르게 따라옴 (0.18 = 이동 속도감 느껴짐)

    [Header("룩어헤드 (이동 방향 미리 보기)")]
    [SerializeField] private bool  useLookAhead     = true;
    [SerializeField] private float lookAheadDist    = 1.8f;    // 앞으로 보는 거리
    [SerializeField] private float lookAheadSmooth  = 0.12f;   // 룩어헤드 자체 스무딩

    [Header("줌")]
    [SerializeField] private float defaultZoom      = 6f;
    [SerializeField] private float zoomSmoothTime   = 0.3f;

    [Header("카메라 경계 (선택)")]
    [SerializeField] private bool  useBounds        = false;
    [SerializeField] private Bounds cameraBounds;

    // ── 런타임 상태 ──────────────────────────────────────────
    private Camera   _cam;
    private Vector3  _velocity     = Vector3.zero;
    private Vector2  _lookAheadPos = Vector2.zero;
    private Vector2  _lookAheadVel = Vector2.zero;

    private float    _currentZoom;
    private float    _targetZoom;
    private float    _zoomVelocity;

    private Vector2  _lastTargetPos;

    // 흔들기
    private float    _shakeTimer;
    private float    _shakeMagnitude;
    private float    _shakeDampening = 1f;

    // ────────────────────────────────────────────────────────

    private void Awake()
    {
        _cam         = GetComponent<Camera>();
        _currentZoom = defaultZoom;
        _targetZoom  = defaultZoom;
        _cam.orthographicSize = defaultZoom;

        if (target != null)
        {
            _lastTargetPos = target.position;
            transform.position = new Vector3(target.position.x, target.position.y, transform.position.z);
        }
    }

    private void Start()
    {
        // 플레이어 자동 탐색 (Inspector에서 미지정 시)
        if (target == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player) target = player.transform;
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // 일시정지(timeScale = 0) 중에는 deltaTime 이 0 이라 속도 계산이 0/0 = NaN 이 된다.
        // NaN 은 한 번 섞이면 계속 전파되므로 아예 갱신을 건너뛴다.
        if (Time.deltaTime <= 0f) return;

        UpdateLookAhead();
        UpdatePosition();
        UpdateZoom();
        ApplyShake();
    }

    // ── 위치 추적 ────────────────────────────────────────────

    private void UpdateLookAhead()
    {
        if (!useLookAhead) return;

        Vector2 moveDir = ((Vector2)target.position - _lastTargetPos) / Time.deltaTime;
        _lastTargetPos  = target.position;

        // 이동 방향 앞쪽 오프셋
        Vector2 targetLook = moveDir.normalized * lookAheadDist
                             * Mathf.Clamp01(moveDir.magnitude / 2f);  // 속도에 비례

        _lookAheadPos = Vector2.SmoothDamp(_lookAheadPos, targetLook,
                        ref _lookAheadVel, lookAheadSmooth);
    }

    private void UpdatePosition()
    {
        Vector2 desiredPos = (Vector2)target.position + _lookAheadPos;
        Vector3 desired    = new(desiredPos.x, desiredPos.y, transform.position.z);

        // SmoothDamp — 이동 방향으로 약간 늦게 따라오므로 속도감이 생김
        Vector3 smoothed = Vector3.SmoothDamp(transform.position, desired,
                           ref _velocity, smoothTime, Mathf.Infinity, Time.deltaTime);

        // 경계 클램프
        if (useBounds)
        {
            float halfH = _cam.orthographicSize;
            float halfW = halfH * _cam.aspect;
            smoothed.x = Mathf.Clamp(smoothed.x, cameraBounds.min.x + halfW, cameraBounds.max.x - halfW);
            smoothed.y = Mathf.Clamp(smoothed.y, cameraBounds.min.y + halfH, cameraBounds.max.y - halfH);
        }

        transform.position = smoothed;
    }

    // ── 줌 ──────────────────────────────────────────────────

    private void UpdateZoom()
    {
        _currentZoom = Mathf.SmoothDamp(_currentZoom, _targetZoom,
                       ref _zoomVelocity, zoomSmoothTime);
        _cam.orthographicSize = _currentZoom;
    }

    // ── 화면 흔들기 ──────────────────────────────────────────

    private void ApplyShake()
    {
        if (_shakeTimer <= 0f) return;

        _shakeTimer -= Time.deltaTime;
        float mag = _shakeMagnitude * (_shakeTimer * _shakeDampening);
        Vector2 offset = Random.insideUnitCircle * mag;
        transform.position += (Vector3)(Vector2)offset;
    }

    // ── Public API ───────────────────────────────────────────

    /// <summary>타격감, 폭발 등에 사용.</summary>
    /// <param name="magnitude">흔들림 강도 (0.1~0.5 권장)</param>
    /// <param name="duration">지속 시간(초)</param>
    public void Shake(float magnitude = 0.25f, float duration = 0.3f)
    {
        _shakeMagnitude = magnitude;
        _shakeTimer     = duration;
        _shakeDampening = 1f / duration;
    }

    /// <summary>보스 등장 줌아웃, 클리어 줌인 등에 사용.</summary>
    public void SetTargetZoom(float zoom) => _targetZoom = zoom;

    /// <summary>기본 줌으로 복귀.</summary>
    public void ResetZoom() => _targetZoom = defaultZoom;

    /// <summary>즉시 타겟 위치로 이동 (씬 전환 후 등).</summary>
    public void SnapToTarget()
    {
        if (target == null) return;
        transform.position = new Vector3(target.position.x, target.position.y, transform.position.z);
        _velocity = Vector3.zero;
    }

    public void SetTarget(Transform t)
    {
        target = t;
        SnapToTarget();
    }

    private void OnDrawGizmosSelected()
    {
        if (!useBounds) return;
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.3f);
        Gizmos.DrawWireCube(cameraBounds.center, cameraBounds.size);
    }
}
