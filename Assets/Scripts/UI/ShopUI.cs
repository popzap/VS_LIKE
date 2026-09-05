using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ────────────────────────────────────────────────────────────────────────────
//  ShopUI  —  상점 전체 UI 컨트롤러
//  레이아웃: [왼쪽: 제거 패널] [중앙: 구매 슬롯] [오른쪽: NPC 패널]
// ────────────────────────────────────────────────────────────────────────────
public class ShopUI : MonoBehaviour
{
    public static ShopUI Instance { get; private set; }

    // ── 루트 ─────────────────────────────────────────────────────
    [Header("패널 루트")]
    [SerializeField] private GameObject  shopRoot;
    [SerializeField] private CanvasGroup canvasGroup;

    // ── 왼쪽: 제거 패널 ─────────────────────────────────────────
    [Header("제거 패널 (왼쪽)")]
    [SerializeField] private Transform          removeListContent;   // ScrollView Content
    [SerializeField] private ShopRemoveRowUI    removeRowPrefab;
    [SerializeField] private TextMeshProUGUI    removeHintText;      // "제거 시 50% 환급" 안내

    // ── 중앙: 구매 슬롯 ─────────────────────────────────────────
    [Header("구매 슬롯 (중앙)")]
    [SerializeField] private Transform       shopCardContent;        // 카드 부모
    [SerializeField] private ShopCardUI      shopCardPrefab;
    [SerializeField] private Button          rerollButton;
    [SerializeField] private TextMeshProUGUI rerollCostText;
    [SerializeField] private Button          closeButton;

    // ── 오른쪽: NPC 패널 ────────────────────────────────────────
    [Header("NPC 패널 (오른쪽)")]
    [SerializeField] private TextMeshProUGUI npcDialogueText;
    [SerializeField] private TextMeshProUGUI killCountText;
    [SerializeField] private TextMeshProUGUI elapsedTimeText;
    [SerializeField] private TextMeshProUGUI playerLevelText;
    [SerializeField] private TextMeshProUGUI waveProgressText;

    // ── HUD 골드 표시 ────────────────────────────────────────────
    [Header("상단 골드")]
    [SerializeField] private TextMeshProUGUI currencyText;

    // ── NPC 대사 목록 ────────────────────────────────────────────
    [Header("NPC 대사 (랜덤)")]
    [SerializeField] private string[] npcDialogues = new[]
    {
        "Only the finest goods here.\nNothing you fancy?\nI can reroll the lot.",
        "Today's bargain! Buy quick,\nor the next customer will.",
        "Looking for something?\nI can help. For a price.",
        "Stronger foes are waiting.\nCome prepared, friend.",
        "Removing is cheap. Travel light,\nthat's what I always say."
    };

    // ── 페이드 설정 ──────────────────────────────────────────────
    [Header("페이드")]
    [SerializeField] private float fadeSpeed = 6f;

    private float _targetAlpha;
    private readonly List<ShopCardUI>      _cardInstances  = new();
    private readonly List<ShopRemoveRowUI> _rowInstances   = new();

    // ─────────────────────────────────────────────────────────────
    //  초기화
    // ─────────────────────────────────────────────────────────────

    /// <summary>리롤을 누를 수 있을 때의 글자색 (D65). <see cref="LevelUpManager"/> 와 같은 값.</summary>
    // 🔴 <b>흰색이다</b> (D82 · 사용자 요구). `D65` 가 회색 -> 금색으로 올려 *"눌러 보이게"* 는 됐지만,
    //    상점 화면이 온통 금색(가격·골드·아이템 등급)이라 **금색끼리 묻힌다.**
    //    흰색은 이 화면에서 유일해서 버튼이 곧바로 눈에 든다.
    private static readonly Color RerollOn  = Color.white;
    private static readonly Color RerollOff = new Color(0.48f, 0.50f, 0.55f, 1f);

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // 🔴 상점에서 KILLS / TIME / LEVEL 을 뺀다 (D65 · 사용자 요구 4).
        //    상점은 "무엇을 살까"를 정하는 화면이고 이 셋은 그 결정에 아무것도 안 보탠다 —
        //    NPC 자리를 셋이서 차지하고 있었다.
        //    🔑 씬에서 지우지 않고 여기서 끈다. 나중에 되살리려면 이 세 줄만 지우면 되고,
        //       배선(SerializeField)은 그대로라 씬을 다시 만질 필요가 없다.
        if (killCountText   != null) killCountText  .gameObject.SetActive(false);
        if (elapsedTimeText != null) elapsedTimeText.gameObject.SetActive(false);
        if (playerLevelText != null) playerLevelText.gameObject.SetActive(false);
    }

    private void Start()
    {
        // ShopManager 이벤트 구독
        ShopManager.Instance.OnShopOpened.AddListener(OnShopOpened);
        ShopManager.Instance.OnShopClosed.AddListener(OnShopClosed);
        ShopManager.Instance.OnItemPurchased.AddListener(OnItemPurchased);
        ShopManager.Instance.OnItemRemoved.AddListener(OnItemRemoved);
        ShopManager.Instance.OnSlotsRerolled.AddListener(RefreshShopSlots);

        // 런 골드 변경 구독 (Action<int> 이므로 += 사용).
        // 🔴 상점은 런 골드만 쓴다 — 메타 골드(MetaProgression.Currency)가 아니다 (TODO §2-B).
        GameManager.Instance.OnRunGoldChanged += OnCurrencyChanged;

        // 버튼 연결
        rerollButton.onClick.AddListener(ShopManager.Instance.Reroll);
        closeButton .onClick.AddListener(ShopManager.Instance.CloseShop);

        // 초기 숨김
        shopRoot.SetActive(false);
        canvasGroup.alpha          = 0f;
        canvasGroup.blocksRaycasts = false;
        _targetAlpha               = 0f;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnRunGoldChanged -= OnCurrencyChanged;
    }

    private void Update()
    {
        // 부드러운 페이드 (unscaled: 상점은 Time.timeScale 영향 없음)
        if (!Mathf.Approximately(canvasGroup.alpha, _targetAlpha))
        {
            canvasGroup.alpha = Mathf.MoveTowards(
                canvasGroup.alpha, _targetAlpha, fadeSpeed * Time.unscaledDeltaTime);
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  이벤트 수신
    // ─────────────────────────────────────────────────────────────

    private void OnShopOpened()
    {
        shopRoot.SetActive(true);
        _targetAlpha               = 1f;
        canvasGroup.blocksRaycasts = true;

        RefreshAll();
    }

    private void OnShopClosed()
    {
        _targetAlpha               = 0f;
        canvasGroup.blocksRaycasts = false;
        // 페이드 완료 후 비활성화는 Update에서 처리
        Invoke(nameof(DeactivateRoot), 1f / fadeSpeed + 0.1f);
    }

    private void DeactivateRoot() => shopRoot.SetActive(false);

    private void OnItemPurchased(ShopSlot slot)
    {
        // 구매된 카드 UI만 갱신 (sold-out 표시)
        var card = _cardInstances.Find(c => c.BoundSlot == slot);
        card?.SetSoldOut();

        RefreshRemovePanel();
        RefreshRerollButton();
    }

    private void OnItemRemoved(ItemData item)
    {
        RefreshRemovePanel();
    }

    private void OnCurrencyChanged(int newAmount)
    {
        if (currencyText) currencyText.text = $"{newAmount} G";
        RefreshCardAffordability();
        RefreshRerollButton();
    }

    // ─────────────────────────────────────────────────────────────
    //  전체 갱신
    // ─────────────────────────────────────────────────────────────

    private void RefreshAll()
    {
        RefreshCurrency();
        RefreshShopSlots();
        RefreshRemovePanel();
        RefreshRerollButton();
        RefreshNPCPanel();
    }

    // ─────────────────────────────────────────────────────────────
    //  골드 표시
    // ─────────────────────────────────────────────────────────────

    private void RefreshCurrency()
    {
        int cur = GameManager.Instance.RunGold;
        if (currencyText) currencyText.text = $"{cur} G";
    }

    // ─────────────────────────────────────────────────────────────
    //  구매 슬롯
    // ─────────────────────────────────────────────────────────────

    private void RefreshShopSlots()
    {
        // 기존 카드 정리
        foreach (var c in _cardInstances) Destroy(c.gameObject);
        _cardInstances.Clear();

        int currency = GameManager.Instance.RunGold;

        foreach (var slot in ShopManager.Instance.CurrentSlots)
        {
            var card = Instantiate(shopCardPrefab, shopCardContent);
            card.Setup(slot, ShopManager.Instance, currency);
            _cardInstances.Add(card);
        }

        RefreshRerollButton();
    }

    private void RefreshCardAffordability()
    {
        int currency = GameManager.Instance.RunGold;
        foreach (var card in _cardInstances)
            card.RefreshAffordability(currency);
    }

    // ─────────────────────────────────────────────────────────────
    //  리롤 버튼
    // ─────────────────────────────────────────────────────────────

    private void RefreshRerollButton()
    {
        int cost = ShopManager.Instance.CurrentRerollCost;

        // 이모지(🔀)를 쓰지 말 것 — Pretendard SDF 에 없는 글리프라 콘솔 경고와 함께
        // 대체 문자(␡)가 그려진다. 폰트 아틀라스에 있는 글자만 쓴다.
        //
        // 🔴 색을 코드가 정한다 (D65 · 사용자 요구 1) — 씬 값이 회색이라 **누를 수 있는지가
        //    안 보였다.** 값만 바꾸면 씬 오버라이드가 이긴다(B11).
        bool can = ShopManager.Instance.CanReroll();
        rerollCostText.text          = $"REROLL  {cost} G";
        rerollCostText.color         = can ? RerollOn : RerollOff;
        rerollButton.interactable    = can;
    }

    // ─────────────────────────────────────────────────────────────
    //  제거 패널
    // ─────────────────────────────────────────────────────────────

    private void RefreshRemovePanel()
    {
        foreach (var r in _rowInstances) Destroy(r.gameObject);
        _rowInstances.Clear();

        var items = GameManager.Instance.LevelUpManager.GetInventoryItems();
        foreach (var item in items)
        {
            var row = Instantiate(removeRowPrefab, removeListContent);
            int refund = ShopManager.Instance.GetRefundAmount(item);
            row.Setup(item, refund, ShopManager.Instance);
            _rowInstances.Add(row);
        }

        // 보유 아이템이 없으면 힌트 텍스트 표시
        if (removeHintText)
            removeHintText.gameObject.SetActive(items.Count == 0);
    }

    // ─────────────────────────────────────────────────────────────
    //  NPC 패널
    // ─────────────────────────────────────────────────────────────

    private void RefreshNPCPanel()
    {
        // 랜덤 대사
        if (npcDialogueText && npcDialogues.Length > 0)
            npcDialogueText.text = npcDialogues[Random.Range(0, npcDialogues.Length)];

        // 🔴 KILLS / TIME / LEVEL 은 Awake 에서 껐다 (D65 · 사용자 요구 4).
        //    꺼진 오브젝트에 글자를 채우던 코드도 같이 걷어낸다 —
        //    남겨 두면 "왜 안 보이지" 하고 이 함수를 먼저 뒤지게 된다.
    }
}
