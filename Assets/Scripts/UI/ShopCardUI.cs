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

    public void Setup(ShopSlot slot, ShopManager manager, int currentCurrency)
    {
        BoundSlot = slot;
        var item = slot.Item;

        // 기본 정보
        if (iconImage) iconImage.sprite = item.Icon;
        nameText.text    = item.ItemName;
        priceText.text   = $"{slot.Price} G";

        // 카테고리 태그
        categoryTag.text  = item.Category switch
        {
            ItemCategory.Weapon   => "무기",
            ItemCategory.Building => "건물",
            ItemCategory.Passive  => "패시브",
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
            levelText.text = "<color=#AAAAAA>신규</color>";

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
