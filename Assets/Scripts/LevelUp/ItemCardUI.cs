using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemCardUI : MonoBehaviour
{
    [SerializeField] private Image            iconImage;
    [SerializeField] private TextMeshProUGUI  nameText;
    [SerializeField] private TextMeshProUGUI  descText;
    [SerializeField] private TextMeshProUGUI  categoryTag;
    [SerializeField] private Button           selectButton;

    public void Setup(ItemData data, LevelUpManager manager)
    {
        iconImage.sprite = data.Icon;
        nameText.text    = data.ItemName;
        descText.text    = data.GetLeveledDescription();
        categoryTag.text = data.Category.ToString().ToUpper();

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(() => manager.SelectItem(data));
    }
}
