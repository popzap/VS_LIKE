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

        // 🔴 보상 카드로 쓰였던 카드가 돌아올 수 있다 — 아이콘을 되살린다 (D98).
        if (iconImage) iconImage.gameObject.SetActive(true);

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(() =>
        {
            AudioManager.Play(SfxId.UiSelect);
            manager.SelectItem(data);
        });
    }

    /// <summary>
    /// <b>아이템 칸이 꽉 찼을 때</b> 대신 뜨는 보상 카드 (D98 · 사용자 요구).
    ///
    /// <para>🔑 <b>같은 카드를 쓴다.</b> <see cref="ShopCardUI.SetupService"/> 와 같은 판단이다 —
    /// 카드를 따로 만들면 두 벌을 나란히 유지해야 하고, 지금 필요한 차이는 <b>글자와 아이콘 유무</b>뿐이다.</para>
    ///
    /// <para>🔴 아이콘은 <b>끈다.</b> 안 끄면 앞 카드의 아이템 그림이 그대로 붙어 보인다.</para>
    ///
    /// <para>🔴 문자열은 영문이다 — 폰트가 Static 115자라 한글 글리프가 없다 (I-60).</para>
    /// </summary>
    public void SetupBonus(LevelUpBonusKind kind, int amount, LevelUpManager manager)
    {
        if (iconImage) iconImage.gameObject.SetActive(false);

        // 🔑 숫자를 이름 자리에 크게 둔다 — 이 카드에서 알고 싶은 건 "뭘 얼마나" 하나뿐이다.
        nameText.text = kind switch
        {
            LevelUpBonusKind.Gold => $"+{amount} G",
            LevelUpBonusKind.Heal => $"+{amount} HP",
            _                     => $"-{amount}s"
        };
        descText.text = kind switch
        {
            LevelUpBonusKind.Gold => "Run gold, right now.",
            LevelUpBonusKind.Heal => "Patch up on the spot.",
            _                     => "Ends this wave sooner."
        };
        categoryTag.text = "BONUS";

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(() =>
        {
            AudioManager.Play(SfxId.UiSelect);
            manager.SelectBonus(kind);
        });
    }
}
