using UnityEngine;

/// <summary>
/// 보스가 화면 밖에 있을 때 <b>화면 테두리에 화살표</b>로 방향을 알린다 (D66 · 사용자 요구 13).
///
/// <para>사용자 요구: *"보스 화면 밖에 있으면 어디있는지 맵 테두리에 화살표로"*.
/// 보스는 소환 지점이 플레이어와 멀고 이동이 느려서, 등장음이 울린 뒤에도
/// <b>어디로 가야 만나는지</b>가 화면 어디에도 없었다.</para>
///
/// <para>🔑 보스를 <c>FindObjectsByType</c> 으로 찾아다니지 않는다 —
/// <see cref="WaveManager.OnBossSpawned"/> 를 구독한다. <see cref="BossHealthBarUI"/> 와 같은 방식이고,
/// 같은 이벤트라 <b>둘이 어긋날 수가 없다</b>.</para>
///
/// <para>🔑 <b>씬 배선이 필요 없다.</b> 화살표 오브젝트를 런타임에 스스로 만든다 —
/// 캔버스에 칸을 하나 더 두면 그 칸이 언젠가 비거나(<c>I-24</c>) 위치가 어긋난다.</para>
/// </summary>
public class BossOffscreenArrowUI : MonoBehaviour
{
    /// <summary>컴파일 반영 확인용 (D27). 값을 바꿨으면 이 숫자를 올린다.</summary>
    public const int Version = 5;   // 5 = 외곽선 (B21) · 4 = 띠 210 (B20) · 3 = 212 · 2 = 띠 회피 · 1 = 최초

    [Tooltip("화면 테두리에서 안쪽으로 이만큼 띄운다(px). 화살표가 잘리지 않을 만큼.")]
    [SerializeField] private float edgeMargin = 56f;

    [Tooltip("화살표 크기(px).")]
    [SerializeField] private float arrowSize = 44f;

    [SerializeField] private Color arrowColor = new(0.90f, 0.24f, 0.24f, 0.92f);

    [Tooltip("화살표 외곽선 두께(캔버스px). 0 이면 안 그린다. 보스 체력 바와 같은 빨강 위에서도 갈리게 한다 (B21).")]
    [SerializeField] private float arrowOutline = 6f;

    [SerializeField] private Color arrowOutlineColor = new(0f, 0f, 0f, 0.85f);

    [Tooltip("0 이면 깜빡이지 않는다. 초당 맥동 횟수.")]
    [SerializeField] private float pulsePerSecond = 1.6f;

    [Tooltip("🔴 이 거리(월드 유닛)보다 멀면 화면 안이어도 화살표를 띄운다. 0 이면 예전처럼 화면 밖에서만.")]
    [SerializeField] private float showBeyondDistance = 7f;

    /// <summary>
    /// 화면 위쪽 HUD 글자 띠의 높이(캔버스px). 화살표는 이 아래로만 다닌다 (B20).
    ///
    /// <para>🔴 <b>보스가 있는 상태에서 재야 한다.</b> 처음엔 100 으로 넣었다가 그대로 겹쳤다 —
    /// 보스 없이 쟀기 때문이다. 화살표는 <b>보스가 있을 때만</b> 뜨고 그때는
    /// <c>BossNameText</c>·<c>BossPhaseText</c> 가 화면 위쪽에 추가로 뜬다.</para>
    ///
    /// <para>🔑 <b>글리프로 쟀다. 글자의 <c>RectTransform</c> 이 아니다.</b>
    /// <c>BossNameText</c> 의 rect 는 가로로 넓은데 글자는 왼쪽에만 있어서,
    /// rect 로 재면 <b>안 겹치는 것도 겹친다고 나온다</b>(12방향 중 하나가 헛경보였다).
    /// <c>ForceMeshUpdate</c> 뒤 <c>characterInfo[i].isVisible</c> 인 것만 모아 외곽을 냈다.</para>
    ///
    /// <para>글리프 아래끝(위 테두리에서): <c>BossNameText</c> <b>206</b> ·
    /// <c>BossPhaseText</c> 205 · <c>TimerText</c> 149 · <c>KillText</c> 91 · <c>HPText</c> 89.</para>
    ///
    /// <para>⚠️ HUD 를 손대면 <b>이 값을 다시 재라.</b> 그냥 두면 화살표가 조용히 다시 글자를 덮는다.</para>
    /// </summary>
    [Tooltip("화면 위쪽 HUD 글자 띠의 깊이(캔버스px). 보스 이름표까지 글리프로 잰 값 + 여유.")]
    [SerializeField] private float topHudBand = 210f;

    private WaveManager      _wave;
    private EnemyBase        _boss;
    private BossArrowGraphic _arrow;
    private RectTransform    _arrowRt;
    private RectTransform    _canvasRt;

    private void Start()
    {
        // 🔴 Awake 가 아니라 Start 다 — GameManager.Instance.WaveManager 는
        //    GameManager.Start 에서 채워지고 모든 Awake 는 모든 Start 보다 먼저 돈다 (I-8/I-38).
        _wave = GameManager.Instance != null ? GameManager.Instance.WaveManager : null;
        if (_wave != null) _wave.OnBossSpawned += HandleBossSpawned;

        BuildArrow();
        SetVisible(false);
    }

    private void OnDestroy()
    {
        if (_wave != null) _wave.OnBossSpawned -= HandleBossSpawned;
    }

    private void BuildArrow()
    {
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;
        _canvasRt = canvas.rootCanvas.transform as RectTransform;
        if (_canvasRt == null) return;

        // 🔴 <b>CanvasRenderer 를 여기서 같이 만든다</b> (D104).
        //    Graphic 이 [RequireComponent(typeof(CanvasRenderer))] 를 달고 있어도
        //    <b>파생 클래스를 AddComponent 할 때 그게 안 따라왔다</b> — 실측으로 확인했다.
        //    그러면 화살표는 켜지고 위치·각도까지 정확한데 <b>한 픽셀도 안 그려진다.</b>
        //    activeSelf 로는 절대 안 잡히는 고장이라 세 번을 놓쳤다.
        //    ⇒ 속성에 기대지 말고 생성 시점에 못박는다.
        var go = new GameObject("BossOffscreenArrow", typeof(RectTransform), typeof(CanvasRenderer));
        go.transform.SetParent(_canvasRt, false);

        _arrowRt = (RectTransform)go.transform;
        _arrowRt.anchorMin = _arrowRt.anchorMax = new Vector2(0.5f, 0.5f);
        _arrowRt.pivot     = new Vector2(0.5f, 0.5f);
        _arrowRt.sizeDelta = new Vector2(arrowSize, arrowSize * 0.78f);

        _arrow = go.AddComponent<BossArrowGraphic>();
        _arrow.color = arrowColor;
        _arrow.OutlineWidth = arrowOutline;      // B21 — 같은 빨강 위에서도 갈리게
        _arrow.OutlineColor = arrowOutlineColor;
        _arrow.raycastTarget = false;   // 화살표가 버튼을 먹으면 안 된다
    }

    private void HandleBossSpawned(EnemyBase boss, BossBrain brain) => _boss = boss;

    private void LateUpdate()
    {
        if (_arrowRt == null) return;

        if (_boss == null || _boss.Dead || !_boss.gameObject.activeInHierarchy
            || GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Wave)
        {
            SetVisible(false);
            return;
        }

        var cam = Camera.main;
        if (cam == null) { SetVisible(false); return; }

        // 캔버스 좌표계로 옮긴다. ScreenSpaceOverlay 라 화면 px 와 1:1 이지만,
        // 해상도가 바뀌어도 맞도록 캔버스 rect 를 기준으로 계산한다.
        Vector3 sp = cam.WorldToScreenPoint(_boss.transform.position);
        var size   = _canvasRt.rect.size;
        float sx   = Screen.width  > 0 ? size.x / Screen.width  : 1f;
        float sy   = Screen.height > 0 ? size.y / Screen.height : 1f;

        // 캔버스 중심 기준 좌표 (오른쪽/위가 +)
        Vector2 p = new Vector2(sp.x * sx - size.x * 0.5f, sp.y * sy - size.y * 0.5f);

        float halfW = size.x * 0.5f - edgeMargin;
        float halfH = size.y * 0.5f - edgeMargin;

        // 🔴 <b>위쪽만 더 안으로 민다</b> (B20). 화살표는 테두리를 타는데
        //    HUD 글자 여섯(HP·Level·Timer·XP·Kills·Gold)이 <b>전부 화면 위쪽 띠</b>에 있다
        //    ⇒ 보스가 위쪽에 있으면 <b>반드시</b> 그 위를 탄다. 12방향 중 5방향에서 겹쳤다.
        //    글자를 옮기지 않고 화살표를 내린다 — 글자를 건드리면 D85 가 잡아 놓은 배치가 흔들린다.
        //
        //    🔑 <b>여유를 arrowSize 에서 계산한다.</b> 회전한 사각형의 화면 AABB 는
        //    각도에 따라 커지는데 <b>대각선을 넘지는 못한다</b> ⇒ 그 절반이 최악의 반높이다.
        //    상수로 박아 두면 arrowSize 를 바꿀 때(이미 44 → 96 으로 바뀌었다) 조용히 다시 겹친다.
        //    ⚠️ <b>외곽선은 rect 밖으로 나간다</b> (B21). 그 배율을 안 곱하면 B20 이 다시 열린다.
        float w = _arrowRt.rect.width, h = _arrowRt.rect.height;
        float arrowHalfMax = 0.5f * Mathf.Sqrt(w * w + h * h) * _arrow.OutlineScale(_arrowRt.rect);
        float halfHTop = size.y * 0.5f - Mathf.Max(edgeMargin, topHudBand + arrowHalfMax);

        // 🔴 <b>"화면 밖일 때만" 은 실제로 거의 안 뜬다</b> (D85 · 사용자가 세 번 지적했다).
        //    보스는 플레이어를 쫓고 카메라는 플레이어를 따라가므로 <b>화면 밖으로 나갈 일이 거의 없다.</b>
        //    `D83` 에서 보스를 25.7유닛 밖으로 옮겨 보니 화살표는 정상으로 떴다 —
        //    즉 <b>기능이 죽은 게 아니라 조건이 안 생겼다.</b>
        //
        //    ⇒ 조건을 하나 더 준다: <b>멀면</b> 화면 안이어도 띄운다.
        //    보스는 화면 안에 있어도 작은 점이라 난전에서 안 찾아진다 — 사용자가 본 게 그 상태다.
        //    🔑 가까우면 안 뜬다 — 눈앞에 있는 걸 화살표로 가리키면 그건 방해다.
        var player = PlayerStats.Current;
        bool far = showBeyondDistance > 0f && player != null
                && Vector2.Distance(player.transform.position, _boss.transform.position) > showBeyondDistance;

        if (!far && sp.z > 0f && Mathf.Abs(p.x) <= halfW && Mathf.Abs(p.y) <= halfH)
        {
            SetVisible(false);
            return;
        }

        // 🔴 카메라 뒤(z<0)면 화면 좌표가 뒤집힌다. 2D 직교라 거의 안 나지만,
        //    한 번이라도 나면 화살표가 정반대를 가리켜 안 넣느니만 못하다.
        if (sp.z < 0f) p = -p;

        if (p.sqrMagnitude < 0.0001f) p = Vector2.right;

        // 중심에서 p 방향으로 뻗은 반직선이 테두리 사각형과 만나는 점.
        // 두 축 중 <b>먼저 닿는 쪽</b>이 경계라서 비율의 최솟값을 쓴다.
        // 🔑 위로 갈 때만 한계가 다르다 (B20). "언제 뜨는가"(위의 화면 안 판정)는 안 건드렸다 —
        //    그건 이 버그 밖이고, 같이 바꾸면 화살표가 뜨는 조건까지 달라진다.
        float limY = p.y >= 0f ? halfHTop : halfH;

        float tx = Mathf.Abs(p.x) > 0.0001f ? halfW / Mathf.Abs(p.x) : float.MaxValue;
        float ty = Mathf.Abs(p.y) > 0.0001f ? limY  / Mathf.Abs(p.y) : float.MaxValue;
        Vector2 edge = p * Mathf.Min(tx, ty);

        _arrowRt.anchoredPosition = edge;
        _arrowRt.localRotation    = Quaternion.Euler(0f, 0f, Mathf.Atan2(p.y, p.x) * Mathf.Rad2Deg);

        var c = arrowColor;
        if (pulsePerSecond > 0f)
        {
            // 일시정지·레벨업에서도 맥동이 얼어붙지 않게 unscaled 로 센다 (D41 에서 겪었다).
            float s = 0.72f + 0.28f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * Mathf.PI * pulsePerSecond));
            c.a *= s;
        }
        _arrow.color = c;

        SetVisible(true);
    }

    private void SetVisible(bool on)
    {
        if (_arrowRt != null && _arrowRt.gameObject.activeSelf != on)
            _arrowRt.gameObject.SetActive(on);
    }
}
