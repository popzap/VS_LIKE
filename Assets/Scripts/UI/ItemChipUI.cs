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
}
