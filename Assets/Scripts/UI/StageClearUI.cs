using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 스테이지 클리어 시 나타나는 간략 결과 창.
/// 표시 항목: 획득 XP, 재화, 획득 아이템, 스탯 변화
/// 클릭(아무 곳이나) 또는 "계속" 버튼 → 스테이지 맵으로.
/// </summary>
public class StageClearUI : MonoBehaviour
{
    public static StageClearUI Instance { get; private set; }

    [Header("패널")]
    [SerializeField] private GameObject        panel;
    [SerializeField] private CanvasGroup       canvasGroup;
    [SerializeField] private float             fadeSpeed = 6f;

    [Header("헤더")]
    [SerializeField] private TextMeshProUGUI   stageTitleText;  // "Stage Clear!" / "Elite Cleared!" 등

    [Header("요약 수치")]
    [SerializeField] private TextMeshProUGUI   xpGainText;
    [SerializeField] private TextMeshProUGUI   currencyGainText;
    [SerializeField] private TextMeshProUGUI   killCountText;
    [SerializeField] private TextMeshProUGUI   timeText;

    [Header("스탯 변화 목록")]
    [SerializeField] private Transform         statChangeContainer;
    [SerializeField] private GameObject        statChangeRowPrefab; // TextMeshProUGUI "▲ Damage +0.2"

    [Header("획득 아이템 목록")]
    [SerializeField] private Transform         itemContainer;
    [SerializeField] private GameObject        itemIconPrefab;   // Image + Text (이름/레벨)

    [Header("계속 버튼")]
    [SerializeField] private Button            continueButton;
    [SerializeField] private TextMeshProUGUI   continueHint;     // "아무 곳이나 클릭..."

    // ── 스냅샷 ───────────────────────────────────────────────
    private StatBlock    _statsBeforeWave;
    private int          _xpBefore;
    private int          _currencyBefore;

    // ── 표시용 누적 ──────────────────────────────────────────
    private int          _xpGained;
    private int          _currencyGained;
    private int          _killCount;
    private float        _elapsed;

    private bool  _waitingForInput;
    private float _targetAlpha;
    private float _inputUnlockTime;   // 창이 뜬 프레임의 클릭으로 즉시 닫히는 것 방지

    // ────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        continueButton.onClick.AddListener(OnContinue);
        panel.SetActive(false);
    }

    private void Update()
    {
        // 페이드
        if (canvasGroup != null)
        {
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, _targetAlpha, fadeSpeed * Time.unscaledDeltaTime);
            if (!_waitingForInput && Mathf.Approximately(canvasGroup.alpha, 0f))
                panel.SetActive(false);
        }

        // 클릭으로 닫기
        if (_waitingForInput && Time.unscaledTime >= _inputUnlockTime &&
            Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            OnContinue();
    }

    // ── 스냅샷 저장 (웨이브 시작 전 WaveManager에서 호출) ────

    public void TakeSnapshot()
    {
        var stats = FindFirstObjectByType<PlayerStats>();
        _statsBeforeWave = stats != null ? CopyStats(stats.Final) : new StatBlock();
        _xpBefore        = ExperienceManager.Instance?.CurrentXp ?? 0;
        _currencyBefore  = GameManager.Instance?.MetaProgression.Currency ?? 0;
        _killCount = 0;
        _elapsed   = 0f;
    }

    public void AddKill()      => _killCount++;
    public void Tick(float dt) => _elapsed += dt;

    // ── 표시 ─────────────────────────────────────────────────

    public void Show(StageNode clearedNode)
    {
        _xpGained       = (ExperienceManager.Instance?.CurrentXp ?? 0) - _xpBefore;
        _currencyGained = (GameManager.Instance?.MetaProgression.Currency ?? 0) - _currencyBefore;

        panel.SetActive(true);
        _targetAlpha     = 1f;
        _waitingForInput = true;
        _inputUnlockTime = Time.unscaledTime + 0.4f;
        Time.timeScale   = 0f;

        // 헤더
        stageTitleText.text = clearedNode.StageType switch
        {
            StageType.Elite => "Elite Cleared!",
            StageType.Boss  => "Boss Defeated!",
            _               => "Stage Clear!"
        };

        // 수치
        xpGainText.text       = $"+{_xpGained} XP";
        currencyGainText.text  = $"+{_currencyGained}G";
        killCountText.text     = $"{_killCount} Kills";
        timeText.text          = FormatTime(_elapsed);

        // 스탯 변화
        PopulateStatChanges();

        // 아이템
        PopulateItems();

        continueHint.text = "Click to continue";
    }

    // ── 내부 ─────────────────────────────────────────────────

    private void PopulateStatChanges()
    {
        foreach (Transform child in statChangeContainer) Destroy(child.gameObject);

        var stats = FindFirstObjectByType<PlayerStats>();
        if (stats == null || _statsBeforeWave == null) return;

        var after = stats.Final;
        AddStatRow("Max HP",     _statsBeforeWave.MaxHp,       after.MaxHp);
        AddStatRow("Damage",     _statsBeforeWave.Damage,      after.Damage);
        AddStatRow("Move Speed", _statsBeforeWave.MoveSpeed,   after.MoveSpeed);
        AddStatRow("Armor",      _statsBeforeWave.Armor,       after.Armor);
        AddStatRow("Crit",       _statsBeforeWave.CritChance,  after.CritChance);
    }

    private void AddStatRow(string label, float before, float after)
    {
        float delta = after - before;
        if (Mathf.Abs(delta) < 0.001f) return;

        var go   = Instantiate(statChangeRowPrefab, statChangeContainer);
        var text = go.GetComponent<TextMeshProUGUI>();
        string sign  = delta > 0 ? "▲" : "▼";
        string color = delta > 0 ? "#22c55e" : "#ef4444";
        text.text = $"<color={color}>{sign}</color> {label}  <color={color}>{delta:+0.##;-0.##}</color>";
    }

    /// <summary>
    /// 이번 웨이브에 <i>새로 집은</i> 목록이 아니라 <b>현재 보유 중인 아이템 전체</b>를
    /// 누적 레벨과 함께 보여준다. 예전에는 획득할 때마다 한 줄씩 쌓아서
    /// 같은 무기를 3번 고르면 "Sword Lv.3" 이 세 줄 나왔다.
    /// </summary>
    private void PopulateItems()
    {
        foreach (Transform child in itemContainer) Destroy(child.gameObject);

        var levelUp = GameManager.Instance != null ? GameManager.Instance.LevelUpManager : null;
        if (levelUp == null) return;

        foreach (var item in levelUp.GetInventoryItems())
        {
            var go    = Instantiate(itemIconPrefab, itemContainer);
            var img   = go.GetComponentInChildren<Image>();
            var label = go.GetComponentInChildren<TextMeshProUGUI>();
            if (img)   img.sprite = item.Icon;
            if (label) label.text = $"{item.ItemName} Lv.{item.CurrentLevel}";
        }
    }

    private void OnContinue()
    {
        if (!_waitingForInput) return;
        _waitingForInput = false;
        _targetAlpha     = 0f;
        Time.timeScale   = 1f;
        GameManager.Instance.ChangeState(GameState.StageMap);
    }

    private static string FormatTime(float seconds)
    {
        int m = (int)(seconds / 60);
        int s = (int)(seconds % 60);
        return $"{m:00}:{s:00}";
    }

    private static StatBlock CopyStats(StatBlock src) => new()
    {
        MaxHp          = src.MaxHp,
        MoveSpeed      = src.MoveSpeed,
        Damage         = src.Damage,
        AttackSpeed    = src.AttackSpeed,
        ProjectileSize = src.ProjectileSize,
        PickupRadius   = src.PickupRadius,
        CritChance     = src.CritChance,
        CritMultiplier = src.CritMultiplier,
        Armor          = src.Armor,
    };
}
