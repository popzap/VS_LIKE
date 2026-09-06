using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

// ────────────────────────────────────────────────────────────────────────────
//  StatsPanelUI  —  일시정지(ESC) 화면에 뜨는 스탯 · 보유 아이템
// ────────────────────────────────────────────────────────────────────────────
/// <summary>
/// <b>뭘 들고 있는지 인게임에서 확인할 방법이 없었다</b>(`ROADMAP` §3-1).
/// 레벨업 카드는 고르는 순간만 보이고, 상점은 파는 것만 보여 준다.
///
/// <para>🔑 <b>D92 에서 <c>ESC</c> 일시정지 화면으로 옮겼다.</b> 원래는 <c>TAB</c> 을 누르는 동안
/// <c>timeScale 0.25</c> 저배속으로 띄웠는데(*"보는 도중에도 플레이 되게"*), 사용자 판정이
/// *"뒤랑 겹쳐 보이는 게 쎄다"* 였다 — 카드가 반투명(<c>UI_Panel</c> 채움 알파 <b>0.92</b>)인 데다
/// <b>가림막이 없어서</b> 전투 화면과 HUD 아이템 줄이 글자 위로 그대로 비쳤다.
/// 요구는 *"ESC 로 게임도 멈추고 한 번에 보여 달라"*.</para>
///
/// <para>🟢 <b>그래서 <c>timeScale</c> 을 다투는 코드가 통째로 사라졌다.</b>
/// 저배속을 스스로 걸던 시절엔 히트스톱·레벨업·웨이브와 주인을 다투느라
/// <see cref="GameManager.DoHitstop"/> 과 같은 <b>3단 계약</b>이 필요했다.
/// 이제 멈추는 일은 <see cref="PauseMenuUI"/> 한 곳만 하고, 이 스크립트는 <b>값만 채운다.</b></para>
///
/// <para>🔴 <c>?.</c> 를 쓰지 않는다 — 미할당 직렬화 필드는 "가짜 null" 이라 예외가 샌다 (I-24).</para>
/// </summary>
public class StatsPanelUI : MonoBehaviour
{
    // 🔴 컴파일 반영 확인용 (D27).
    public const int Version = 7;   // 7 = 값 위치 58% (D93) · 6 = 글자 30 (D93) · 5 = 꺼진 채 저장된 카드 자가복구 (D92) · 4 = ESC 화면으로 이사 · TAB 폐지 (D92) · 3 = 글자 25 + 코드가 배치 (D87) · 2 = 2단 + 값 정렬 (D87) · 1 = 최초

    [Header("배선")]
    [Tooltip("내용이 담긴 카드. 여닫는 주인은 PauseMenuUI 다 — 이 스크립트는 켜고 끄지 않는다 (D92).")]
    [SerializeField] private GameObject      panelRoot;
    [SerializeField] private TextMeshProUGUI classText;
    [Tooltip("왼쪽 단 — SURVIVAL + OFFENSE")]
    [SerializeField] private TextMeshProUGUI statsText;

    [Tooltip("오른쪽 단 — CRITICAL + UTILITY. 🔴 없으면 왼쪽 단에 전부 이어 붙인다(예전 모양).")]
    [SerializeField] private TextMeshProUGUI statsRightText;
    [SerializeField] private TextMeshProUGUI itemsTitleText;
    [Tooltip("칩이 채워지는 격자. GridLayoutGroup 이 붙어 있어야 한다.")]
    [SerializeField] private Transform       itemGrid;
    [SerializeField] private GameObject      chipPrefab;

    [Tooltip("카드 아래 한 줄 안내. 🔴 글자는 코드가 정한다 — 씬에 두면 낡는다 (B11).")]
    [SerializeField] private TextMeshProUGUI hintText;

    private readonly List<ItemChipUI> _chips = new();

    // ─────────────────────────────────────────────────────────────

    // 🔴 <b>Awake 에서 panelRoot 를 끄지 않는다</b> (D92).
    //    이제 이 내용은 <see cref="PauseMenuUI"/> 의 <c>PausePanel</c> 안에 들어 있고,
    //    여닫는 주인은 그쪽이다. 여기서 끄면 <b>일시정지 화면을 열어도 영영 안 보인다</b>
    //    — PausePanel 은 자기 루트만 켜지 이 자식까지 다시 켜 주지 않는다.

    /// <summary>
    /// 화면에 값을 다시 채운다. <b>부르는 쪽은 <see cref="PauseMenuUI.Open"/> 하나다</b> (D92).
    ///
    /// <para>🔑 <b>스스로 열지 않는다.</b> 예전에는 TAB 을 눌러 <c>timeScale 0.25</c> 로
    /// 저배속을 걸었는데, 사용자 판정이 *"뒤랑 겹쳐 보이는 게 쎄다"* 였다 —
    /// 창이 반투명(<c>UI_Panel</c> 채움 알파 <b>0.92</b> · I-53/D47)인 데다
    /// <b>가림막이 아예 없어서</b> 게임과 HUD 가 그대로 비쳤다.
    /// 요구는 *"ESC 로 게임도 멈추고 한 번에 보여 달라"* 였다.</para>
    ///
    /// <para>🟢 <b>덕분에 <c>timeScale</c> 계약이 통째로 사라졌다.</b> 저배속을 스스로 걸던 시절엔
    /// 히트스톱·레벨업·웨이브와 <c>timeScale</c> 주인을 다투느라 3단 계약이 필요했다.
    /// 이제 멈추는 일은 <see cref="PauseMenuUI"/> 한 곳만 한다.</para>
    /// </summary>
    public void Refresh()
    {
        // 🔴 <b>실제로 이것 때문에 한 번 안 보였다</b> (D92 검증).
        //    예전 <c>Awake</c> 가 <c>panelRoot.SetActive(false)</c> 를 하고 있어서 씬에도
        //    <b>꺼진 채로 저장</b>돼 있었다. 그 <c>Awake</c> 를 없애자 아무도 다시 켜 주지 않아
        //    값은 전부 채워졌는데 <b>화면에는 아무것도 안 떴다</b> — 로그로만 보면 정상이다.
        //    씬 값도 켜 뒀지만, 누가 또 꺼서 저장해도 여기서 스스로 복구한다.
        if (panelRoot != null && !panelRoot.activeSelf) panelRoot.SetActive(true);

        var ps = PlayerStats.Current;

        BuildClassLine(ps);
        BuildStats(ps);
        BuildItems();

        // 🔴 씬에 박아 두면 낡는다 (B11) — 실제로 여기엔 *"Hold [TAB] — time runs slow"* 가
        //    남아 있었고, D92 로 조작이 바뀐 뒤에도 그대로였을 것이다.
        if (hintText != null) hintText.text = "[ESC] Resume  ·  The game is fully paused.";
    }

    private void BuildClassLine(PlayerStats ps)
    {
        if (classText == null) return;
        if (ps == null || ps.Class == null) { classText.text = "-"; return; }

        // 승급은 사슬로 누적된다(I-56). 마지막 것만 보이면 어떻게 여기까지 왔는지가 안 보인다.
        var sb = new StringBuilder();
        var chain = ps.ClassChain;
        for (int i = 0; i < chain.Count; i++)
        {
            if (i > 0) sb.Append("  >  ");
            sb.Append(chain[i] != null ? chain[i].ClassName : "?");
        }
        classText.text = sb.ToString();
    }

    /// <summary>
    /// 스탯을 한 줄씩 쌓는다.
    ///
    /// <para>🔑 <b>배율 스탯은 %, 절대값 스탯은 숫자</b>로 쓴다. <c>Damage 1.35</c> 는
    /// 피해량이 1.35 라는 뜻으로 읽히지만 실제로는 <b>1.35 배</b>다.</para>
    ///
    /// <para>⚠️ <c>AttackSpeed</c> 와 <c>BuildingCooldown</c> 은 <b>낮을수록 빠르다</b>.
    /// 그대로 보여 주면 "공격속도 0.8" 이 느려 보인다 — <b>뒤집어서</b> 보여 준다.</para>
    /// </summary>
    private void BuildStats(PlayerStats ps)
    {
        if (statsText == null) return;
        if (ps == null)
        {
            statsText.text = "-";
            if (statsRightText != null) statsRightText.text = string.Empty;
            return;
        }

        var s = ps.Final;

        // ── 왼쪽 단 ──────────────────────────────────────────
        var l = new StringBuilder(384);
        Head(l, "SURVIVAL");
        Row(l, "Health",       ps.CurrentHp.ToString("F0") + " / " + s.MaxHp.ToString("F0"));
        Row(l, "Armor",        s.Armor.ToString("F0"));
        Row(l, "Move Speed",   s.MoveSpeed.ToString("F2"));
        Gap(l);
        Head(l, "OFFENSE");
        Row(l, "Damage",       Pct(s.Damage));
        Row(l, "Attack Speed", Pct(Inverse(s.AttackSpeed)));
        Row(l, "Projectile",   Pct(s.ProjectileSize));

        // ── 오른쪽 단 ────────────────────────────────────────
        var r = new StringBuilder(384);
        Head(r, "CRITICAL");
        Row(r, "Crit Chance",  (s.CritChance * 100f).ToString("F0") + "%");
        Row(r, "Crit Damage",  s.CritMultiplier.ToString("F2") + "x");
        Gap(r);
        Head(r, "UTILITY");
        Row(r, "Pickup Range", s.PickupRadius.ToString("F1"));
        Row(r, "XP Gain",      Pct(s.XpGain));
        Row(r, "Gold Gain",    Pct(s.GoldGain));
        Row(r, "Luck",         s.Luck.ToString("F2"));
        Row(r, "Build Speed",  Pct(Inverse(s.BuildingCooldown)));

        // 🔴 오른쪽 단이 배선 안 됐으면 <b>버리지 않고</b> 왼쪽에 이어 붙인다.
        //    스탯이 조용히 사라지는 것보다 못생긴 게 낫다.
        if (statsRightText != null)
        {
            Style(statsText,      l.ToString());
            Style(statsRightText, r.ToString());
        }
        else
        {
            Gap(l);
            Style(statsText, l.Append(r).ToString());
        }
    }

    /// <summary>
    /// 글자 배치도 코드가 정한다 — 씬 값이면 다음에 캔버스를 만질 때 조용히 돌아간다 (B11).
    ///
    /// <para>🔴 <b>재고 나서 키운다.</b> 단이 300x340 이던 시절엔 25 가 상한이었다
    /// (오른쪽 단 실측 316/340). D93 에서 한 상자로 합치며 단이 <b>460x420</b> 이 됐으므로
    /// <c>316 x 30/25 = 379 ≤ 420</c> — <b>30 까지 올릴 수 있다</b>(사용자: *"스탯 글씨 키워"*).</para>
    ///
    /// <para>🔴 <b>자동 축소는 끈다.</b> 켜 두면 스탯이 늘 때 글자가 스스로 작아져
    /// 어느 날 갑자기 안 읽히는데, 그게 언제 시작됐는지 알 방법이 없다.</para>
    /// </summary>
    private static void Style(TMPro.TextMeshProUGUI t, string body)
    {
        if (t == null) return;
        t.text             = body;
        t.alignment        = TextAlignmentOptions.TopLeft;
        t.enableAutoSizing = false;
        t.fontSize         = 30f;
        t.overflowMode     = TextOverflowModes.Overflow;
        t.richText         = true;
    }

    /// <summary>
    /// 스탯 한 줄. 🔑 <b>값을 <c>&lt;pos&gt;</c> 로 같은 자리에 세운다</b> (D87 · 참고 이미지).
    ///
    /// <para>예전에는 <c>이름 	 값</c> 이었는데 TMP 의 탭 정지 위치는 기본값이라
    /// 이름 길이에 따라 값이 <b>들쭉날쭉했다</b>. <c>&lt;pos&gt;</c> 는 단 너비의 비율이라
    /// 단을 넓히거나 좁혀도 <b>줄이 계속 맞는다.</b></para>
    ///
    /// <para>🔴 <b>72 % 는 여백이 5px 뿐이었다</b> (D93). 글자를 30 으로 키우자 단 460 에서
    /// 실측 가로가 <b>455/460</b> 이 됐다. <b>58 %</b> 로 당기니 같은 글이 <b>391/460</b> 이다.</para>
    ///
    /// <para>⚠️ <b>정정</b> — 처음에는 *"72 % 면 네 자리 체력에서 넘친다"* 고 적었는데
    /// <b>재 보니 아니었다</b>(<c>9999 / 9999</c> 로 만든 최악 문자열이 72 % 에서 425, 58 % 에서 439).
    /// 넘치지는 않는다. 58 % 를 쓰는 이유는 <b>실제 본문의 여백이 5px → 69px 로 늘어서</b>이지
    /// 72 % 가 깨지기 때문이 아니다.</para>
    /// </summary>
    private static void Row(StringBuilder sb, string label, string value)
        => sb.Append(label).Append("<pos=58%>").Append(value).Append('\n');

    /// <summary>구역 제목 — 금색. <see cref="HelpPanel"/> 과 같은 배색이라 창끼리 통일된다.</summary>
    private static void Head(StringBuilder sb, string title)
        => sb.Append("<size=108%><color=#F0C040>").Append(title).Append("</color></size>\n");

    private static void Gap(StringBuilder sb) => sb.Append('\n');

    /// <summary>낮을수록 빠른 값을 "빠르기"로 뒤집는다. 0 이하는 나눗셈이 터지므로 막는다.</summary>
    private static float Inverse(float cooldownMult)
        => cooldownMult > 0.01f ? 1f / cooldownMult : 1f;

    private static string Pct(float mult) => $"{mult * 100f:F0}%";

    // ─────────────────────────────────────────────────────────────

    private void BuildItems()
    {
        if (itemGrid == null || chipPrefab == null) return;

        var lm = GameManager.Instance != null ? GameManager.Instance.LevelUpManager : null;
        var inv = lm != null ? lm.Inventory : null;

        int used = 0;
        if (inv != null)
        {
            foreach (var pair in inv)
            {
                if (pair.Key == null) continue;
                var chip = GetChip(used++);
                chip.gameObject.SetActive(true);
                chip.Bind(pair.Key, pair.Value);
            }
        }

        // 남는 칩은 지우지 않고 끈다 — 창은 자주 열리므로 매번 Destroy 하면 쓰레기가 는다.
        for (int i = used; i < _chips.Count; i++)
            _chips[i].gameObject.SetActive(false);

        if (itemsTitleText != null)
            itemsTitleText.text = used > 0 ? $"ITEMS  ({used})" : "ITEMS  (none yet)";
    }

    private ItemChipUI GetChip(int index)
    {
        while (_chips.Count <= index)
        {
            var go   = Instantiate(chipPrefab, itemGrid);
            var chip = go.GetComponent<ItemChipUI>();
            if (chip == null) chip = go.AddComponent<ItemChipUI>();

            // 🔴 <b>안 부르면 아이콘이 프리팹의 고정 64x64 로 남는다</b> (D93 · `B16`).
            //    격자 칸을 96x104 로 키웠는데 아이콘만 64 면 <b>여백만 늘어난다</b> —
            //    사용자 요구는 *"아이템 크기 키워"* 였다. <c>false</c> 는 <b>글자를 켠 채</b>
            //    아이콘을 칸에 맞추라는 뜻이다(레벨은 여기서 보여 준다).
            chip.SetCompact(false);
            _chips.Add(chip);
        }
        return _chips[index];
    }
}
