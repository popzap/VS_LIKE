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
    // 🔴 컴파일 반영 확인용 (D27).
    public const int Version = 1;   // 1 = 테두리를 별도 자식으로 분리 (D90)

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

        // 🔑 <b>아이템이 들어와도 테두리는 남는다</b> (D90 · 사용자 요구).
        //    예전에는 <see cref="icon"/> 하나가 테두리 노릇도 했기 때문에 아이콘을 그리는 순간
        //    <b>테두리가 사라져</b> 찬 칸만 줄에서 튀어 보였다. 테두리를 별도 자식으로 뽑았다.
        ShowFrame(item.Category, 0.95f);

        if (icon != null)
        {
            // 🔴 빈 칸으로 쓰였던 칩이 풀에서 돌아올 수 있다 — 9-슬라이스를 되돌린다 (D86).
            icon.type = UnityEngine.UI.Image.Type.Simple;

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

    public void BindEmpty(ItemCategory category)
    {
        // 빈 칸은 <b>같은 테두리를 더 옅게</b> 그린다 — 찬 칸과 한눈에 갈린다.
        ShowFrame(category, 0.55f);

        if (icon != null)
        {
            // 🔴 <b>스프라이트를 null 로 두면 흰 사각형이 통째로 칠해진다</b> (D86 · 사용자 요구).
            //    Unity 의 Image 는 스프라이트가 없으면 흰 쿼드를 그린다 — 그게 "흰박스"였다.
            //    테두리는 이제 <see cref="ShowFrame"/> 이 그리므로 여기서는 <b>지우기만</b> 한다.
            icon.sprite = null;
            icon.type   = UnityEngine.UI.Image.Type.Simple;
            icon.color  = new Color(0f, 0f, 0f, 0f);
        }
        if (label != null) label.text = string.Empty;
        name = "Chip_Empty";
    }

    // ── 테두리 (D86 신설 · D90 에 별도 자식으로 분리) ──────────────

    private UnityEngine.UI.Image _frame;

    /// <summary>
    /// <b>분류색 테두리</b>를 켠다. 칸이 비었든 찼든 <b>항상</b> 그린다 (D90).
    ///
    /// <para>🔑 <b>왜 자식으로 뽑았나</b> — 예전에는 <see cref="icon"/> 한 장이
    /// 아이콘도 그리고 테두리도 그렸다. 두 일을 동시에 못 하니 아이템이 들어오는 순간
    /// 테두리가 사라졌고, <b>찬 칸만 줄에서 튀어 보였다</b>. 겹쳐 그릴 것이 둘이면 Image 도 둘이어야 한다.</para>
    ///
    /// <para>애셋은 여전히 0개다 — 오브젝트는 <b>런타임에 한 번</b> 만들고 재사용한다.
    /// 형제 순서를 <b>맨 뒤</b>로 두어 아이콘 위에 얹는다(가운데가 뚫려 있어 그림을 가리지 않는다).</para>
    /// </summary>
    private void ShowFrame(ItemCategory category, float alpha)
    {
        if (_frame == null)
        {
            var go = new GameObject("Frame", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.SetAsLastSibling();

            _frame = go.GetComponent<UnityEngine.UI.Image>();
            _frame.sprite        = FrameSprite;
            _frame.type          = UnityEngine.UI.Image.Type.Sliced;
            _frame.raycastTarget = false;
        }

        var tint = category switch
        {
            ItemCategory.Weapon   => WeaponTint,
            ItemCategory.Building => BuildingTint,
            _                     => PassiveTint
        };
        _frame.color = new Color(tint.r, tint.g, tint.b, alpha);
        _frame.gameObject.SetActive(true);
    }

    // ── 빈 칸 테두리 (D86) ───────────────────────────────────────

    private static Sprite _frameSprite;

    /// <summary>
    /// <b>가운데가 뚫린 1px 테두리</b> 스프라이트. 애셋을 만들지 않는다 (D86).
    ///
    /// <para>🔑 8x8 텍스처의 <b>가장자리 1px 만 불투명</b>하게 칠하고 9-슬라이스 <c>border</c> 를
    /// <c>(1,1,1,1)</c> 로 준다. 그러면 칸이 얼마로 늘어나도 <b>테두리는 항상 1px</b> 이다 —
    /// 늘어나는 건 투명한 가운데뿐이다.</para>
    ///
    /// <para><see cref="BossSlam"/> 이 <c>Texture2D.whiteTexture</c> 로 사각형을 만든 것과 같은 수법인데,
    /// 여기서는 <b>속이 비어야</b> 해서 텍스처를 직접 칠한다.</para>
    /// </summary>
    private static Sprite FrameSprite
    {
        get
        {
            if (_frameSprite != null) return _frameSprite;

            // 🔴 <b>PPU 를 100 으로 맞춘다</b> (D87 — 이걸 몰라서 테두리가 12.5배로 그려졌다).
            //    9-슬라이스 테두리는 <c>border x (캔버스 referencePixelsPerUnit / sprite.pixelsPerUnit)</c>
            //    만큼 그려진다. 캔버스 기준이 100 인데 스프라이트를 PPU 8 로 만들었더니
            //    <b>1px 테두리가 12.5 캔버스px</b> 가 됐다 — 칩이 36px 이라 양쪽 25px 를 먹고
            //    가운데 11px 만 남았다. <b>사실상 꽉 찬 박스</b>였고, 사용자가 본 게 그것이다.
            const int N      = 16;
            const int Border = 4;    // PPU 100 이므로 그대로 4 캔버스px

            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode   = TextureWrapMode.Clamp,
                name       = "ItemChipFrame"
            };
            var px = new Color32[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    bool edge = x < Border || y < Border || x >= N - Border || y >= N - Border;
                    px[y * N + x] = new Color32(255, 255, 255, (byte)(edge ? 255 : 0));
                }
            tex.SetPixels32(px);
            tex.Apply();

            _frameSprite = Sprite.Create(tex, new Rect(0f, 0f, N, N), new Vector2(0.5f, 0.5f),
                                         100f, 0, SpriteMeshType.FullRect,
                                         new Vector4(Border, Border, Border, Border));
            _frameSprite.name = "ItemChipFrame";
            return _frameSprite;
        }
    }
}
