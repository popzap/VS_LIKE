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
        _cam = GetComponent<Camera>();

        // 🔴 저장된 사용자 배율에서 시작한다 (D83). `defaultZoom` 으로 시작하면
        //    매 판 첫 몇 초 동안 예전 배율로 보이다가 스르륵 바뀐다.
        float z      = UserZoom;
        _currentZoom = z;
        _targetZoom  = z;
        _cam.orthographicSize = z;

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

    // 🔴 <b>키 입력은 LateUpdate 가 아니라 여기서 받는다</b> (D83).
    //    아래 LateUpdate 는 <c>target == null</c> 이거나 <c>deltaTime == 0</c>(일시정지)면
    //    바로 돌아간다 — 거기 넣으면 <b>메뉴나 일시정지 중에 F7/F8 이 안 먹는다.</b>
    private void Update() => HandleZoomKeys();

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

            // 🔴 아레나가 화면보다 좁으면 min > max 가 되고, Unity 의 Clamp 은 그때 <b>min 으로 붙인다</b>.
            //    그러면 카메라가 한쪽 구석에 처박힌다. 그 축은 <b>가두지 말고 중앙에 둔다</b> (D111).
            //    F7/F8 줌(4~11)으로 화면이 커질 수 있어서 실제로 일어날 수 있는 상황이다.
            float loX = cameraBounds.min.x + halfW, hiX = cameraBounds.max.x - halfW;
            float loY = cameraBounds.min.y + halfH, hiY = cameraBounds.max.y - halfH;
            smoothed.x = loX <= hiX ? Mathf.Clamp(smoothed.x, loX, hiX) : cameraBounds.center.x;
            smoothed.y = loY <= hiY ? Mathf.Clamp(smoothed.y, loY, hiY) : cameraBounds.center.y;
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
    /// <summary>
    /// 카메라를 이 사각형 안에 가둔다 (D111 · <see cref="ArenaBounds"/> 가 부른다).
    ///
    /// <para>🔑 <b>클램프 코드는 원래 있었다</b> — <c>useBounds</c> 가 꺼져 있었을 뿐이다.
    /// 여기서는 값을 넣고 스위치를 켜기만 한다.</para>
    ///
    /// <para>⚠️ 아레나가 화면보다 작으면 <c>min + halfW &gt; max - halfW</c> 가 되어
    /// <c>Mathf.Clamp</c> 이 <b>min 쪽으로 붙는다</b>(Unity 의 Clamp 은 min 을 우선한다).
    /// 그러면 카메라가 한쪽 구석에 붙어 버리므로, 그 경우에는 <b>가두지 않는다.</b></para>
    /// </summary>
    public void SetBounds(Bounds b)
    {
        cameraBounds = b;
        useBounds    = true;
    }

    public void SetTargetZoom(float zoom) => _targetZoom = zoom;

    /// <summary>기본 줌으로 복귀. 🔴 사용자가 F7/F8 로 정한 배율을 존중한다.</summary>
    public void ResetZoom() => _targetZoom = UserZoom;

    // ── 사용자 화면 배율 · F7 / F8 (D83) ─────────────────────────

    [Header("화면 배율 (F7 좁게 / F8 넓게)")]
    [Tooltip("한 번에 바뀌는 폭(월드 유닛). 0.5 면 F8 두 번에 한 칸이 더 보인다.")]
    [SerializeField] private float zoomStep = 0.5f;
    [Tooltip("가장 좁게. 이보다 좁으면 적이 화면 밖에서 튀어나온다.")]
    [SerializeField] private float minZoom  = 4f;
    [Tooltip("가장 넓게. 이보다 넓으면 적 스프라이트가 너무 작아 읽히지 않는다.")]
    [SerializeField] private float maxZoom  = 11f;

    private const string ZoomPrefKey = "vs_user_zoom";

    /// <summary>
    /// 사용자가 정한 화면 배율 (D83 · 사용자 요구).
    ///
    /// <para>요구: *"적과 플레이어 크기는 적절한데 화면에 너무 끼는 느낌이라
    /// F7,F8로 화면 배율을 조절할 수 있게 해줘"*.</para>
    ///
    /// <para>🔑 <b>적을 줄이는 대신 화면을 넓힌다.</b> `D79` 가 적을 1.5배로 키운 건
    /// 사용자 요구였고 판정도 *"크기는 적절"* 이었다. 좁게 느껴지는 건 크기가 아니라
    /// <b>보이는 범위</b>의 문제라 손잡이를 그쪽에 달았다.</para>
    ///
    /// <para>🔴 <b>런을 넘어 남는다</b>(<see cref="PlayerPrefs"/>). 매 판 다시 맞춰야 하면
    /// 손잡이가 아니라 성가신 일이 된다.</para>
    /// </summary>
    public float UserZoom
    {
        get
        {
            if (!_userZoomLoaded)
            {
                _userZoom = PlayerPrefs.GetFloat(ZoomPrefKey, defaultZoom);
                _userZoom = Mathf.Clamp(_userZoom, minZoom, maxZoom);
                _userZoomLoaded = true;
            }
            return _userZoom;
        }
    }

    private float _userZoom;
    private bool  _userZoomLoaded;

    /// <summary>F7 = 좁게(줌인) · F8 = 넓게(줌아웃). 단계는 <see cref="zoomStep"/>.</summary>
    public void NudgeUserZoom(float delta)
    {
        float before = UserZoom;                       // getter 가 처음 한 번 불러오기도 한다
        _userZoom = Mathf.Clamp(before + delta, minZoom, maxZoom);
        PlayerPrefs.SetFloat(ZoomPrefKey, _userZoom);
        _targetZoom = _userZoom;
        Debug.Log($"[Camera] 화면 배율 {before:F1} -> {_userZoom:F1} (F7 좁게 / F8 넓게 · 범위 {minZoom}~{maxZoom})");
    }

    private void HandleZoomKeys()
    {
        // 🔴 <b>`UnityEngine.Input` 을 쓰면 안 된다</b> (D83 에서 실제로 에디터를 멈췄다).
        //    이 프로젝트는 `ProjectSettings.activeInputHandler = 1`, 즉
        //    <b>Input System 패키지 전용</b>이다. 그 모드에서 옛 `Input.GetKeyDown` 은
        //    <b>매 프레임 `InvalidOperationException` 을 던진다</b> — 플레이 중에 초당 수십 번
        //    쏟아져 콘솔이 잠기고 에디터가 멎었다.
        //    🔑 <see cref="DevPanel"/>·<see cref="LevelUpManager"/> 가 이미 이 방식을 쓰고 있었다.
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null) return;   // 키보드가 없을 수 있다 (패드만 연결 등)

        if (kb[UnityEngine.InputSystem.Key.F7].wasPressedThisFrame) NudgeUserZoom(-zoomStep);
        if (kb[UnityEngine.InputSystem.Key.F8].wasPressedThisFrame) NudgeUserZoom(+zoomStep);
    }

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
