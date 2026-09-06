using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// ────────────────────────────────────────────────────────────────────────────
//  StatsPanelUI  —  전투 중 TAB 으로 여는 스탯 · 보유 아이템 창
// ────────────────────────────────────────────────────────────────────────────
/// <summary>
/// <b>뭘 들고 있는지 인게임에서 확인할 방법이 없었다</b>(`ROADMAP` §3-1).
/// 레벨업 카드는 고르는 순간만 보이고, 상점은 파는 것만 보여 준다.
///
/// <para>🔑 <b>멈추지 않고 느려진다.</b> 사용자 요구 — *"보는 도중에도 게임은 플레이 되게"*.
/// 그래서 일시정지(<c>timeScale = 0</c>)가 아니라 <b>저배속</b>이다. 대가가 있다:
/// 창을 보는 동안에도 <b>적은 계속 다가오고 무기는 계속 나간다.</b>
/// 그래서 창은 <b>화면 왼쪽만</b> 덮고 전투는 계속 보이게 둔다.</para>
///
/// <para>🔴 <b><c>Time.timeScale</c> 은 이 프로젝트에서 여러 곳이 공유한다</b> —
/// <c>WaveManager.PauseWave</c> 가 0, 레벨업·상점·결과창이 각각 0/1 을 쓰고
/// <c>GameManager.DoHitstop</c> 도 끼어든다. 아무 때나 1f 로 되돌리면 일시정지가 저절로 풀린다.
/// 그래서 <see cref="GameManager.DoHitstop"/> 이 쓰는 것과 <b>같은 계약</b>을 따른다:</para>
/// <list type="number">
///   <item><b>웨이브 중일 때만</b> 연다</item>
///   <item>이미 <c>timeScale</c> 이 1 이 아니면(누가 멈춰 놨으면) <b>아예 안 연다</b></item>
///   <item>닫을 때도 <b>여전히 웨이브인지 다시 확인</b>하고, 아니면 손대지 않는다 —
///         그 사이에 레벨업 패널이 떴다면 <c>timeScale</c> 의 주인이 바뀐 것이다</item>
/// </list>
///
/// <para>⚠️ <b>알려진 부작용</b> — 창이 열려 있는 동안에는 히트스톱이 안 걸린다.
/// <c>DoHitstop</c> 이 <c>timeScale != 1</c> 이면 스스로 물러나기 때문이다.
/// 저배속 중에 히트스톱까지 겹치면 어차피 뭐가 뭔지 안 보인다 — 그대로 둔다.</para>
/// </summary>
public class StatsPanelUI : MonoBehaviour
{
    // 🔴 컴파일 반영 확인용 (D27).
    public const int Version = 3;   // 3 = 글자 25 + 코드가 배치 (D87) · 2 = 2단 + 값 정렬 (D87) · 1 = 최초

    [Header("배선")]
    [Tooltip("TAB 을 누르는 동안 켜지는 루트. 이 스크립트가 붙은 오브젝트는 항상 활성이어야 한다.")]
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

    [Header("저배속 — 값은 Economy.csv 가 덮는다")]
    [Tooltip("창을 보는 동안의 시간 배속. 0 이면 완전 정지라 요구와 어긋난다 — 최소 0.02 로 묶는다.")]
    [SerializeField] private float slowTimeScale = 0.25f;

    private readonly List<ItemChipUI> _chips = new();
    private bool _open;

    // ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        // 🔴 GameManager.Instance.XxxMgr 를 여기서 캐시하지 않는다 (I-8 · I-38).
        //    그 참조는 GameManager.Start() 에서 채워지는데 모든 Awake 는 모든 Start 보다 먼저 돈다.
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (!_open && kb.tabKey.wasPressedThisFrame) TryOpen();
        else if (_open && kb.tabKey.wasReleasedThisFrame) Close();

        // 창이 열린 사이에 레벨업 패널이 뜨거나 웨이브가 끝났을 수 있다.
        // 그러면 timeScale 의 주인이 바뀐 것이라 내가 붙잡고 있으면 안 된다.
        if (_open && !CanBeOpen()) Close();
    }

    private void OnDisable()
    {
        // 창을 연 채로 씬이 바뀌거나 이 오브젝트가 꺼지면 저배속이 그대로 남는다.
        if (_open) Close();
    }

    private bool CanBeOpen()
    {
        var gm = GameManager.Instance;
        return gm != null && gm.CurrentState == GameState.Wave;
    }

    // ─────────────────────────────────────────────────────────────

    private void TryOpen()
    {
        if (!CanBeOpen()) return;

        // 🔴 누가 이미 멈춰 놨으면 손대지 않는다 (DoHitstop 과 같은 계약).
        //    이걸 빼면 히트스톱 도중에 TAB 을 눌렀다가 놓는 순간 히트스톱이 저절로 풀린다.
        if (!Mathf.Approximately(Time.timeScale, 1f)) return;

        _open = true;
        Time.timeScale = Mathf.Clamp(slowTimeScale, 0.02f, 1f);

        Refresh();
        if (panelRoot != null) panelRoot.SetActive(true);
    }

    private void Close()
    {
        _open = false;
        if (panelRoot != null) panelRoot.SetActive(false);

        // 닫는 시점에도 여전히 웨이브인지 본다. 아니면 timeScale 은 새 주인 것이다.
        if (CanBeOpen()) Time.timeScale = 1f;
    }

    // ─────────────────────────────────────────────────────────────

    private void Refresh()
    {
        var ps = PlayerStats.Current;

        BuildClassLine(ps);
        BuildStats(ps);
        BuildItems();
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
    /// <para>단 하나가 300 x 340 이고 실측 필요 높이가 <b>279</b>(오른쪽) 였다.
    /// 25 로 키우면 <c>279 x 25/22 = 317</c> 이라 아직 들어간다 — <b>재고 나서 키운다.</b></para>
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
        t.fontSize         = 25f;
        t.overflowMode     = TextOverflowModes.Overflow;
        t.richText         = true;
    }

    /// <summary>
    /// 스탯 한 줄. 🔑 <b>값을 <c>&lt;pos&gt;</c> 로 같은 자리에 세운다</b> (D87 · 참고 이미지).
    ///
    /// <para>예전에는 <c>이름 	 값</c> 이었는데 TMP 의 탭 정지 위치는 기본값이라
    /// 이름 길이에 따라 값이 <b>들쭉날쭉했다</b>. <c>&lt;pos=72%&gt;</c> 는 단 너비의 비율이라
    /// 단을 넓히거나 좁혀도 <b>줄이 계속 맞는다.</b></para>
    /// </summary>
    private static void Row(StringBuilder sb, string label, string value)
        => sb.Append(label).Append("<pos=72%>").Append(value).Append('\n');

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
            _chips.Add(chip);
        }
        return _chips[index];
    }
}
