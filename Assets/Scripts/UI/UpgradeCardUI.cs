using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 메타 강화 카드 한 장 (D30).
///
/// <para><see cref="ShopCardUI"/> 와 모양이 비슷하지만 합치지 않았다 — 파는 물건이 다르다.
/// 상점은 <b>이번 런에서만 쓰는 아이템</b>을 런 골드로 팔고, 여기는
/// <b>런을 넘어 남는 강화</b>를 메타 골드로 판다. 두 지갑은 D25 에서 갈라졌다.</para>
/// </summary>
public class UpgradeCardUI : MonoBehaviour
{
    [Header("UI 요소")]
    [SerializeField] private Image           iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private Button          buyButton;
    [SerializeField] private GameObject      maxedOverlay;

    [Header("색")]
    [SerializeField] private Color affordColor = new(0.94f, 0.75f, 0.25f);
    [SerializeField] private Color pricyColor  = new(0.60f, 0.60f, 0.60f);

    private UpgradeDefinition _def;

    public UpgradeDefinition Bound => _def;

    /// <param name="onBuy">구매 시도. 성공 여부는 화면이 다시 그리며 판단한다.</param>
    public void Setup(UpgradeDefinition def, int level, int currency, System.Action<UpgradeDefinition> onBuy)
    {
        _def = def;

        if (iconImage != null)
        {
            // 아이콘이 아직 없다 (Upgrades.csv 의 Icon 열이 비어 있다).
            // 빈 Image 를 그대로 두면 흰 사각형이 남으므로 통째로 끈다.
            iconImage.sprite  = def.Icon;
            iconImage.enabled = def.Icon != null;
        }

        if (nameText != null) nameText.text = def.DisplayName;
        if (descText != null) descText.text = def.Description;

        bool maxed = level >= def.MaxLevel;

        if (levelText != null)
            levelText.text = maxed
                ? $"Lv.<color=#F0C040>{level}</color> / {def.MaxLevel}"
                : $"Lv.{level} → <color=#F0C040>{level + 1}</color>  / {def.MaxLevel}";

        int cost = maxed ? 0 : def.GetCost(level);

        if (priceText != null)
        {
            priceText.text  = maxed ? "MAX" : $"{cost} G";
            priceText.color = maxed || currency >= cost ? affordColor : pricyColor;
        }

        if (maxedOverlay != null) maxedOverlay.SetActive(maxed);

        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.interactable = !maxed && currency >= cost;
            buyButton.onClick.AddListener(() =>
            {
                AudioManager.Play(SfxId.UiSelect);
                onBuy?.Invoke(def);
            });
        }
    }
}
