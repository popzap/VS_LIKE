using UnityEngine;

/// <summary>
/// 스테이지 이벤트 (D37 에 스텁에서 실물로).
///
/// <para>예전에는 <c>XpBonus</c>·<c>CurrencyBonus</c>·<c>TriggerRandomWave</c> 셋뿐이었고
/// 제목·설명이 <c>Debug.Log</c> 로만 나갔다. 즉 <b>플레이어는 무슨 일이 났는지 못 봤고</b>,
/// 이벤트는 사실상 "공짜 XP 또는 노말 웨이브 한 번" 이었다 (<c>ROADMAP.md</c> §7).</para>
///
/// <para>이제 <see cref="EventKind"/> 로 갈래가 생기고, <see cref="EventUI"/> 가 제목·설명·선택지를
/// 화면에 띄운다. 🔑 <b>이벤트는 "다른 규칙으로 한 판"이라야 콘텐츠가 된다</b> —
/// 보상만 주는 것은 상자와 다를 게 없다.</para>
/// </summary>
public class EventManager : MonoBehaviour
{
    public static EventManager Instance { get; private set; }

    /// <summary>
    /// 이벤트의 갈래. 🔴 <c>Events.csv</c> 의 <c>Kind</c> 열과 이름이 같아야 한다 —
    /// 임포터가 문자열로 파싱하고, <b>모르는 값은 <see cref="Reward"/> 로 떨어진다.</b>
    /// </summary>
    public enum EventKind
    {
        /// <summary>예전 동작. 보상을 주고 끝난다 (전투 여부는 <c>TriggerRandomWave</c>).</summary>
        Reward,
        /// <summary>E7 — 런 골드를 메타 골드로 환전한다. 환율은 나쁘다.</summary>
        Exchange,
        /// <summary>E9 — 다음 전투 동안 제단 없이 아무 데서나 승급할 수 있다.</summary>
        FieldPromotion,
        /// <summary>E11 — 바닥에 예고 원이 계속 뜨는 전투.</summary>
        Minefield,
    }

    [System.Serializable]
    public class GameEvent
    {
        public string    Title;
        [TextArea] public string Description;
        public EventKind Kind = EventKind.Reward;

        [Header("Reward")]
        public int  XpBonus;
        public int  CurrencyBonus;
        public bool TriggerRandomWave;

        [Header("선택지 문구 (비우면 확인 버튼 하나만 뜬다)")]
        public string AcceptLabel;
        public string DeclineLabel;

        [Header("E7 Exchange — 런 골드 → 메타 골드")]
        [Tooltip("런 골드 몇 개가 메타 골드 1 이 되는가. 3 이면 3:1")]
        public int ExchangeRate = 3;
        [Tooltip("한 번에 바꿀 수 있는 메타 골드 상한. 0 이면 무제한")]
        public int ExchangeCap  = 40;

        [Header("E11 Minefield — 예고 원이 깔리는 전투")]
        [Tooltip("한 번에 깔리는 예고 원 수")]
        public int   MineCount    = 3;
        [Tooltip("다음 무리까지 간격(초)")]
        public float MineInterval = 2.6f;
        [Tooltip("플레이어 주변 이 반경 안에 깐다")]
        public float MineSpread   = 7f;
    }

    [SerializeField] private GameEvent[] events;

    /// <summary>지금 벌어지는 이벤트. <see cref="EventUI"/> 가 읽는다.</summary>
    public GameEvent Current { get; private set; }

    private StageNode _node;

    // ── E9 야전 승급 ────────────────────────────────────────────
    //
    // 🔴 static 을 쓰지 않는다. 씬을 다시 로드해도 남아 있으면
    //    "왜 승급이 되지?" 를 아무도 설명 못 한다 (I-25 계열 사고).

    /// <summary>제단 조건을 무시하는가 (E9). <b>다음 전투가 끝나면 꺼진다</b>(WaveManager.ClearWave).</summary>
    public bool FieldPromotionActive { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── 진입 ────────────────────────────────────────────────────

    public void TriggerRandomEvent(StageNode node)
    {
        _node = node;

        if (events == null || events.Length == 0) { FinishEvent(); return; }

        Current = events[Random.Range(0, events.Length)];
        Debug.Log($"[Event] {Current.Title} ({Current.Kind}): {Current.Description}");

        // 화면이 없으면 예전처럼 곧바로 처리한다 — UI 가 아직 안 붙은 씬에서도 굴러가야 한다.
        if (EventUI.Instance == null)
        {
            Accept();
            return;
        }

        EventUI.Instance.Show(Current);
    }

    // ── 선택 (EventUI 버튼이 부른다) ────────────────────────────

    /// <summary>받아들인다. 갈래별 효과가 여기서 갈린다.</summary>
    public void Accept()
    {
        var e = Current;
        if (e == null) { FinishEvent(); return; }

        switch (e.Kind)
        {
            case EventKind.Exchange:       DoExchange(e);       break;
            case EventKind.FieldPromotion: DoFieldPromotion();  break;
            case EventKind.Minefield:      DoMinefield(e);      return;   // 전투로 넘어간다
            default:                       DoReward(e);         return;   // 전투일 수도 있다
        }

        FinishEvent();
    }

    /// <summary>거절한다. 아무 일도 없이 맵으로 돌아간다.</summary>
    public void Decline()
    {
        Debug.Log($"[Event] '{Current?.Title}' 거절");
        FinishEvent();
    }

    // ── 갈래별 처리 ─────────────────────────────────────────────

    private void DoReward(GameEvent e)
    {
        if (e.XpBonus > 0)       ExperienceManager.Instance?.CollectXp(e.XpBonus);
        if (e.CurrencyBonus > 0) GameManager.Instance.GrantGold(e.CurrencyBonus);   // 런 골드

        if (e.TriggerRandomWave) StartEventWave();
        else                     FinishEvent();
    }

    /// <summary>
    /// E7 — 런 골드를 메타 골드로 바꾼다.
    ///
    /// <para>🔑 <b>이 게임에만 성립하는 선택이다.</b> 런 골드는 런이 끝나면 소멸하므로
    /// (<c>D25</c>) "남은 걸 얼마나 들고 나갈까" 가 진짜 결정이 된다.
    /// 환율을 나쁘게 두는 이유도 그것이다 — 공짜면 선택이 아니다.</para>
    /// </summary>
    private void DoExchange(GameEvent e)
    {
        var gm = GameManager.Instance;
        int rate = Mathf.Max(1, e.ExchangeRate);

        int meta = gm.RunGold / rate;
        if (e.ExchangeCap > 0) meta = Mathf.Min(meta, e.ExchangeCap);

        if (meta <= 0)
        {
            Debug.Log($"[Event] 환전 불가 — 런 골드 {gm.RunGold} 로는 {rate}:1 을 못 채운다");
            return;
        }

        int spent = meta * rate;
        if (!gm.SpendRunGold(spent)) return;

        gm.MetaProgression.AddCurrency(meta);
        gm.MetaProgression.Save();
        Debug.Log($"[Event] 환전 — 런 골드 {spent} → 메타 골드 {meta} (환율 {rate}:1, 상한 {e.ExchangeCap})");
    }

    /// <summary>
    /// E9 — 다음 전투 동안 제단 조건을 무시한다.
    ///
    /// <para>승급은 이 게임의 유일한 설계 차별점인데 <b>"건물 앞에서 E"</b> 를 모르면 못 쓴다.
    /// 이 이벤트는 그걸 한 번 강제로 알려 주는 장치다.</para>
    /// </summary>
    private void DoFieldPromotion()
    {
        FieldPromotionActive = true;
        Debug.Log("[Event] 야전 승급 — 다음 전투 동안 제단 없이 승급할 수 있다");
    }

    /// <summary>E11 — 예고 원이 깔리는 전투로 넘어간다.</summary>
    private void DoMinefield(GameEvent e)
    {
        StartEventWave();
        var wm = GameManager.Instance.WaveManager;
        if (wm != null) wm.BeginMinefield(e.MineCount, e.MineInterval, e.MineSpread);
    }

    // ── 공통 ────────────────────────────────────────────────────

    private void StartEventWave()
    {
        // 상태 전환을 빠뜨리면 Event 상태로 남아 HUD 도 맵도 표시되지 않는다.
        GameManager.Instance.WaveManager.StartWave(_node);
        GameManager.Instance.ChangeState(GameState.Wave);
    }

    private void FinishEvent()
    {
        Current = null;
        GameManager.Instance.StageMap.AdvanceToNext(_node);
        GameManager.Instance.ChangeState(GameState.StageMap);
    }

    /// <summary>전투가 끝날 때 전투 한정 효과를 끈다. <see cref="WaveManager"/>.ClearWave 가 부른다.</summary>
    public void ClearLayerEffects()
    {
        if (FieldPromotionActive) Debug.Log("[Event] 야전 승급 종료");
        FieldPromotionActive = false;
    }
}
