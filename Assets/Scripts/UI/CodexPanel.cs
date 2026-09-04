using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 도감 — 메인 메뉴에서 연다 (`ROADMAP` §3-3 의 마지막 화면 · D54).
///
/// <para><b>탭 5개를 한 화면에 둔다</b>(무기 · 아이템 · 진화 · 직업 · 적). 창을 갈아타는 게 아니라
/// 왼쪽 목록만 바뀌고 오른쪽 상세는 그대로 붙어 있어서, 여러 개를 훑어볼 때 클릭이 반으로 준다.</para>
///
/// <para>🔑 <b>목록을 따로 배선하지 않는다.</b> 이미 배선된 매니저들(<see cref="LevelUpManager"/>,
/// <see cref="EvolutionManager"/>, <see cref="WaveManager"/>, <see cref="GameManager"/>)에서 읽는다.
/// 애셋 폴더를 훑으면 <b>게임에 안 들어간 시험용 애셋까지</b> 도감에 뜨고,
/// <c>SceneWiring.csv</c> 에 목록을 또 적으면 <b>게임과 도감이 따로 놀 수 있다</b>.
/// 여기서 읽으면 "게임에 나오는 것 = 도감에 있는 것"이 구조적으로 보장된다.</para>
///
/// <para>🔴 문자열은 전부 영문이다 — 폰트가 Static 115자라 한글 글리프가 없다 (I-60).
/// 화살표·곱셈기호 같은 기호도 문자표에 없을 수 있어 ASCII 로만 쓴다.</para>
///
/// <para>🔴 표시 문자열의 원본은 <b>코드</b>다 (B11 의 교훈). 씬에 글자를 박아 두면
/// 씬이 코드를 덮어 나중에 고쳐도 화면이 안 바뀐다.</para>
/// </summary>
public class CodexPanel : MonoBehaviour
{
    // 🔴 컴파일 반영 확인용 (D27).
    public const int Version = 1;

    public enum Tab { Weapons, Items, Evolution, Classes, Enemies }

    [Header("탭 (Weapons / Items / Evolution / Classes / Enemies 순서)")]
    [SerializeField] private Button[] tabButtons;

    [Header("목록")]
    [Tooltip("ScrollView 의 Content. 세로 레이아웃이 붙어 있어야 한다")]
    [SerializeField] private RectTransform listContent;

    [Tooltip("행 하나의 원본. 꺼진 채로 Content 아래 두면 된다 — 복제해서 쓴다")]
    [SerializeField] private GameObject rowTemplate;

    [Header("상세")]
    [SerializeField] private Image           detailIcon;
    [SerializeField] private TextMeshProUGUI detailTitle;
    [SerializeField] private TextMeshProUGUI detailBody;

    [Header("기타")]
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Button          closeButton;

    /// <summary>미발견 항목의 이름. 사용자 요구 그대로 물음표 세 개다.</summary>
    private const string Unknown = "???";

    private readonly List<GameObject> _rows = new();
    private Tab _tab = Tab.Weapons;

    // ── 한 줄이 들고 있는 것 ────────────────────────────────────
    private struct Entry
    {
        public string  Label;      // 목록에 뜨는 글자 (미발견이면 이미 마스킹된 상태)
        public Sprite  Icon;       // 미발견이면 null 로 둔다 — 실루엣도 힌트가 된다
        public bool    Found;
        public string  Title;      // 상세 제목
        public string  Body;       // 상세 본문
    }

    private readonly List<Entry> _entries = new();

    // ── 수명 ────────────────────────────────────────────────────

    private void Awake()
    {
        if (closeButton != null) closeButton.onClick.AddListener(Close);

        if (tabButtons != null)
            for (int i = 0; i < tabButtons.Length; i++)
            {
                int idx = i;   // 🔴 클로저가 루프 변수를 잡으면 전부 마지막 값이 된다
                if (tabButtons[i] != null) tabButtons[i].onClick.AddListener(() => SelectTab((Tab)idx));
            }
    }

    private void OnEnable()
    {
        // 발견 기록은 성능 때문에 모아 두었다가 여기서 한 번 내린다 (D54).
        var gm = GameManager.Instance;
        if (gm != null && gm.MetaProgression != null) gm.MetaProgression.FlushIfDirty();

        SelectTab(_tab);
    }

    public void Close() => gameObject.SetActive(false);

    // ── 탭 ──────────────────────────────────────────────────────

    public void SelectTab(Tab tab)
    {
        _tab = tab;
        Rebuild();
    }

    private void Rebuild()
    {
        _entries.Clear();

        switch (_tab)
        {
            case Tab.Items:     BuildItems(false); break;
            case Tab.Evolution: BuildEvolutions(); break;
            case Tab.Classes:   BuildClasses();    break;
            case Tab.Enemies:   BuildEnemies();    break;
            default:            BuildItems(true);  break;   // Weapons
        }

        DrawRows();
        DrawProgress();
        DrawTabHighlight();

        // 탭을 바꾸면 목록 맨 위로 돌린다. 안 그러면 이전 탭에서 내려 둔 위치가 남아
        // 새 목록의 중간이 보이고 "왜 비었지?" 가 된다.
        if (listContent != null) listContent.anchoredPosition = Vector2.zero;

        ShowDetail(_entries.Count > 0 ? 0 : -1);
    }

    private void DrawTabHighlight()
    {
        if (tabButtons == null) return;
        for (int i = 0; i < tabButtons.Length; i++)
        {
            if (tabButtons[i] == null) continue;
            var img = tabButtons[i].GetComponent<Image>();
            if (img == null) continue;
            // 고른 탭만 밝게. 색만으로 구분하므로 명도 차를 크게 준다.
            img.color = (i == (int)_tab) ? new Color(0.85f, 0.80f, 0.45f, 1f)
                                         : new Color(0.28f, 0.28f, 0.32f, 1f);
        }
    }

    private void DrawProgress()
    {
        if (progressText == null) return;

        int found = 0;
        foreach (var e in _entries) if (e.Found) found++;
        progressText.text = $"{found} / {_entries.Count} discovered";
    }

    // ── 행 그리기 ───────────────────────────────────────────────

    private void DrawRows()
    {
        if (listContent == null || rowTemplate == null) return;

        // 필요한 만큼 만들어 두고 재사용한다. 탭을 누를 때마다 Destroy/Instantiate 하면
        // 레이아웃이 한 프레임 튀고 GC 도 같이 돈다 (MetaScreenUI 와 같은 이유).
        while (_rows.Count < _entries.Count)
        {
            var go = Instantiate(rowTemplate, listContent);
            int idx = _rows.Count;
            var btn = go.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(() => ShowDetail(idx));
            _rows.Add(go);
        }

        for (int i = 0; i < _rows.Count; i++)
        {
            bool used = i < _entries.Count;
            _rows[i].SetActive(used);
            if (!used) continue;

            var e = _entries[i];

            var label = _rows[i].GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.text  = e.Label;
                label.color = e.Found ? Color.white : new Color(0.55f, 0.55f, 0.60f, 1f);
            }

            // 아이콘은 자식 Image 중 Button 의 배경이 아닌 것.
            var icon = FindIcon(_rows[i]);
            if (icon != null)
            {
                icon.sprite  = e.Icon;
                icon.enabled = e.Icon != null;
                // 미발견은 검은 실루엣. 모양만으로도 유추가 된다.
                icon.color = e.Found ? Color.white : new Color(0f, 0f, 0f, 0.75f);
            }
        }
    }

    private static Image FindIcon(GameObject row)
    {
        var t = row.transform.Find("Icon");
        return t != null ? t.GetComponent<Image>() : null;
    }

    private void ShowDetail(int index)
    {
        bool ok = index >= 0 && index < _entries.Count;
        var e = ok ? _entries[index] : default;

        if (detailTitle != null) detailTitle.text = ok ? e.Title : "";
        if (detailBody  != null) detailBody.text  = ok ? e.Body  : "";
        if (detailIcon  != null)
        {
            detailIcon.sprite  = ok ? e.Icon : null;
            detailIcon.enabled = ok && e.Icon != null;
            detailIcon.color   = ok && e.Found ? Color.white : new Color(0f, 0f, 0f, 0.75f);
        }
    }

    // ── 발견 여부 ───────────────────────────────────────────────

    private static bool Found(CodexKind kind, Object asset)
    {
        if (asset == null) return false;
        var gm = GameManager.Instance;
        if (gm == null || gm.MetaProgression == null) return false;   // 🔴 ?. 금지 (I-24)
        return gm.MetaProgression.IsDiscovered(kind, asset.name);
    }

    // ── 탭 내용 ─────────────────────────────────────────────────

    /// <summary>
    /// 아이템 목록. <paramref name="weapons"/> 가 true 면 무기만, false 면 건물·패시브만.
    ///
    /// <para>🔑 <c>allItems</c> 에 <b>진화 결과 아이템이 없다</b> — 레벨업 카드에 그냥 뜨면
    /// 진화가 의미를 잃기 때문에 일부러 빼 둔 것이다. 도감에는 있어야 하므로 여기서 합친다.</para>
    /// </summary>
    private void BuildItems(bool weapons)
    {
        foreach (var item in AllKnownItems())
        {
            if (item == null) continue;
            bool isWeapon = item.Category == ItemCategory.Weapon;
            if (isWeapon != weapons) continue;

            bool found = Found(CodexKind.Item, item);
            string name = found ? Display(item.ItemName, item.name) : Unknown;

            _entries.Add(new Entry
            {
                Label = name,
                Icon  = item.Icon,
                Found = found,
                Title = name,
                Body  = found ? ItemBody(item) : NotFoundBody(item.Category.ToString().ToUpperInvariant()),
            });
        }
    }

    /// <summary>배선된 아이템 + 진화 결과. 중복은 걸러낸다.</summary>
    private List<ItemData> AllKnownItems()
    {
        var list = new List<ItemData>();
        var gm   = GameManager.Instance;

        if (gm != null && gm.LevelUpManager != null)
            foreach (var i in gm.LevelUpManager.AllItems)
                if (i != null && !list.Contains(i)) list.Add(i);

        var em = EvolutionManager.Instance;
        if (em != null)
            foreach (var ev in em.AllEvolutions)
                if (ev != null && ev.ResultItem != null && !list.Contains(ev.ResultItem))
                    list.Add(ev.ResultItem);

        return list;
    }

    private void BuildEvolutions()
    {
        var em = EvolutionManager.Instance;
        if (em == null) return;

        foreach (var ev in em.AllEvolutions)
        {
            if (ev == null) continue;
            bool resultFound = Found(CodexKind.Item, ev.ResultItem);
            string recipe    = Recipe(ev.Ingredients, i => ev.GetRequiredLevel(i));
            string result    = resultFound && ev.ResultItem != null
                             ? Display(ev.ResultItem.ItemName, ev.ResultItem.name) : Unknown;

            var sb = new StringBuilder();
            sb.AppendLine("<b>RECIPE</b>");
            sb.AppendLine(recipe);
            sb.AppendLine();
            sb.AppendLine("<b>RESULT</b>");
            sb.AppendLine(result);
            sb.AppendLine();
            sb.AppendLine(ev.IsFinalEvolution
                ? "Completed at the altar building in the field."
                : "Found in a treasure chest.");
            if (resultFound && !string.IsNullOrEmpty(ev.Description))
            {
                sb.AppendLine();
                sb.AppendLine(ev.Description);
            }

            _entries.Add(new Entry
            {
                Label = recipe,
                Icon  = resultFound && ev.ResultItem != null ? ev.ResultItem.Icon : null,
                Found = resultFound,
                Title = result,
                Body  = sb.ToString(),
            });
        }

        foreach (var ce in em.ClassEvolutions)
        {
            if (ce == null) continue;
            bool resultFound = Found(CodexKind.Class, ce.ResultClass);
            string recipe    = Recipe(ce.Ingredients, i => ce.GetRequiredLevel(i));
            string result    = resultFound && ce.ResultClass != null
                             ? Display(ce.ResultClass.ClassName, ce.ResultClass.name) : Unknown;

            var sb = new StringBuilder();
            sb.AppendLine("<b>PROMOTION</b>");
            sb.AppendLine(recipe);
            sb.AppendLine();
            sb.AppendLine("<b>BECOMES</b>");
            sb.AppendLine(result);
            sb.AppendLine();
            if (ce.FromClass != null)
                sb.AppendLine("Requires class: " + (Found(CodexKind.Class, ce.FromClass)
                    ? Display(ce.FromClass.ClassName, ce.FromClass.name) : Unknown));
            sb.AppendLine("Materials are NOT consumed.");

            _entries.Add(new Entry
            {
                Label = "[CLASS] " + recipe,
                Icon  = null,
                Found = resultFound,
                Title = result,
                Body  = sb.ToString(),
            });
        }
    }

    /// <summary>
    /// <c>gun Lv.5 + turret Lv.3</c> 를 만든다. 사용자 요구의 핵심이다.
    ///
    /// <para>🔑 <b>재료마다 따로 가린다.</b> 하나만 먹어 봤으면
    /// <c>gun Lv.5 + ???</c> 가 되어 "뭐랑 합치는지"를 좁혀 갈 수 있다.
    /// 통째로 가리면 조합이 몇 개짜리인지도 모른다.</para>
    ///
    /// <para>가릴 때는 <b>레벨도 같이 가린다</b> — 안 그러면 이름만 모르는 셈이라
    /// 유추가 아니라 정답 공개에 가까워진다.</para>
    /// </summary>
    private string Recipe(ItemData[] ingredients, System.Func<int, int> levelOf)
    {
        if (ingredients == null || ingredients.Length == 0) return Unknown;

        var sb = new StringBuilder();
        for (int i = 0; i < ingredients.Length; i++)
        {
            if (i > 0) sb.Append("  +  ");
            var ing = ingredients[i];
            if (ing != null && Found(CodexKind.Item, ing))
                sb.Append(Display(ing.ItemName, ing.name)).Append(" Lv.").Append(levelOf(i));
            else
                sb.Append(Unknown);
        }
        return sb.ToString();
    }

    private void BuildClasses()
    {
        foreach (var cls in AllKnownClasses())
        {
            if (cls == null) continue;
            bool found  = Found(CodexKind.Class, cls);
            string name = found ? Display(cls.ClassName, cls.name) : Unknown;

            _entries.Add(new Entry
            {
                Label = (cls.Tier > 1 ? "[T" + cls.Tier + "] " : "") + name,
                Icon  = null,
                Found = found,
                Title = name,
                Body  = found ? ClassBody(cls) : NotFoundBody(cls.Tier > 1 ? "PROMOTION CLASS" : "STARTING CLASS"),
            });
        }
    }

    /// <summary>시작 직업 + 승급으로만 닿는 직업. 후자는 <c>GameManager.Classes</c> 에 일부러 없다.</summary>
    private List<CharacterClassData> AllKnownClasses()
    {
        var list = new List<CharacterClassData>();
        var gm   = GameManager.Instance;

        if (gm != null && gm.Classes != null)
            foreach (var c in gm.Classes)
                if (c != null && !list.Contains(c)) list.Add(c);

        var em = EvolutionManager.Instance;
        if (em != null)
            foreach (var ce in em.ClassEvolutions)
                if (ce != null && ce.ResultClass != null && !list.Contains(ce.ResultClass))
                    list.Add(ce.ResultClass);

        return list;
    }

    private void BuildEnemies()
    {
        var gm = GameManager.Instance;
        var wm = gm != null ? gm.WaveManager : null;
        if (wm == null) return;

        foreach (var e in wm.CollectEnemies())
        {
            if (e == null) continue;
            bool found  = Found(CodexKind.Enemy, e);
            string name = found ? Display(e.EnemyName, e.name) : Unknown;

            _entries.Add(new Entry
            {
                Label = name,
                Icon  = e.Sprite,
                Found = found,
                Title = name,
                Body  = found ? EnemyBody(e) : NotFoundBody("ENEMY"),
            });
        }
    }

    // ── 상세 본문 ───────────────────────────────────────────────

    private static string NotFoundBody(string category)
        => $"<b>{category}</b>\n\nNot discovered yet.\n\n" +
           "Find it in a run and it will be recorded here\nwith its full numbers.";

    private static string ItemBody(ItemData item)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"<b>{item.Category.ToString().ToUpperInvariant()}</b>   Max Lv.{item.MaxLevel}   Shop {item.ShopPrice}G");
        if (!string.IsNullOrEmpty(item.Description))
        {
            sb.AppendLine();
            sb.AppendLine(item.Description);
        }
        sb.AppendLine();

        if (item.WeaponRef != null)   AppendWeapon(sb, item.WeaponRef);
        if (item.BuildingRef != null) AppendBuilding(sb, item.BuildingRef);
        if (item.PassiveRef != null)  AppendPassive(sb, item.PassiveRef);
        return sb.ToString();
    }

    private static void AppendWeapon(StringBuilder sb, WeaponData w)
    {
        // 위 첫 줄이 이미 카테고리를 말했다 — 여기서 또 WEAPON 이라고 쓰면 같은 말이 두 번이다.
        sb.AppendLine("<b>NUMBERS</b>   Lv1 -> max");
        Curve(sb, "Damage",      w.Damage);
        Curve(sb, "Cooldown",    w.Cooldown);
        Curve(sb, "Projectiles", w.ProjectileCount);
        Curve(sb, "Range",       w.Range);
        Curve(sb, "Size",        w.ProjectileSize);
        sb.Append("Speed".PadRight(12));
        sb.AppendLine($"{w.ProjectileSpeed:0.##}");
    }

    private static void AppendBuilding(StringBuilder sb, BuildingData b)
    {
        sb.AppendLine("<b>NUMBERS</b>   Lv1 -> max");
        Curve(sb, "Damage",   b.Damage);
        Curve(sb, "Range",    b.AttackRange);
        Curve(sb, "Cooldown", b.AttackCooldown);
        Curve(sb, "Max count", b.MaxCount);
    }

    private static void AppendPassive(StringBuilder sb, PassiveData p)
    {
        // 🔴 패시브 보너스는 레벨 배열이다. Lv1 과 Lv5 를 같이 보여야
        //    "지금 먹으면 뭐가 얼마나 오르는지"가 보인다.
        sb.AppendLine("<b>NUMBERS</b>   Lv1 -> max");
        Curve(sb, "Max HP",       p.BonusMaxHp);
        Curve(sb, "Move Speed",   p.BonusMoveSpeed);
        Curve(sb, "Damage",       p.BonusDamage);
        Curve(sb, "Atk Speed",    p.BonusAttackSpeed);
        Curve(sb, "Proj Size",    p.BonusProjectileSize);
        Curve(sb, "Armor",        p.BonusArmor);
        Curve(sb, "Pickup",       p.BonusPickupRadius);
        Curve(sb, "Crit",         p.BonusCritChance);
        Curve(sb, "XP Gain",      p.BonusXpGain);
        Curve(sb, "Gold Gain",    p.BonusGoldGain);
        Curve(sb, "Bld Cooldown", p.BonusBuildingCooldown);
        Curve(sb, "Luck",         p.BonusLuck);
    }

    private static string EnemyBody(EnemyData e)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"<b>{e.AI.ToString().ToUpperInvariant()}</b>");
        sb.AppendLine();
        sb.AppendLine($"HP        {e.MaxHp:0.##}");
        sb.AppendLine($"Speed     {e.MoveSpeed:0.##}");
        sb.AppendLine($"Contact   {e.ContactDamage:0.##}");
        sb.AppendLine($"Armor     {e.Armor:0.##}");
        sb.AppendLine($"XP        {e.XpDrop}");
        sb.AppendLine();

        switch (e.AI)
        {
            case EnemyAI.Ranged:
                sb.AppendLine($"Keeps range {e.PreferredRange:0.##}, fires every {e.AttackCooldown:0.##}s.");
                break;
            case EnemyAI.Charger:
                sb.AppendLine($"Winds up {e.ChargeWindup:0.##}s, then dashes at x{e.ChargeSpeedMult:0.##}.");
                break;
            case EnemyAI.Flanker:
                sb.AppendLine("Circles in from the side instead of walking straight at you.");
                break;
            case EnemyAI.Swarmer:
                sb.AppendLine($"Speeds up in a crowd: x{e.SwarmSoloMult:0.##} alone, x{e.SwarmPackMult:0.##} in a pack.");
                break;
            case EnemyAI.Blocker:
                sb.AppendLine($"Does not chase. Cuts off where you are heading, {e.BlockLeadTime:0.##}s ahead.");
                break;
            default:
                sb.AppendLine("Walks straight at you.");
                break;
        }

        sb.AppendLine();
        sb.AppendLine($"<b>ELITE</b>  HP x{e.EliteHpMult:0.##}  DMG x{e.EliteDamageMult:0.##}  XP x{e.EliteXpMult}");
        sb.AppendLine($"<b>BOSS</b>   HP x{e.BossHpMult:0.##}  DMG x{e.BossDamageMult:0.##}  XP x{e.BossXpMult}");
        return sb.ToString();
    }

    private static string ClassBody(CharacterClassData c)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"<b>TIER {c.Tier}</b>" + (c.Tier > 1 ? "   (promotion only)" : "   (selectable at start)"));
        if (!string.IsNullOrEmpty(c.Description))
        {
            sb.AppendLine();
            sb.AppendLine(c.Description);
        }
        sb.AppendLine();
        if (c.StartingWeapon != null)
            sb.AppendLine($"Starts with  {c.StartingWeapon.WeaponName} Lv.{c.StartingWeaponLevel}");
        sb.AppendLine();
        sb.AppendLine("<b>BONUSES</b>");
        Bonus(sb, "Max HP",       c.BonusMaxHp);
        Bonus(sb, "Move Speed",   c.BonusMoveSpeed);
        Bonus(sb, "Damage",       c.BonusDamage);
        Bonus(sb, "Atk Speed",    c.BonusAttackSpeed);
        Bonus(sb, "Proj Size",    c.BonusProjectileSize);
        Bonus(sb, "Pickup",       c.BonusPickupRadius);
        Bonus(sb, "Crit",         c.BonusCritChance);
        Bonus(sb, "Armor",        c.BonusArmor);
        Bonus(sb, "XP Gain",      c.BonusXpGain);
        Bonus(sb, "Gold Gain",    c.BonusGoldGain);

        // 🔑 직업을 가르는 두 번째 축이다 — "얼마나 센가"가 아니라 "무엇을 들 수 있는가".
        //    더해지는 값이고 승급하면 사슬 전체가 합산된다.
        sb.AppendLine();
        sb.AppendLine("<b>SLOTS</b>   (added, stacks along the class chain)");
        Bonus(sb, "Weapons",   c.BonusWeaponSlots);
        Bonus(sb, "Passives",  c.BonusPassiveSlots);
        Bonus(sb, "Buildings", c.BonusBuildingSlots);
        return sb.ToString();
    }

    // ── 작은 도우미 ─────────────────────────────────────────────

    /// <summary>표시 이름이 비면 애셋 파일명으로 떨어진다. 도감이 빈 줄을 그리지 않게.</summary>
    private static string Display(string shown, string assetName)
        => string.IsNullOrEmpty(shown) ? assetName : shown;

    /// <summary>레벨 배열을 <c>Lv1 -> Lv5</c> 한 줄로. 값이 하나면 그것만 쓴다.</summary>
    private static void Curve(StringBuilder sb, string label, float[] values)
    {
        if (values == null || values.Length == 0 || AllZero(values)) return;
        float a = values[0], b = values[values.Length - 1];
        sb.Append(label.PadRight(12));
        sb.AppendLine(Mathf.Approximately(a, b) ? $"{a:0.##}" : $"{a:0.##}  ->  {b:0.##}");
    }

    private static void Curve(StringBuilder sb, string label, int[] values)
    {
        if (values == null || values.Length == 0) return;
        int a = values[0], b = values[values.Length - 1];
        sb.Append(label.PadRight(12));
        sb.AppendLine(a == b ? $"{a}" : $"{a}  ->  {b}");
    }

    /// <summary>0 인 보너스는 안 적는다. 전부 0 이면 그 절은 제목만 남는다.</summary>
    private static void Bonus(StringBuilder sb, string label, float v)
    {
        if (Mathf.Approximately(v, 0f)) return;
        sb.Append(label.PadRight(14));
        sb.AppendLine((v > 0f ? "+" : "") + v.ToString("0.##"));
    }

    private static void Bonus(StringBuilder sb, string label, int v)
    {
        if (v == 0) return;
        sb.Append(label.PadRight(14));
        sb.AppendLine((v > 0 ? "+" : "") + v);
    }

    /// <summary>배열이 전부 0 이면 안 적는다 — 안 쓰는 축까지 줄줄이 뜨면 읽을 게 없다.</summary>
    private static bool AllZero(float[] v)
    {
        if (v == null) return true;
        foreach (var x in v) if (!Mathf.Approximately(x, 0f)) return false;
        return true;
    }
}
