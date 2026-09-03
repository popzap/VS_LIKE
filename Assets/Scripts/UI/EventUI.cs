using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 이벤트 화면 (D37) — **이벤트가 처음으로 눈에 보이게 되는 곳**이다.
///
/// <para>예전에는 <c>EventManager</c> 가 제목·설명을 <c>Debug.Log</c> 로만 찍었다.
/// 플레이어는 <b>무슨 일이 났는지 몰랐고</b>, 그래서 선택형 이벤트가 아예 성립할 수 없었다
/// (<c>ROADMAP.md</c> §7).</para>
///
/// <para>⚠️ <see cref="GameStatePanel"/> 규칙대로 이 스크립트가 붙은 오브젝트는 <b>항상 활성</b>이어야 한다.
/// 켜고 끄는 건 <c>panel</c> 이다 — 꺼진 오브젝트는 <c>Awake</c> 가 안 돌아 상태 구독을 놓친다.</para>
/// </summary>
public class EventUI : GameStatePanel
{
    public static EventUI Instance { get; private set; }

    [Header("텍스트")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI bodyText;

    [Header("버튼")]
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button declineButton;
    [SerializeField] private TextMeshProUGUI acceptLabel;
    [SerializeField] private TextMeshProUGUI declineLabel;

    protected override bool IsVisibleIn(GameState state) => state == GameState.Event;

    protected override void Awake()
    {
        base.Awake();
        Instance = this;

        // MainMenuUI 와 같은 이유로 Awake 에서 건다 — 상태 방송이 Start 보다 먼저 올 수 있다.
        if (acceptButton  != null) acceptButton .onClick.AddListener(OnAccept);
        if (declineButton != null) declineButton.onClick.AddListener(OnDecline);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (Instance == this) Instance = null;
    }

    protected override void OnShown(GameState state)
    {
        // 이벤트 화면은 멈춘 화면이다. 뒤에서 웨이브가 돌 이유가 없다.
        Time.timeScale = 1f;
    }

    /// <summary><see cref="EventManager"/> 가 부른다. 상태 전환은 이미 끝난 뒤다.</summary>
    public void Show(EventManager.GameEvent e)
    {
        if (e == null) return;

        if (titleText != null) titleText.text = e.Title;
        if (bodyText  != null) bodyText.text  = BuildBody(e);

        // 🔴 거절 문구가 비어 있으면 거절 버튼 자체를 숨긴다 —
        //    "선택지가 없는 이벤트" 에 빈 버튼이 남으면 눌러도 되는 줄 안다.
        bool hasDecline = !string.IsNullOrEmpty(e.DeclineLabel);
        if (declineButton != null) declineButton.gameObject.SetActive(hasDecline);

        if (acceptLabel  != null) acceptLabel.text  = string.IsNullOrEmpty(e.AcceptLabel) ? "Continue" : e.AcceptLabel;
        if (declineLabel != null) declineLabel.text = hasDecline ? e.DeclineLabel : "Leave";
    }

    /// <summary>
    /// 설명 아래에 <b>이번에 실제로 일어날 일</b>을 한 줄 덧붙인다.
    ///
    /// <para>설명문은 분위기를 말하고, 이 줄은 <b>숫자를 말한다.</b>
    /// 둘을 섞으면 플레이어가 무엇을 고르는지 흐려진다.</para>
    /// </summary>
    private static string BuildBody(EventManager.GameEvent e)
    {
        string extra = null;
        var gm = GameManager.Instance;

        switch (e.Kind)
        {
            case EventManager.EventKind.Exchange:
            {
                int rate = Mathf.Max(1, e.ExchangeRate);
                int meta = gm != null ? gm.RunGold / rate : 0;
                if (e.ExchangeCap > 0) meta = Mathf.Min(meta, e.ExchangeCap);
                extra = meta > 0
                    ? $"<color=#F0C040>{meta * rate} run gold  ->  {meta} meta gold</color>   ({rate}:1)"
                    : "<color=#8A8F98>Not enough run gold.</color>";
                break;
            }
            case EventManager.EventKind.FieldPromotion:
                extra = "<color=#F0C040>Promote anywhere in your next battle  —  no altar needed.</color>";
                break;
            case EventManager.EventKind.Minefield:
                extra = "<color=#E04030>The ground will not be safe.</color>";
                break;
            default:
                if (e.XpBonus > 0 || e.CurrencyBonus > 0)
                    extra = $"<color=#F0C040>+{e.XpBonus} XP   +{e.CurrencyBonus} G</color>";
                break;
        }

        return string.IsNullOrEmpty(extra) ? e.Description : e.Description + "\n\n" + extra;
    }

    // ── 버튼 ────────────────────────────────────────────────────

    private void OnAccept()
    {
        AudioManager.Play(SfxId.UiSelect);
        EventManager.Instance?.Accept();
    }

    private void OnDecline()
    {
        AudioManager.Play(SfxId.UiCancel);
        EventManager.Instance?.Decline();
    }
}
