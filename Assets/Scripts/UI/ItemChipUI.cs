using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ────────────────────────────────────────────────────────────────────────────
//  ItemChipUI  —  보유 아이템 하나를 나타내는 작은 칩 (아이콘 + 레벨)
// ────────────────────────────────────────────────────────────────────────────
/// <summary>
/// <see cref="StatsPanelUI"/> 의 격자에 채워지는 부품.
///
/// <para>🔑 <b><c>Prefab_ItemChip</c> 은 원래부터 있었다.</b> <c>LayoutElement</c> + <c>Icon</c> +
/// <c>Label</c> 까지 갖춰져 있는데 <b>어느 스크립트도 참조하지 않아 한 번도 안 쓰였다</b>(D46 확인).
/// 새로 만들지 않고 그 프리팹에 이 스크립트만 붙였다.</para>
///
/// <para>🔴 <c>?.</c> 를 쓰지 않는다 — 미할당 직렬화 필드는 "가짜 null" 이라
/// <c>?.</c> 가 통과시키고 예외가 호출 사슬 밖으로 샌다 (I-24).
/// 여기서 새면 <b>칩 하나가 아니라 목록 전체가 안 그려진다.</b></para>
/// </summary>
public class ItemChipUI : MonoBehaviour
{
    [SerializeField] private Image           icon;
    [SerializeField] private TextMeshProUGUI label;

    /// <summary>최대 레벨 칩은 글자를 금색으로 바꾼다 — 더 올릴 수 없다는 걸 색으로 알린다.</summary>
    private static readonly Color LevelColor    = new(0.78f, 0.82f, 0.90f);
    private static readonly Color MaxLevelColor = new(1f, 0.82f, 0.35f);

    /// <summary>아이콘이 없는 아이템은 칸이 비어 보이므로, 색으로라도 분류를 알린다.</summary>
    private static readonly Color WeaponTint   = new(0.45f, 0.72f, 1f);
    private static readonly Color BuildingTint = new(1f, 0.72f, 0.35f);
    private static readonly Color PassiveTint  = new(0.45f, 0.92f, 0.60f);

    public void Bind(ItemData item, int level)
    {
        if (item == null) return;

        if (icon != null)
        {
            if (item.Icon != null)
            {
                icon.sprite = item.Icon;
                icon.color  = Color.white;
            }
            else
            {
                // 아이콘이 비어 있으면 스프라이트를 지우고 분류색 사각형으로 남긴다.
                // 지난번 칩의 그림이 남는 것보다 낫다 (칩은 풀에서 재사용된다).
                icon.sprite = null;
                icon.color  = item.Category switch
                {
                    ItemCategory.Weapon   => WeaponTint,
                    ItemCategory.Building => BuildingTint,
                    _                     => PassiveTint
                };
            }
        }

        if (label != null)
        {
            bool max = level >= item.MaxLevel;
            label.text  = max ? "MAX" : $"Lv.{level}";
            label.color = max ? MaxLevelColor : LevelColor;
        }

        name = $"Chip_{item.name}";
    }

    /// <summary>
    /// <b>빈 칸</b>으로 만든다 (D82 · HUD 아이템 줄).
    ///
    /// <para>사용자 요구는 *"상한까지 몇 개 남았는지 확인하기 쉽게"* 였다.
    /// 가진 것만 보여 주면 <b>남은 자리가 안 보인다</b> — 빈 칸을 같이 그려야
    /// "셋 중 둘 찼다"가 한눈에 읽힌다.</para>
    ///
    /// <para>🔴 <see cref="Bind"/> 는 <c>item == null</c> 이면 <b>그냥 돌아간다</b>(앞 칩의 그림이 남는다).
    /// 칩은 풀에서 재사용되므로 빈 칸은 이렇게 <b>명시적으로</b> 지워야 한다.</para>
    /// </summary>
    /// <summary>
    /// <b>아이콘만</b> 남긴다 (D82 · HUD 아이템 줄).
    ///
    /// <para>🔴 이 프리팹은 TAB 스탯 창용이라 <b>76x96</b> 이다. HUD 줄은 상한 합이
    /// 최대 <b>14칸</b>(Mage)이라 그 크기로는 <b>1116px</b> 이 필요한데 자리는 660px 뿐이다.
    /// 44px 로 줄이면 <c>Lv.N</c> 글자가 읽히지 않으므로 <b>글자를 끈다</b> —
    /// 요구도 *"박스 안에 아이콘으로"* 였고, 레벨은 TAB 창에서 본다.</para>
    /// </summary>
    public void SetCompact(bool on)
    {
        if (label != null) label.gameObject.SetActive(!on);
    }

    public void BindEmpty()
    {
        if (icon != null)
        {
            icon.sprite = null;
            icon.color  = new Color(1f, 1f, 1f, 0.10f);   // 자리만 보이는 흐린 사각형
        }
        if (label != null) label.text = string.Empty;
        name = "Chip_Empty";
    }
}
