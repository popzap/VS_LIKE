using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ────────────────────────────────────────────────────────────────────────────
//  LevelUpManager  —  레벨업 패널 표시 및 선택 처리
// ────────────────────────────────────────────────────────────────────────────
public class LevelUpManager : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private GameObject       levelUpPanel;
    [SerializeField] private ItemCardUI[]      cards;          // 3장 고정
    [SerializeField] private Button           rerollButton;
    [SerializeField] private TextMeshProUGUI  rerollCostText;

    [Header("아이템 데이터베이스")]
    [SerializeField] private ItemData[]       allItems;        // 모든 ItemData 등록

    [Header("리롤 설정")]
    [Tooltip("리롤 1회 비용(골드). 레벨업 1회당 리롤은 한 번만 가능하다.")]
    [SerializeField] private int rerollCost = 1;

    // 런타임 인벤토리 (현재 런에서 보유 중인 아이템)
    private readonly Dictionary<ItemData, int> _inventory = new();  // item → level
    private List<ItemData> _currentChoices = new();
    private bool _rerollUsed;

    // 진화처럼 "후보를 미리 정해 놓고 띄운" 경우의 선택 처리기.
    // null 이면 평범한 레벨업이라 고른 아이템을 그대로 적용한다.
    private System.Action<ItemData> _forcedChoiceHandler;

    // ── Public API ───────────────────────────────────────────────

    /// <summary>
    /// 새 런을 시작할 때 호출. 인벤토리를 비우고 <b>ItemData 애셋의 CurrentLevel 도</b> 초기화한다.
    ///
    /// CurrentLevel 은 ScriptableObject 애셋에 직접 쓰이는 런타임 값이라
    /// <c>[NonSerialized]</c> 라도 도메인 리로드 전까지 살아남는다.
    /// 씬만 다시 로드하는 Retry 경로에서는 초기화되지 않아 이전 런의 레벨이 이월된다.
    /// </summary>
    public void ResetRunState()
    {
        _inventory.Clear();
        _currentChoices.Clear();
        _rerollUsed = false;

        if (allItems == null) return;
        foreach (var item in allItems)
            if (item != null) item.CurrentLevel = 0;
    }

    public void ShowLevelUpPanel()
    {
        _forcedChoiceHandler = null;   // 진화 패널이 취소된 채 남아 있으면 다음 선택을 가로챈다
        _rerollUsed     = false;       // 리롤 횟수는 레벨업 1회마다 초기화된다
        _currentChoices = PickItems(cards.Length);
        RefreshPanel();
        levelUpPanel.SetActive(true);
    }

    /// <summary>
    /// 후보를 <b>강제로 지정해</b> 같은 패널을 띄운다 (진화 제안용).
    ///
    /// <para>고른 카드는 <paramref name="onSelected"/> 로 넘어간다 — 진화는 결과 아이템을
    /// 주기 전에 재료를 소모해야 하므로 <see cref="ApplyItem"/> 를 그냥 태울 수 없다.
    /// 리롤은 막는다. 후보가 한 장뿐인데 다시 뽑는다는 개념이 성립하지 않는다.</para>
    /// </summary>
    public void ShowForcedChoices(List<ItemData> choices, System.Action<ItemData> onSelected)
    {
        _forcedChoiceHandler = onSelected;
        _rerollUsed          = true;
        _currentChoices      = choices;
        RefreshPanel();
        levelUpPanel.SetActive(true);
    }

    public void HidePanel()
    {
        levelUpPanel.SetActive(false);
        GameManager.Instance.OnLevelUpCompleted();
    }

    // 카드 선택 (ItemCardUI 버튼에서 호출)
    public void SelectItem(ItemData item)
    {
        // 처리기를 먼저 비운 뒤에 부른다 — 안에서 예외가 나도 다음 레벨업까지 물고 늘어지지 않게.
        var handler = _forcedChoiceHandler;
        _forcedChoiceHandler = null;

        if (handler != null) handler(item);
        else                 ApplyItem(item);

        HidePanel();
    }

    // 리롤 버튼 — 레벨업 1회당 한 번만 쓸 수 있다.
    public void OnRerollClicked()
    {
        if (_rerollUsed) return;

        // 리롤 비용은 런 골드다 (TODO §2-B).
        if (!GameManager.Instance.SpendRunGold(rerollCost)) return;

        _rerollUsed     = true;
        _currentChoices = PickItems(cards.Length);
        RefreshPanel();

        // 카드 선택(UiSelect)과 갈라 놓는다 — 다시 뽑은 것이지 고른 것이 아니다.
        AudioManager.Play(SfxId.UiCancel);
    }

    // ── 아이템 적용 ──────────────────────────────────────────────

    private void ApplyItem(ItemData item)
    {
        // 슬롯이 꽉 찬 카테고리의 신규 아이템은 여기서 막는다.
        //
        // 예전에는 이 검사가 없어서, 무기 슬롯이 찬 상태로 새 무기를 고르면
        // _inventory 에는 기록되고 WeaponManager 는 경고만 남긴 채 거절해
        // **"보유 목록에는 있는데 무기는 없는"** 상태가 됐다. 그 아이템은 이후
        // 보유로 취급돼 카드 가중치가 2배가 되고 진화 재료 판정까지 통과한다.
        if (!CanAcquire(item)) return;

        if (!_inventory.ContainsKey(item)) _inventory[item] = 0;
        _inventory[item]++;
        item.CurrentLevel = _inventory[item];

        switch (item.Category)
        {
            case ItemCategory.Weapon:
                WeaponManager.Instance.AddOrUpgradeWeapon(item.WeaponRef, item.CurrentLevel);
                break;
            case ItemCategory.Building:
                GameManager.Instance.BuildingMgr.UnlockBuilding(item.BuildingRef, item.CurrentLevel);
                break;
            case ItemCategory.Passive:
                ApplyPassive(item.PassiveRef, item.CurrentLevel);
                break;
        }
    }

    private void ApplyPassive(PassiveData data, int level)
    {
        var player = FindFirstObjectByType<PlayerStats>();
        if (player == null) return;

        // 같은 패시브를 다시 고른 것이면 레벨만 올린다. 새 인스턴스를 추가하면
        // 이전 레벨 값까지 전부 합산돼 버린다 (I-32).
        player.AddOrUpgradePassive(data, level);
    }

    // ── 아이템 선택 알고리즘 ─────────────────────────────────────

    /// <summary>
    /// 보유 아이템 업그레이드와 미보유 신규 아이템을 <b>한 풀에 섞어</b> 뽑는다.
    ///
    /// 예전에는 보유 업그레이드로 슬롯을 먼저 다 채우고 남은 자리에만 신규를 넣었다.
    /// 그래서 최대레벨이 아닌 아이템을 3개만 들고 있어도 카드 3장이 전부 그 3개로
    /// 고정되어 "리롤을 안 돌리면 계속 같은 것만 나온다"는 상태가 됐다.
    ///
    /// 지금은 가중치 추첨(비복원)이다. 보유 아이템은 <see cref="OwnedWeight"/> 배
    /// 더 자주 나와서 육성은 계속 가능하되, 신규 아이템도 항상 섞여 들어온다.
    /// </summary>
    private const float OwnedWeight = 2f;

    private List<ItemData> PickCandidates(int count)
    {
        // 후보 = 미보유 전체 + 보유 중 최대레벨이 아닌 것
        var pool = new List<ItemData>();
        foreach (var item in allItems)
        {
            if (item == null) continue;
            if (_inventory.ContainsKey(item) && item.IsMaxLevel) continue;
            if (!CanAcquire(item)) continue;   // 꽉 찬 카테고리의 신규 아이템은 카드에 띄우지 않는다
            pool.Add(item);
        }

        var result = new List<ItemData>(count);
        while (result.Count < count && pool.Count > 0)
        {
            float total = 0f;
            foreach (var item in pool) total += Weight(item);

            float roll = Random.value * total;
            int picked = pool.Count - 1;
            for (int i = 0; i < pool.Count; i++)
            {
                roll -= Weight(pool[i]);
                if (roll <= 0f) { picked = i; break; }
            }

            result.Add(pool[picked]);
            pool.RemoveAt(picked);      // 비복원 — 같은 카드가 두 장 나오지 않는다
        }
        return result;
    }

    private float Weight(ItemData item) => _inventory.ContainsKey(item) ? OwnedWeight : 1f;

    private List<ItemData> PickItems(int count) => PickCandidates(count);

    // ── UI 갱신 ──────────────────────────────────────────────────

    private void RefreshPanel()
    {
        for (int i = 0; i < cards.Length; i++)
        {
            bool hasItem = i < _currentChoices.Count;
            cards[i].gameObject.SetActive(hasItem);
            if (hasItem) cards[i].Setup(_currentChoices[i], this);
        }
        if (_rerollUsed)
        {
            rerollCostText.text       = "Reroll used";
            rerollButton.interactable = false;
        }
        else
        {
            rerollCostText.text       = $"Reroll ({rerollCost}G)  x1";
            rerollButton.interactable = GameManager.Instance.RunGold >= rerollCost;
        }
    }

    // ── 상점 공개 API ────────────────────────────────────────────

    /// <summary>상점에서 아이템을 적용할 때 사용 (레벨업과 동일 로직).</summary>
    public void ApplyItemFromShop(ItemData item) => ApplyItem(item);

    /// <summary>현재 런에서 해당 아이템을 보유 중인지 확인.</summary>
    public bool HasItem(ItemData item) => _inventory.ContainsKey(item);

    // ── 소지 상한 ────────────────────────────────────────────────

    /// <summary>
    /// 이 아이템을 지금 얻을 수 있는가. <b>이미 보유 중이면 항상 참</b>이다 —
    /// 상한은 "칸을 새로 차지하는가"만 막지 레벨업을 막지 않는다.
    ///
    /// <para>레벨업 카드와 상점 진열은 둘 다 <see cref="PickCandidates"/> 를 지나므로
    /// 여기 한 곳만 막으면 두 경로가 같이 잡힌다.</para>
    /// </summary>
    public bool CanAcquire(ItemData item)
    {
        if (item == null) return false;
        if (_inventory.ContainsKey(item)) return true;

        var player = PlayerStats.Current;
        if (player == null) return true;

        return CountOwned(item.Category) < player.SlotLimit(item.Category);
    }

    /// <summary>보유 중인 <b>종류 수</b>. 레벨은 세지 않는다.</summary>
    public int CountOwned(ItemCategory category)
    {
        int n = 0;
        foreach (var pair in _inventory)
            if (pair.Key != null && pair.Key.Category == category) n++;
        return n;
    }

    /// <summary>보유 레벨. 없으면 0. 진화 조건 판정이 쓴다.</summary>
    public int GetItemLevel(ItemData item)
        => item != null && _inventory.TryGetValue(item, out int lv) ? lv : 0;

    /// <summary>진화 결과를 지급한다 (레벨업/상점과 같은 경로).</summary>
    public void GrantEvolvedItem(ItemData item) => ApplyItem(item);

    /// <summary>
    /// 시작 무기를 <b>장부에 등록하면서</b> 지급한다 (B6).
    ///
    /// <para>🔴 예전에는 <see cref="GameManager"/> 가 <see cref="WeaponManager"/> 를 직접 불렀다.
    /// 그래서 무기 개수를 세는 장부가 <b>둘</b>이 됐다 — <c>_inventory</c> 는 시작 무기를 모르고
    /// <c>WeaponManager._weapons</c> 는 안다. 매 판 <c>_weapons.Count == CountOwned(Weapon) + 1</c>
    /// 이라 <see cref="CanAcquire"/> 가 "자리 있다"고 한 걸 <c>AddOrUpgradeWeapon</c> 이 거절했다.</para>
    ///
    /// <para>개수만 어긋난 게 아니다. 장부에 없으니 시작 무기는 레벨업 카드에 <b>미보유 신규</b>로
    /// 다시 떠서 슬롯을 한 칸 더 먹고, <see cref="HasItem"/>·<see cref="GetItemLevel"/> 이
    /// 0 을 돌려줘 <b>진화 재료 판정에서도 빠졌다.</b> 넷 다 같은 원인이라 여기서 한 번에 닫는다.</para>
    ///
    /// <para>⚠️ <see cref="ResetRunState"/> <b>뒤에</b> 불러야 한다. 먼저 부르면 지워진다
    /// (<c>GameManager.StartRun</c> 이 그 순서로 부른다).</para>
    /// </summary>
    public void GrantStartingWeapon(WeaponData weapon, int level)
    {
        if (weapon == null) return;

        var item = FindWeaponItem(weapon);
        if (item == null)
        {
            // 대응 ItemData 가 없으면 장부에 올릴 방법이 없다. 무기는 주되 어긋남이
            // 되살아난 것이므로 반드시 로그로 드러나야 한다 (B6 재발 감지).
            Debug.LogWarning($"[LevelUpManager] 시작 무기 '{weapon.WeaponName}' 에 대응하는 ItemData 가 " +
                             $"allItems 에 없다 — 장부를 우회해 직접 지급한다 (B6 어긋남 재발).");
            WeaponManager.Instance?.AddOrUpgradeWeapon(weapon, level);
            return;
        }

        // ApplyItem 을 태우지 않는다 — 그쪽은 "한 레벨 올린다"라서 StartingWeaponLevel 이
        // 2 이상이면 맞지 않는다. 여기는 레벨을 그대로 박는다.
        _inventory[item]  = level;
        item.CurrentLevel = level;
        WeaponManager.Instance?.AddOrUpgradeWeapon(weapon, level);
    }

    /// <summary><paramref name="weapon"/> 을 가리키는 무기 <see cref="ItemData"/> 를 찾는다. 없으면 null.</summary>
    private ItemData FindWeaponItem(WeaponData weapon)
    {
        if (allItems == null) return null;
        foreach (var item in allItems)
            if (item != null && item.Category == ItemCategory.Weapon && item.WeaponRef == weapon)
                return item;
        return null;
    }

    /// <summary>
    /// 아이템을 인벤토리에서 완전히 제거하고 각 시스템에서 효과를 해제한다.
    /// 골드 환급은 ShopManager가 담당한다.
    /// </summary>
    public void RemoveItemFull(ItemData item)
    {
        if (!_inventory.ContainsKey(item)) return;
        _inventory.Remove(item);
        item.CurrentLevel = 0;

        switch (item.Category)
        {
            case ItemCategory.Weapon:
                WeaponManager.Instance?.RemoveWeapon(item.WeaponRef);
                break;
            case ItemCategory.Building:
                GameManager.Instance.BuildingMgr?.LockBuilding(item.BuildingRef);
                break;
            case ItemCategory.Passive:
                FindFirstObjectByType<PlayerStats>()?.RemovePassiveByData(item.PassiveRef);
                break;
        }
    }

    /// <summary>
    /// 상점 후보 아이템 목록을 반환한다.
    /// 우선순위: 보유 중 업그레이드 가능 → 미보유 전체 DB에서 랜덤.
    /// </summary>
    public List<ItemData> GetShopCandidates(int count) => PickCandidates(count);

    public List<ItemData> GetInventoryItems() => new(_inventory.Keys);

    /// <summary>
    /// 배선된 아이템 DB 전체. <c>DevPanel</c> 이 목록을 그리는 데 쓴다
    /// (그쪽은 에디터/개발 빌드 전용이라 cref 로 걸지 않는다).
    /// 읽기 전용으로만 다룰 것 — 이 배열은 씬에 직렬화된 원본이다.
    /// </summary>
    public IReadOnlyList<ItemData> AllItems => allItems;
}
