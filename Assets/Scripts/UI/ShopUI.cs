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
        "좋은 물건만 골라왔지.\n마음에 드는 게 없으면\n리롤도 해드릴 수 있어…",
        "오늘 특가! 빨리 사지 않으면\n다음 손님이 가져가지.",
        "뭔가 찾고 있나? 내가 도와줄 수 있어.\n물론 공짜는 아니지만.",
        "강한 적들이 기다리고 있다네.\n준비를 단단히 하게나.",
        "제거 비용은 저렴해. 짐은 가볍게\n다니는 게 좋다고 생각하거든."
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

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // ShopManager 이벤트 구독
        ShopManager.Instance.OnShopOpened.AddListener(OnShopOpened);
        ShopManager.Instance.OnShopClosed.AddListener(OnShopClosed);
        ShopManager.Instance.OnItemPurchased.AddListener(OnItemPurchased);
        ShopManager.Instance.OnItemRemoved.AddListener(OnItemRemoved);
        ShopManager.Instance.OnSlotsRerolled.AddListener(RefreshShopSlots);

        // MetaProgression 골드 변경 구독 (Action<int> 이므로 += 사용)
        GameManager.Instance.MetaProgression.OnCurrencyChanged += OnCurrencyChanged;

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
        if (GameManager.Instance?.MetaProgression != null)
            GameManager.Instance.MetaProgression.OnCurrencyChanged -= OnCurrencyChanged;
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
        int cur = GameManager.Instance.MetaProgression.Currency;
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

        int currency = GameManager.Instance.MetaProgression.Currency;

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
        int currency = GameManager.Instance.MetaProgression.Currency;
        foreach (var card in _cardInstances)
            card.RefreshAffordability(currency);
    }

    // ─────────────────────────────────────────────────────────────
    //  리롤 버튼
    // ─────────────────────────────────────────────────────────────

    private void RefreshRerollButton()
    {
        int cost = ShopManager.Instance.CurrentRerollCost;
        rerollCostText.text          = $"🔀 리롤  ({cost}G)";
        rerollButton.interactable    = ShopManager.Instance.CanReroll();
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

        // 런 현황 (WaveManager / ExperienceManager 에서 조회)
        var wave = GameManager.Instance.WaveManager;
        var exp  = GameManager.Instance.ExpManager;

        if (killCountText  && wave != null) killCountText.text  = $"{wave.TotalKillCount}";
        if (playerLevelText && exp != null) playerLevelText.text = $"Lv.{exp.CurrentLevel}";

        // 경과 시간은 WaveManager 에서 누적값 제공 (없으면 "-" 표시)
        if (elapsedTimeText && wave != null)
            elapsedTimeText.text = FormatTime(wave.TotalElapsedTime);
        else if (elapsedTimeText)
            elapsedTimeText.text = "--:--";
    }

    private static string FormatTime(float seconds)
    {
        int m = (int)(seconds / 60);
        int s = (int)(seconds % 60);
        return $"{m:00}:{s:00}";
    }
}
