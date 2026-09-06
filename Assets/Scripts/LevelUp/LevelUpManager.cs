using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ────────────────────────────────────────────────────────────────────────────
//  LevelUpManager  —  레벨업 패널 표시 및 선택 처리
// ────────────────────────────────────────────────────────────────────────────
/// <summary>
/// 아이템 칸이 꽉 찼을 때 대신 주는 보상 (D98 · 사용자 요구).
/// <b>상점의 <c>ShopSlotKind</c> 와 같은 발상</b>이다 — 살 게 없으면 <b>다른 걸 판다</b>(D71).
/// </summary>
public enum LevelUpBonusKind { Gold, Heal, Time }

public class LevelUpManager : MonoBehaviour
{
    // 🔴 컴파일 반영 확인용 (D27).
    public const int Version = 1;   // 1 = 칸이 꽉 찼을 때 대체 보상 (D98)

    [Header("UI 참조")]
    [SerializeField] private GameObject       levelUpPanel;
    [SerializeField] private ItemCardUI[]      cards;          // 3장 고정
    [SerializeField] private Button           rerollButton;
    [SerializeField] private TextMeshProUGUI  rerollCostText;

    [Header("아이템 데이터베이스")]
    [SerializeField] private ItemData[]       allItems;        // 모든 ItemData 등록

    /// <summary>리롤을 누를 수 있을 때의 글자색 (D65). 씬이 아니라 코드가 정본이다.</summary>
    private static readonly Color RerollOn  = new Color(0.96f, 0.80f, 0.28f, 1f);
    /// <summary>못 누를 때. 회색이되 <b>바탕과는 확실히 다른</b> 밝기로 둔다.</summary>
    private static readonly Color RerollOff = new Color(0.48f, 0.50f, 0.55f, 1f);

    [Header("리롤 설정")]
    [Tooltip("리롤 1회 비용(골드). 레벨업 1회당 리롤은 한 번만 가능하다.")]
    [SerializeField] private int rerollCost = 1;

    // ── 칸이 꽉 찼을 때의 대체 보상 (D98) — 값은 Economy.csv 가 덮는다 ──
    [Tooltip("고를 아이템이 없을 때 주는 런 골드")]
    [SerializeField] private int   fullGoldReward = 60;

    [Tooltip("고를 아이템이 없을 때 회복하는 체력")]
    [SerializeField] private int   fullHealAmount = 40;

    [Tooltip("고를 아이템이 없을 때 줄여 주는 남은 웨이브 시간(초). 킬 목표 웨이브에는 안 뜬다")]
    [SerializeField] private float fullTimeCut    = 5f;

    /// <summary>이번 패널에서 아이템 뒤에 채운 대체 보상들.</summary>
    private readonly System.Collections.Generic.List<LevelUpBonusKind> _bonuses = new();

    // 런타임 인벤토리 (현재 런에서 보유 중인 아이템)
    private readonly Dictionary<ItemData, int> _inventory = new();  // item → level
    private List<ItemData> _currentChoices = new();
    private bool _rerollUsed;

    // 진화처럼 "후보를 미리 정해 놓고 띄운" 경우의 선택 처리기.
    // null 이면 평범한 레벨업이라 고른 아이템을 그대로 적용한다.
    private System.Action<ItemData> _forcedChoiceHandler;

    /// <summary>
    /// 아직 카드를 못 준 레벨업 횟수 (B10).
    ///
    /// <para>경험치가 한 번에 크게 들어오면 <see cref="ExperienceManager.CollectXp"/> 의
    /// <c>while</c> 이 오른 레벨 수만큼 <see cref="ShowLevelUpPanel"/> 을 부른다.
    /// 예전에는 그때마다 <c>_currentChoices</c> 를 덮어써서 <b>카드가 한 장만 남았다</b> —
    /// 레벨 숫자와 스탯은 정상인데 아이템만 조용히 사라지는, 찾기 어려운 손실이었다.
    /// 이제는 세어 두고 <see cref="HidePanel"/> 에서 하나씩 소진한다.</para>
    ///
    /// <para>🔴 <b>진화 제안(<see cref="ShowForcedChoices"/>)은 이 값을 올리지 않는다.</b>
    /// 그건 레벨업이 아니라 "카드만 한 번 더 고르는 것"이라 빚이 아니다.</para>
    /// </summary>
    private int _pendingLevelUps;

    /// <summary>
    /// 지금 떠 있는 패널이 <see cref="ShowForcedChoices"/> 로 열린 것인가 (B10).
    ///
    /// <para>진화 제안 패널이 떠 있는 동안 레벨업이 들어올 수 있다. 그때 진화 카드를 고른 것을
    /// <b>레벨업 빚을 갚은 것으로 세면 대기 중인 레벨업이 그대로 사라진다.</b>
    /// 어느 종류의 패널을 닫는지 알아야 빚을 옳게 센다.</para>
    /// </summary>
    private bool _panelIsForced;

    /// <summary>
    /// 이번 패널 세션에서 <b>레벨업 빚을 하나라도 갚았나</b> (D43).
    ///
    /// <para>파동은 패널이 실제로 닫힐 때 한 번만 띄운다. 그런데 같은 패널을
    /// <see cref="ShowForcedChoices"/>(진화 제안)로 닫는 경우도 있어서,
    /// <b>레벨업으로 열린 것이었는지</b>를 따로 세야 진화 카드를 골랐을 뿐인데
    /// 레벨업 파동이 뜨는 일이 없다.</para>
    ///
    /// <para>레벨업이 여러 번 밀려 있었어도(B10 대기열) 파동은 <b>마지막에 한 번</b>이다 —
    /// 세 번 겹쳐 뜨면 그냥 지저분하다.</para>
    /// </summary>
    private bool _levelUpConsumed;

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
        _rerollUsed      = false;
        _pendingLevelUps = 0;   // 이월되면 새 런 시작하자마자 패널이 뜬다
        _panelIsForced   = false;

        if (allItems == null) return;
        foreach (var item in allItems)
            if (item != null) item.CurrentLevel = 0;
    }

    public void ShowLevelUpPanel()
    {
        _pendingLevelUps++;

        // 🔴 이미 떠 있는 패널을 덮어쓰지 않는다 (B10). 덮어쓰면 그 선택권이 사라진다.
        //    한 번의 CollectXp 로 3레벨이 오르면 여기가 세 번 불린다.
        //    ⚠️ ?. 를 쓰지 않는다 — 미할당 필드는 "가짜 null" 이다 (I-24).
        if (levelUpPanel != null && levelUpPanel.activeSelf) return;

        OpenLevelUpPanel();
    }

    /// <summary>대기열에서 하나를 꺼내 실제로 패널을 연다. 카드 재추첨은 여기서만 한다.</summary>
    private void OpenLevelUpPanel()
    {
        _forcedChoiceHandler = null;   // 진화 패널이 취소된 채 남아 있으면 다음 선택을 가로챈다
        _panelIsForced  = false;
        _rerollUsed     = false;       // 리롤 횟수는 레벨업 1회마다 초기화된다
        _currentChoices = PickItems(cards.Length);
        BuildBonuses();                      // 🔴 모자란 자리를 대체 보상으로 채운다 (D98)
        RefreshPanel();
        if (levelUpPanel != null) levelUpPanel.SetActive(true);
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
        _bonuses.Clear();                    // 🔴 진화 제안에는 대체 보상을 섞지 않는다
        _panelIsForced       = true;   // 이건 레벨업이 아니다 — 빚으로 세지 않는다 (B10)
        _rerollUsed          = true;
        _currentChoices      = choices;
        RefreshPanel();
        levelUpPanel.SetActive(true);
    }

    /// <summary>
    /// 선택이 끝났다. <b>대기 중인 레벨업이 남아 있으면 창을 닫지 않고 다음 것을 띄운다</b> (B10).
    ///
    /// <para>진화 제안으로 열린 패널은 빚을 <b>만들지도 갚지도 않는다.</b>
    /// 진화 도중에 레벨업이 들어왔다면 진화 카드를 고른 뒤 그 레벨업 카드가 이어서 뜬다.</para>
    /// </summary>
    public void HidePanel()
    {
        // 🔴 진화 제안 패널을 닫는 것은 레벨업 빚을 갚은 게 아니다.
        //    여기서 같이 세면 대기 중인 레벨업이 카드 없이 사라진다.
        if (!_panelIsForced && _pendingLevelUps > 0)
        {
            _pendingLevelUps--;
            _levelUpConsumed = true;
        }
        _panelIsForced = false;

        if (_pendingLevelUps > 0)
        {
            // 아직 갚을 게 남았다. 상태(LevelUp)와 일시정지는 그대로 두고 카드만 다시 뽑는다.
            OpenLevelUpPanel();
            return;
        }

        if (levelUpPanel != null) levelUpPanel.SetActive(false);

        // 🔴 레벨업 파동은 여기서 띄운다 (D43 · A안).
        //    HUDManager.OnLevelUp 에서 띄우면 같은 프레임에 이 패널이 열려 통째로 가린다 —
        //    캔버스가 ScreenSpaceOverlay 라 월드 스프라이트는 그 아래고 sortingOrder 로도 못 이긴다.
        //    패널을 내린 "지금"이 플레이어가 다시 월드를 보는 순간이다.
        if (_levelUpConsumed)
        {
            _levelUpConsumed = false;
            if (HUDManager.Instance != null) HUDManager.Instance.PlayLevelUpEffect();
        }

        GameManager.Instance.OnLevelUpCompleted();
    }

    /// <summary>
    /// 카드를 <b>숫자키 1·2·3</b> 으로 고른다 (D65 · 사용자 요구 7).
    ///
    /// <para>🔑 <c>Update</c> 는 <c>Time.timeScale</c> 의 영향을 받지 않는다 —
    /// 레벨업 중에는 시간이 멈춰 있으므로 <c>FixedUpdate</c> 였다면 아예 안 돌았다.</para>
    ///
    /// <para>🔴 <b>화면에 실제로 떠 있는 카드만 받는다.</b> 후보가 2장뿐인데 <c>3</c> 을
    /// 누르면 아무 일도 없어야 한다 — <c>_currentChoices</c> 길이로 자른다.</para>
    ///
    /// <para>🔴 <c>wasPressedThisFrame</c>(에지)을 쓴다. <c>isPressed</c>(레벨)로 하면
    /// 다음 레벨업 패널이 같은 눌림에 즉시 또 먹힌다 — 44레벨 판에서는 치명적이다.</para>
    /// </summary>
    private void Update()
    {
        if (levelUpPanel == null || !levelUpPanel.activeSelf) return;

        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null) return;

        var keys = new[]
        {
            UnityEngine.InputSystem.Key.Digit1,
            UnityEngine.InputSystem.Key.Digit2,
            UnityEngine.InputSystem.Key.Digit3,
        };

        int n = Mathf.Min(cards.Length, _currentChoices != null ? _currentChoices.Count : 0);
        for (int i = 0; i < n && i < keys.Length; i++)
        {
            if (!kb[keys[i]].wasPressedThisFrame) continue;
            if (!cards[i].gameObject.activeSelf) continue;   // 안 뜬 카드는 못 고른다
            SelectItem(_currentChoices[i]);
            return;
        }
    }

    // 카드 선택 (ItemCardUI 버튼에서 호출 · 숫자키 1/2/3 으로도 온다)
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

        Discover(CodexKind.Item, item);   // 도감 (D54) — 레벨업·상점·진화 결과가 전부 여길 지난다

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
            if (i < _currentChoices.Count)
            {
                cards[i].gameObject.SetActive(true);
                cards[i].Setup(_currentChoices[i], this);
                continue;
            }

            // 🔴 아이템이 모자란 자리는 <b>대체 보상</b>으로 채운다 (D98).
            //    예전에는 그냥 껐다 — 칸이 꽉 차면 <b>카드가 한 장도 없는 빈 패널</b>이 떴다.
            int b = i - _currentChoices.Count;
            bool hasBonus = b < _bonuses.Count;
            cards[i].gameObject.SetActive(hasBonus);
            if (hasBonus) cards[i].SetupBonus(_bonuses[b], BonusAmount(_bonuses[b]), this);
        }
        // 🔴 색을 코드가 정한다 (D65 · 사용자 요구 1). 씬에 회색으로 박혀 있어서
        //    **누를 수 있는 버튼인지 아닌지가 안 보였다.** 씬 값이 이기면 또 회색이 된다(B11).
        //    누를 수 있을 때 노랑, 못 누를 때 회색 — 차이가 색으로 먼저 읽힌다.
        if (_rerollUsed)
        {
            rerollCostText.text       = "REROLL  used";
            rerollCostText.color      = RerollOff;
            rerollButton.interactable = false;
        }
        else
        {
            bool can = GameManager.Instance.RunGold >= rerollCost;
            rerollCostText.text       = $"REROLL  {rerollCost} G  (x1)";
            rerollCostText.color      = can ? RerollOn : RerollOff;
            rerollButton.interactable = can;
        }
    }

    // ── 칸이 꽉 찼을 때의 대체 보상 (D98) ───────────────────────

    /// <summary>
    /// 아이템이 모자란 자리를 채울 보상을 정한다.
    ///
    /// <para>🔴 <b>사용자가 본 것은 빈 패널이었다</b> — 칸이 꽉 차면 <see cref="PickItems"/> 가
    /// 0장을 돌려주고 <see cref="RefreshPanel"/> 이 카드를 전부 꺼서
    /// <c>LEVEL UP</c> 글자와 리롤 버튼만 남았다. 레벨업을 했는데 <b>아무것도 못 받는다.</b></para>
    ///
    /// <para>🔑 <b>상점이 이미 같은 문제를 풀었다</b> (D71) — 살 게 없으면 휴식·환전을 판다.
    /// 여기도 같은 발상이다: <b>고를 게 없으면 다른 걸 준다.</b></para>
    ///
    /// <para>🔴 <b>시간 단축은 조건부다.</b> 킬 목표 웨이브에는 타이머가 아예 없어서
    /// 줄일 것이 없다 — 그런 웨이브에서는 <b>내놓지 않는다.</b> 못 쓰는 선택지를 보여 주는 건
    /// 아무것도 안 주는 것보다 나쁘다.</para>
    /// </summary>
    private void BuildBonuses()
    {
        _bonuses.Clear();

        int missing = cards.Length - _currentChoices.Count;
        if (missing <= 0) return;

        _bonuses.Add(LevelUpBonusKind.Gold);
        _bonuses.Add(LevelUpBonusKind.Heal);

        var wm = GameManager.Instance != null ? GameManager.Instance.WaveManager : null;
        if (wm != null && wm.HasWaveTimer) _bonuses.Add(LevelUpBonusKind.Time);

        // 카드 수보다 많이 만들지 않는다
        while (_bonuses.Count > missing) _bonuses.RemoveAt(_bonuses.Count - 1);
    }

    /// <summary>카드에 적힐 숫자. 화면과 실제 지급이 <b>같은 값</b>을 쓰게 한 곳에서 낸다.</summary>
    private int BonusAmount(LevelUpBonusKind kind) => kind switch
    {
        LevelUpBonusKind.Gold => fullGoldReward,
        LevelUpBonusKind.Heal => fullHealAmount,
        _                     => Mathf.RoundToInt(fullTimeCut)
    };

    /// <summary>
    /// 대체 보상 카드를 골랐다. <see cref="SelectItem"/> 과 <b>같은 마무리</b>를 탄다 —
    /// 그래야 대기 중인 레벨업(<c>B10</c>)이 이어서 뜬다.
    /// </summary>
    public void SelectBonus(LevelUpBonusKind kind)
    {
        var gm = GameManager.Instance;

        switch (kind)
        {
            case LevelUpBonusKind.Gold:
                if (gm != null) gm.AddRunGold(fullGoldReward);
                break;

            case LevelUpBonusKind.Heal:
                // 🔴 ?. 를 쓰지 않는다 (I-24). 죽는 순간과 겹치면 null 일 수 있다.
                var ps = PlayerStats.Current;
                if (ps != null) ps.Heal(fullHealAmount);
                break;

            case LevelUpBonusKind.Time:
                var wm = gm != null ? gm.WaveManager : null;
                // 🔴 고르는 사이에 웨이브가 끝났을 수 있다 — 실패하면 조용히 넘어간다.
                if (wm != null && !wm.TryCutRemainingTime(fullTimeCut))
                    Debug.LogWarning("[LevelUp] 시간 단축을 못 넣었다 — 타이머 웨이브가 아니다 (D98)");
                break;
        }

        Debug.Log($"[LevelUp] 대체 보상 — {kind} {BonusAmount(kind)}");
        HidePanel();
    }

    // ── 상점 공개 API ────────────────────────────────────────────

    /// <summary>상점에서 아이템을 적용할 때 사용 (레벨업과 동일 로직).</summary>
    public void ApplyItemFromShop(ItemData item) => ApplyItem(item);

    /// <summary>현재 런에서 해당 아이템을 보유 중인지 확인.</summary>
    /// <summary>
    /// 지금 보유한 아이템과 레벨. <see cref="StatsPanelUI"/> 가 TAB 창에 그린다 (D46).
    ///
    /// <para>🔴 <b>읽기 전용으로 낸다.</b> 밖에서 고치면 <c>ItemData.CurrentLevel</c> 과
    /// 어긋나고, 그건 ScriptableObject 애셋에 쓰이는 값이라 <b>다음 런까지 남는다.</b></para>
    /// </summary>
    public IReadOnlyDictionary<ItemData, int> Inventory => _inventory;

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

        // 🔴 이 경로는 ApplyItem 을 안 태운다(위 주석) — 그래서 발견 기록도 여기서 따로 남긴다.
        //    안 하면 "매 판 들고 시작하는 무기가 도감에서 영원히 ???" 가 된다.
        Discover(CodexKind.Item, item);
    }


    // ── 도감 발견 (D54) ─────────────────────────────────────────
    //
    // 🔴 GameManager.Instance.MetaProgression 을 Awake 에서 캐시하지 않는다 (I-8 / I-38).
    //    그 참조는 GameManager.Start 에서 채워지는데 모든 Awake 는 모든 Start 보다 먼저 돈다 —
    //    캐시하면 null 이 잡히고도 예외가 안 나서 기능만 조용히 죽는다.
    private static void Discover(CodexKind kind, UnityEngine.Object asset)
    {
        if (asset == null) return;
        var gm = GameManager.Instance;
        if (gm == null || gm.MetaProgression == null) return;   // 🔴 ?. 금지 (I-24)
        gm.MetaProgression.Discover(kind, asset.name);
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
