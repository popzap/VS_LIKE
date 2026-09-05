using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 중앙 구매 카드 한 장
/// </summary>
public class ShopCardUI : MonoBehaviour
{
    [Header("UI 요소")]
    [SerializeField] private Image            iconImage;
    [SerializeField] private TextMeshProUGUI  nameText;
    [SerializeField] private TextMeshProUGUI  descText;
    [SerializeField] private TextMeshProUGUI  levelText;
    [SerializeField] private TextMeshProUGUI  categoryTag;
    [SerializeField] private TextMeshProUGUI  priceText;
    [SerializeField] private Button           buyButton;
    [SerializeField] private GameObject       soldOutOverlay;    // "구매 완료" 오버레이

    // 카테고리별 색상
    [Header("카테고리 색상")]
    [SerializeField] private Color weaponColor   = new(0.33f, 0.53f, 0.80f);
    [SerializeField] private Color passiveColor  = new(0.33f, 0.67f, 0.33f);
    [SerializeField] private Color buildingColor = new(0.80f, 0.53f, 0.20f);

    public ShopSlot BoundSlot { get; private set; }

    // ── 세팅 ─────────────────────────────────────────────────────

    [Header("서비스 칸 색상 (D71)")]
    [SerializeField] private Color serviceColor = new(0.85f, 0.72f, 0.35f);

    public void Setup(ShopSlot slot, ShopManager manager, int currentCurrency)
    {
        BoundSlot = slot;

        // 🔑 아이템이 아닌 칸(휴식·전환)은 여기서 갈라진다 (D71 · 사용자 요구 6 + B12).
        //    아이콘·카테고리·레벨처럼 **아이템에만 있는 것**을 건드리기 전에 빠져나간다 —
        //    slot.Item 이 null 이라 그대로 두면 NRE 로 카드가 통째로 안 그려진다.
        if (slot.Kind != ShopSlotKind.Item)
        {
            SetupService(slot, manager, currentCurrency);
            return;
        }

        var item = slot.Item;

        // 기본 정보
        // 🔴 켜 준다 — 서비스 칸이 껐을 수 있다. 지금은 카드를 매번 새로 만들지만,
        //    풀링으로 바뀌는 날 이 한 줄이 없으면 아이콘이 사라진 카드가 생긴다.
        if (iconImage) iconImage.gameObject.SetActive(true);
        if (iconImage) iconImage.sprite = item.Icon;
        nameText.text    = item.ItemName;
        priceText.text   = $"{slot.Price} G";

        // 카테고리 태그
        categoryTag.text  = item.Category switch
        {
            ItemCategory.Weapon   => "WEAPON",
            ItemCategory.Building => "BUILDING",
            ItemCategory.Passive  => "PASSIVE",
            _                     => ""
        };
        categoryTag.color = item.Category switch
        {
            ItemCategory.Weapon   => weaponColor,
            ItemCategory.Building => buildingColor,
            _                     => passiveColor
        };

        // 레벨 표시
        if (slot.IsUpgrade)
            levelText.text = $"Lv.<color=#F0C040>{item.CurrentLevel}</color> → {item.CurrentLevel + 1}";
        else
            levelText.text = "<color=#AAAAAA>NEW</color>";

        // 설명
        descText.text = item.Description;

        // 구매 버튼
        soldOutOverlay.SetActive(false);
        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(() =>
        {
            AudioManager.Play(SfxId.UiSelect);
            manager.Purchase(slot);
        });

        RefreshAffordability(currentCurrency);
    }

    /// <summary>
    /// 휴식 / 골드 전환 칸 (D71).
    ///
    /// <para>🔑 <b>같은 프리팹을 쓴다.</b> 카드를 따로 만들면 두 벌을 나란히 유지해야 하고,
    /// 지금 필요한 차이는 <b>글자 몇 개와 아이콘 유무</b>뿐이다.</para>
    ///
    /// <para>🔴 아이콘은 <b>끈다</b>. 아이템 아이콘이 남아 있으면 앞 카드의 그림이
    /// 그대로 붙어 보인다(프리팹을 재사용하므로).</para>
    /// </summary>
    private void SetupService(ShopSlot slot, ShopManager manager, int currentCurrency)
    {
        bool heal = slot.Kind == ShopSlotKind.Heal;

        if (iconImage) iconImage.gameObject.SetActive(false);

        nameText.text     = heal ? "Rest" : "Exchange";
        priceText.text    = $"{slot.Price} G";
        categoryTag.text  = "SERVICE";
        categoryTag.color = serviceColor;
        levelText.text    = heal ? $"<color=#F0C040>+{slot.Amount} HP</color>"
                                 : $"<color=#F0C040>+{slot.Amount} meta G</color>";
        descText.text     = heal
            ? "Patch yourself up before the next fight."
            : "Run gold vanishes when the run ends.\nCarry some of it home instead.";

        soldOutOverlay.SetActive(false);
        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(() =>
        {
            AudioManager.Play(SfxId.UiSelect);
            manager.Purchase(slot);
        });

        RefreshAffordability(currentCurrency);
    }

    /// <summary>골드 변동 시 구매 버튼 활성 여부만 갱신한다.</summary>
    public void RefreshAffordability(int currentCurrency)
    {
        if (BoundSlot == null || BoundSlot.IsSold) return;
        buyButton.interactable = currentCurrency >= BoundSlot.Price;
    }

    public void SetSoldOut()
    {
        soldOutOverlay.SetActive(true);
        buyButton.interactable = false;
    }
}
