using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

// ────────────────────────────────────────────────────────────────────────────
//  ShopSlot  —  상점 슬롯 런타임 데이터
// ────────────────────────────────────────────────────────────────────────────
/// <summary>
/// 상점 칸이 파는 것 (D71). 예전에는 <b>아이템뿐</b>이었다.
///
/// <para>🔴 <b>정수로 직렬화되지 않는다</b> — <see cref="ShopSlot"/> 은 런타임 전용이라
/// 애셋에 안 남는다. 그래도 순서를 지키는 편이 읽기 쉽다.</para>
/// </summary>
public enum ShopSlotKind
{
    /// <summary>무기·패시브·건물. 지금까지 유일했던 종류.</summary>
    Item,
    /// <summary>휴식 — 골드로 체력을 산다 (사용자 요구 6).</summary>
    Heal,
    /// <summary>런 골드를 메타 골드로 바꾼다 (<c>B12</c> 의 사용자 답).</summary>
    Exchange,
}

[System.Serializable]
public class ShopSlot
{
    public ShopSlotKind Kind = ShopSlotKind.Item;

    /// <summary>🔴 <see cref="ShopSlotKind.Item"/> 일 때만 채워진다. 나머지는 <c>null</c> 이다.</summary>
    public ItemData Item;

    public int      Price;
    public bool     IsSold;
    public bool     IsUpgrade;   // true = 이미 보유 중 → 레벨업 구매

    /// <summary>이 칸이 주는 양. 회복은 체력, 전환은 메타 골드.</summary>
    public int      Amount;
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

    // ── 가격 스케일 (D69 · 사용자 요구 2) ────────────────────────
    // 🔴 수치는 Economy.csv 의 ShopManager 행이 들고 있다. 여기 기본값은 자리표시다.
    [Header("가격 스케일 — 값은 Economy.csv 가 덮는다")]
    [Tooltip("0층·미보유일 때 Items.csv 의 ShopPrice 에 곱하는 값. "
           + "1 보다 작으면 초반이 싸진다 — 사용자 판정 \"상점이 너무 비쌈\" 이 초반 얘기다.")]
    [SerializeField] private float priceBaseMult = 0.6f;

    [Tooltip("이미 가진 아이템의 레벨 1당 가격 증가율. 0.35 = 레벨당 +35 %. "
           + "업그레이드가 쌓일수록 비싸진다.")]
    [SerializeField] private float priceLevelStep = 0.35f;

    [Tooltip("층 1당 가격 증가율. 0.18 = 층마다 +18 %. "
           + "후반에 골드가 남는 문제(D61 의 M=1168)를 여기서 뺀다.")]
    [SerializeField] private float priceLayerStep = 0.18f;

    // ── 서비스 칸 (D71 · 사용자 요구 6 + B12) ────────────────────
    // 🔴 수치는 Economy.csv 의 ShopManager 행이 들고 있다.
    [Header("서비스 칸 — 값은 Economy.csv 가 덮는다")]
    [Tooltip("휴식(회복) 기본 가격. 층 배율(priceLayerStep)이 그대로 곱해진다.")]
    [SerializeField] private int   healBasePrice = 35;

    [Tooltip("휴식이 회복하는 최대 체력 비율. 0.4 = 40 %.")]
    [SerializeField] private float healPercent = 0.4f;

    [Tooltip("골드 전환 한 번에 쓰는 런 골드.")]
    [SerializeField] private int   exchangeCost = 100;

    [Tooltip("전환 비율 — 런 골드 exchangeCost 를 내면 메타 골드 이만큼. "
           + "0.4 = 100 런 골드 → 40 메타 골드. 런이 끝나면 어차피 소멸하는 돈이라 "
           + "손해를 봐도 이득이지만, 1 을 넘기면 골드를 쟁이는 게 최적 전략이 된다.")]
    [SerializeField] private float exchangeRate = 0.4f;

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

    /// <summary>
    /// 칸을 산다. 종류마다 하는 일이 다르다 (D71).
    ///
    /// <para>🔴 <b><c>B12</c> 를 여기서 닫는다.</b> 예전에는 골드를 <b>먼저</b> 빼고
    /// <c>ApplyItem</c> 이 조용히 거절해도 <b>성공 로그를 찍었다</b> —
    /// 칸이 꽉 찬 카테고리의 새 아이템을 사면 <b>골드만 사라졌다.</b>
    /// 이제 <b>줄 수 있는지 먼저 묻고</b>, 못 주면 골드에 손대지 않는다.</para>
    /// </summary>
    public void Purchase(ShopSlot slot)
    {
        if (!CanPurchase(slot)) return;

        // 🔴 골드를 빼기 전에 판정한다. 순서를 뒤집으면 그게 B12 다.
        if (slot.Kind == ShopSlotKind.Item)
        {
            var lm = GameManager.Instance.LevelUpManager;
            if (slot.Item == null || !lm.CanAcquire(slot.Item) || slot.Item.IsMaxLevel)
            {
                Debug.LogWarning($"[ShopManager] 구매 거절 — '{(slot.Item != null ? slot.Item.ItemName : "null")}' 을(를) 지금 받을 수 없다. 골드는 그대로다");
                return;
            }
        }

        GameManager.Instance.SpendRunGold(slot.Price);
        slot.IsSold = true;

        switch (slot.Kind)
        {
            case ShopSlotKind.Item:
                GameManager.Instance.LevelUpManager.ApplyItemFromShop(slot.Item);
                Debug.Log($"[ShopManager] 구매: {slot.Item.ItemName} ({slot.Price}G 소모)");
                break;

            case ShopSlotKind.Heal:
            {
                var ps = PlayerStats.Current;
                if (ps != null) ps.Heal(slot.Amount);
                Debug.Log($"[ShopManager] 휴식: 체력 +{slot.Amount} ({slot.Price}G 소모)");
                break;
            }

            case ShopSlotKind.Exchange:
                // 🔑 런 골드는 런이 끝나면 소멸한다. 손해 보는 환율이어도 안 바꾸는 것보다 낫다.
                GameManager.Instance.AddPendingMetaGold(slot.Amount);
                Debug.Log($"[ShopManager] 골드 전환: 메타 +{slot.Amount} ({slot.Price}G 소모)");
                break;
        }

        OnItemPurchased.Invoke(slot);
    }

    // ─────────────────────────────────────────────────────────────
    //  아이템 제거
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// 지금 이 아이템의 상점 가격 (D69 · 사용자 요구 2).
    ///
    /// <para>사용자 판정: *"상점이 너무 비쌈 (해당 아이템의 현재 레벨, 스테이지 단계에 따라
    /// 비싸지는 방식으로)"*. 요구가 둘로 갈린다 — <b>싸게</b> 그리고 <b>비싸지게</b>.
    /// 모순이 아니다: 실측이 그 둘을 따로 가리키고 있었다.</para>
    ///
    /// <para>🔑 <c>D61</c> 의 두 판이 정확히 반대였다 —
    /// <b>3노드 사망 판은 `M=89`</b>(초반엔 돈이 없어 못 산다),
    /// <b>9노드 완주 판은 `M=1168`</b>(후반엔 남아돈다). 고정가 하나로는 둘 다 못 맞춘다.
    /// 그래서 <b>기준가를 내리고(0.6배) 층·레벨로 올린다.</b></para>
    ///
    /// <para>🔴 <b>보유 레벨은 <c>ItemData.CurrentLevel</c> 로 본다.</b>
    /// 안 가진 아이템은 0 이라 배율이 1 이 되고, 그게 *"처음 사는 건 싸다"* 다.</para>
    /// </summary>
    public int GetPrice(ItemData item)
    {
        if (item == null) return 0;

        int level = Mathf.Max(0, item.CurrentLevel);

        // 🔴 <b>LayerScaling.Layer 를 쓰면 한 층 뒤처진다.</b> 그 값은 WaveManager.StartWave 에서만
        //    갱신되는데, 상점 노드는 웨이브를 안 돈다 — 3층 상점이 2층 가격으로 팔린다.
        //    지금 들어와 있는 노드의 Layer 를 직접 본다. 노드가 없을 때(창 밖에서 값만 물어볼 때)만
        //    전역값으로 떨어진다.
        int layer = _currentNode != null
            ? Mathf.Max(0, _currentNode.Layer)
            : Mathf.Max(0, LayerScaling.Layer);
        float mult  = priceBaseMult
                    * (1f + priceLevelStep * level)
                    * (1f + priceLayerStep * layer);

        // 🔴 최소 1. 0 이 되면 "공짜 아이템"이 생겨 상점이 무의미해진다.
        return Mathf.Max(1, Mathf.RoundToInt(item.ShopPrice * mult));
    }

    /// <summary>골드 환급 금액을 반환한다 (<see cref="GetPrice"/> × refundRate, 최소 1).</summary>
    public int GetRefundAmount(ItemData item)
        // 🔑 고정가가 아니라 **지금 가격**의 절반이다 (D69). 안 그러면 깊은 층에서
        //    비싸게 산 것을 싸게 되팔게 되고, 반대로 초반엔 산 값보다 비싸게 팔린다.
        => Mathf.Max(1, Mathf.RoundToInt(GetPrice(item) * refundRate));

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

    /// <summary>
    /// 진열을 다시 뽑는다 (D71 로 크게 바뀌었다).
    ///
    /// <para>🔑 <b>휴식 칸은 항상 하나 있다</b> (사용자 요구 6). 남은 자리에 아이템을 채우고,
    /// <b>줄 수 있는 아이템이 모자라면 골드 전환으로 메운다</b> (<c>B12</c> 의 사용자 답).</para>
    ///
    /// <para>🔴 예전에는 <c>GetShopCandidates</c> 가 준 것을 그대로 진열했다.
    /// 그 목록은 <b>지금 받을 수 있는지를 안 본다</b> — 칸이 꽉 찬 카테고리의 새 아이템도,
    /// 이미 만렙인 것도 그냥 올라왔다. 그게 <c>B12</c> 의 절반이었다.</para>
    ///
    /// <para>🔑 사용자 답이 좋았던 이유: 앞서 적어 둔 세 안은 전부 *"못 사게 막는다"* 라
    /// <b>상점 노드를 밟은 게 헛수고</b>가 된다. 이쪽은 <b>살 게 없으면 다른 걸 판다.</b></para>
    /// </summary>
    private void RollShopSlots()
    {
        CurrentSlots.Clear();

        var levelUp = GameManager.Instance.LevelUpManager;

        // 🔴 CanAcquire 만으로는 부족하다 — 이미 가진 아이템은 무조건 true 를 돌려주므로
        //    만렙인 것까지 통과한다. 살 수 있다 = 받을 수 있고 + 아직 올릴 수 있다.
        var candidates = levelUp.GetShopCandidates(shopSlotCount * 3)
                                .Where(i => i != null && levelUp.CanAcquire(i) && !i.IsMaxLevel)
                                .Take(shopSlotCount)
                                .ToList();

        foreach (var item in candidates)
        {
            CurrentSlots.Add(new ShopSlot
            {
                Kind      = ShopSlotKind.Item,
                Item      = item,
                Price     = GetPrice(item),
                IsSold    = false,
                IsUpgrade = levelUp.HasItem(item) && !item.IsMaxLevel
            });
        }

        // 🔴 <b>휴식·전환은 살 아이템이 모자랄 때만 나온다</b> (D72 에서 바로잡았다).
        //    D71 은 휴식을 <b>항상</b> 한 칸 두었는데, 사용자 의도는
        //    *"더이상 구매 못하는 상황에서 뜨게"* 였다 — 아이템 칸을 상시로 하나 빼앗으면
        //    살 게 있는 판에서도 선택지가 줄어든다.
        //
        // 🔑 빈자리의 <b>첫 칸이 휴식</b>이다. 체력이 없으면 골드도 의미가 없으므로
        //    회복을 전환보다 먼저 보여 준다.
        for (int i = candidates.Count; i < shopSlotCount; i++)
            CurrentSlots.Add(i == candidates.Count ? MakeHealSlot() : MakeExchangeSlot());
    }

    /// <summary>휴식 칸 (사용자 요구 6). 층이 깊을수록 비싸다 — 가격 규칙을 아이템과 맞춘다.</summary>
    private ShopSlot MakeHealSlot()
    {
        var ps  = PlayerStats.Current;
        int amount = ps != null ? Mathf.Max(1, Mathf.RoundToInt(ps.Final.MaxHp * healPercent)) : 1;

        int layer = _currentNode != null ? Mathf.Max(0, _currentNode.Layer)
                                         : Mathf.Max(0, LayerScaling.Layer);
        int price = Mathf.Max(1, Mathf.RoundToInt(healBasePrice * (1f + priceLayerStep * layer)));

        return new ShopSlot { Kind = ShopSlotKind.Heal, Price = price, Amount = amount };
    }

    /// <summary>골드 전환 칸. 🔑 층 배율을 <b>안</b> 붙인다 — 환율이 층마다 나빠질 이유가 없다.</summary>
    private ShopSlot MakeExchangeSlot()
    {
        int cost = Mathf.Max(1, exchangeCost);
        return new ShopSlot
        {
            Kind   = ShopSlotKind.Exchange,
            Price  = cost,
            Amount = Mathf.Max(1, Mathf.RoundToInt(cost * exchangeRate)),
        };
    }
}
