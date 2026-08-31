using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

// ────────────────────────────────────────────────────────────────────────────
//  ShopSlot  —  상점 슬롯 런타임 데이터
// ────────────────────────────────────────────────────────────────────────────
[System.Serializable]
public class ShopSlot
{
    public ItemData Item;
    public int      Price;
    public bool     IsSold;
    public bool     IsUpgrade;   // true = 이미 보유 중 → 레벨업 구매
}

// ────────────────────────────────────────────────────────────────────────────
//  ShopManager  —  상점 노드 게임로직 (구매 / 제거 / 리롤)
// ────────────────────────────────────────────────────────────────────────────
/// <summary>
/// GameManager.OnStageNodeSelected 에서 StageType.Shop 일 때 OpenShop() 을 호출한다.
/// UI는 ShopUI 가 이벤트를 구독해서 갱신한다.
/// </summary>
public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    // ── 설정 ─────────────────────────────────────────────────────
    [Header("상점 설정")]
    [SerializeField] private int   shopSlotCount      = 3;     // 구매 슬롯 수
    [SerializeField] private int   baseRerollCost     = 3;     // 리롤 기본 비용
    [SerializeField] private int   rerollCostIncrease = 1;     // 리롤 횟수당 비용 증가
    [SerializeField] private float refundRate         = 0.5f;  // 제거 시 환급 비율

    // ── 이벤트 ────────────────────────────────────────────────────
    [Header("이벤트")]
    public UnityEvent          OnShopOpened    = new();
    public UnityEvent          OnShopClosed    = new();
    public UnityEvent<ShopSlot> OnItemPurchased = new();
    public UnityEvent<ItemData> OnItemRemoved   = new();
    public UnityEvent          OnSlotsRerolled = new();

    // ── 런타임 상태 ───────────────────────────────────────────────
    public  List<ShopSlot> CurrentSlots { get; private set; } = new();
    public  int            CurrentRerollCost => _currentRerollCost;

    private int       _currentRerollCost;
    private int       _rerollCount;
    private StageNode _currentNode;   // 상점을 연 스테이지 노드 (닫을 때 맵 진행에 필요)

    // ── 초기화 ──────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ─────────────────────────────────────────────────────────────
    //  상점 열기 / 닫기
    // ─────────────────────────────────────────────────────────────

    /// <param name="node">상점 노드. 닫을 때 이 노드를 기준으로 맵을 진행시킨다.</param>
    public void OpenShop(StageNode node)
    {
        _currentNode       = node;
        _rerollCount       = 0;
        _currentRerollCost = baseRerollCost;
        RollShopSlots();
        OnShopOpened.Invoke();
        Debug.Log("[ShopManager] 상점 열림");
    }

    public void CloseShop()
    {
        OnShopClosed.Invoke();

        // 상점도 하나의 스테이지 노드다. 진행시키지 않으면 같은 층에 계속 머무른다.
        if (_currentNode != null)
        {
            GameManager.Instance.StageMap.AdvanceToNext(_currentNode);
            _currentNode = null;
        }

        GameManager.Instance.ChangeState(GameState.StageMap);
        Debug.Log("[ShopManager] 상점 닫힘");
    }

    // ─────────────────────────────────────────────────────────────
    //  리롤
    // ─────────────────────────────────────────────────────────────

    public bool CanReroll()
        => GameManager.Instance.RunGold >= _currentRerollCost;

    public void Reroll()
    {
        if (!CanReroll()) return;

        GameManager.Instance.SpendRunGold(_currentRerollCost);
        _rerollCount++;
        _currentRerollCost = baseRerollCost + _rerollCount * rerollCostIncrease;

        RollShopSlots();
        OnSlotsRerolled.Invoke();

        // 구매(UiSelect)와 갈라 놓는다. 골드를 썼는데 확정이 아니라는 게 소리로 구분돼야 한다.
        AudioManager.Play(SfxId.UiCancel);

        Debug.Log($"[ShopManager] 리롤 (비용 누계: {_currentRerollCost}G)");
    }

    // ─────────────────────────────────────────────────────────────
    //  구매
    // ─────────────────────────────────────────────────────────────

    // 상점은 런 골드만 쓴다 (TODO §2-B). 메타 골드는 메인 메뉴의 영구 강화 몫이다.
    public bool CanPurchase(ShopSlot slot)
        => !slot.IsSold && GameManager.Instance.RunGold >= slot.Price;

    public void Purchase(ShopSlot slot)
    {
        if (!CanPurchase(slot)) return;

        GameManager.Instance.SpendRunGold(slot.Price);
        slot.IsSold = true;

        GameManager.Instance.LevelUpManager.ApplyItemFromShop(slot.Item);
        OnItemPurchased.Invoke(slot);

        Debug.Log($"[ShopManager] 구매: {slot.Item.ItemName} ({slot.Price}G 소모)");
    }

    // ─────────────────────────────────────────────────────────────
    //  아이템 제거
    // ─────────────────────────────────────────────────────────────

    /// <summary>골드 환급 금액을 반환한다 (ShopPrice × refundRate, 최소 1).</summary>
    public int GetRefundAmount(ItemData item)
        => Mathf.Max(1, Mathf.RoundToInt(item.ShopPrice * refundRate));

    /// <summary>보유 아이템을 제거하고 골드를 환급한다.</summary>
    public void RemoveItem(ItemData item)
    {
        if (!GameManager.Instance.LevelUpManager.HasItem(item))
        {
            Debug.LogWarning($"[ShopManager] 제거 실패: {item.ItemName} 미보유");
            return;
        }

        int refund = GetRefundAmount(item);
        GameManager.Instance.LevelUpManager.RemoveItemFull(item);
        GameManager.Instance.AddRunGold(refund);

        OnItemRemoved.Invoke(item);
        Debug.Log($"[ShopManager] 제거: {item.ItemName} (+{refund}G 환급)");
    }

    // ─────────────────────────────────────────────────────────────
    //  슬롯 생성
    // ─────────────────────────────────────────────────────────────

    private void RollShopSlots()
    {
        CurrentSlots.Clear();

        var levelUp    = GameManager.Instance.LevelUpManager;
        var candidates = levelUp.GetShopCandidates(shopSlotCount);

        foreach (var item in candidates)
        {
            CurrentSlots.Add(new ShopSlot
            {
                Item      = item,
                Price     = item.ShopPrice,
                IsSold    = false,
                IsUpgrade = levelUp.HasItem(item) && !item.IsMaxLevel
            });
        }
    }
}
