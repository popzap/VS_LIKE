using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 런 종료 화면. GameOver(사망) 와 Victory(보스 처치) 를 한 패널로 처리한다.
/// Retry / Main Menu 모두 씬을 다시 로드해 런타임 상태를 완전히 초기화한다.
/// </summary>
public class RunEndUI : GameStatePanel
{
    [Header("텍스트")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI summaryText;

    [Header("버튼")]
    [SerializeField] private Button retryButton;
    [SerializeField] private Button mainMenuButton;

    // ── 아이템 칩 줄 (D83 · 사용자 요구) ─────────────────────────
    [Header("먹은 아이템 — 글자가 아니라 아이콘")]
    [Tooltip("칩이 담길 자리. 그리드 레이아웃 그룹을 붙여 둔다.")]
    [SerializeField] private Transform  itemGrid;
    [Tooltip("Prefab_ItemChip. TAB 스탯 창·HUD 줄과 같은 프리팹이다.")]
    [SerializeField] private GameObject itemChipPrefab;

    private readonly System.Collections.Generic.List<ItemChipUI> _chips = new();

    private static readonly Color ColorWin  = new(0.98f, 0.82f, 0.25f);
    private static readonly Color ColorLose = new(0.93f, 0.30f, 0.29f);

    protected override bool IsVisibleIn(GameState state)
        => state == GameState.GameOver || state == GameState.Victory;

    protected override void Awake()
    {
        base.Awake();
        if (retryButton)    retryButton   .onClick.AddListener(() => GameManager.ReloadScene(true));
        if (mainMenuButton) mainMenuButton.onClick.AddListener(() => GameManager.ReloadScene(false));

        // 🔴 본문이 길어지면(아이템 최대 16종) 상자를 넘쳐 버튼을 덮는다 — D56 에서 실제로 겪었다.
        //    코드가 정본이어야 한다. 씬 값이면 캔버스를 만질 때 조용히 꺼진다 (B11).
        if (summaryText != null)
        {
            // 🔴 <b>글자 상자를 위쪽 절반으로 줄였다</b> (D83 · 사용자 요구).
            //    `D56` 은 아이템이 글자로 들어와 넘치는 걸 막으려고 상자를 230px 로 키웠는데,
            //    이제 아이템이 아이콘으로 빠졌으므로 **세 줄 + 직업 사슬**만 남는다.
            //    아래 절반은 아이콘 격자가 쓴다.
            //
            //    🔑 요구는 *"글자 크기도 키우고 거리도 띄워서 눈에 잘 들어오게"* 였다 —
            //    상자가 작아진 만큼 **글자를 키울 수 있다**(자동 축소 하한 15 -> 22).
            var rt = summaryText.rectTransform;
            rt.sizeDelta        = new Vector2(680f, 150f);
            rt.anchoredPosition = new Vector2(0f, 45f);

            summaryText.enableAutoSizing = true;
            summaryText.fontSizeMin      = 22f;
            summaryText.fontSizeMax      = 34f;
            summaryText.lineSpacing      = 28f;    // "거리도 띄워서"
            summaryText.overflowMode     = TextOverflowModes.Truncate;
        }
    }

    protected override void OnShown(GameState state)
    {
        Time.timeScale = 0f;

        bool win = state == GameState.Victory;
        if (titleText != null)
        {
            titleText.text  = win ? "Victory!" : "Game Over";
            titleText.color = win ? ColorWin : ColorLose;
        }

        if (summaryText == null) return;

        var wave = GameManager.Instance?.WaveManager;
        int   kills = wave != null ? wave.TotalKillCount : 0;
        float time  = wave != null ? wave.TotalElapsedTime : 0f;

        // 이번 런이 **벌어 간** 메타 골드다 (누적 보유액이 아니다). 런 골드는 여기서 소멸했다.
        int gold = GameManager.Instance != null ? GameManager.Instance.LastSettledMetaGold : 0;

        summaryText.text = BuildSummary(kills, time, gold);
        BuildItemChips();
    }

    /// <summary>
    /// 클리어 화면 본문 (D66 · 사용자 요구 15).
    ///
    /// <para>사용자 판정: *"클리어 화면 너무 단조로움. 먹은 아이템들 같이 추가 정보를 보여줘
    /// (가장 큰 데미지 같이 성취감을 줄 수 있는 지표들)"*. 예전에는 <c>Kills / Time / Earned</c>
    /// 세 줄뿐이라 <b>어떤 런이었는지 구분이 안 갔다</b> — 3노드에서 죽은 판과 완주한 판이 같은 모양이다.</para>
    ///
    /// <para>🔑 <b>새로 잰 값은 하나뿐이다</b>(<see cref="WaveManager.BiggestHit"/>).
    /// 나머지는 이미 굴러다니던 것을 모았다 — 층·레벨·직업 사슬·보유 아이템.
    /// 성취감은 새 계측이 아니라 <b>이미 있는 숫자를 안 버리는 것</b>에서 나온다.</para>
    ///
    /// <para>🔴 기호는 <b>폰트 문자표에 있는 것만</b> 쓴다 (<c>I-60</c>·<c>B4</c>) —
    /// U+2192(→) 와 U+00B7(·) 는 굽혀 있고, U+25B8(▸) 같은 건 <b>빈칸으로 나온다.</b></para>
    /// </summary>
    private string BuildSummary(int kills, float time, int gold)
    {
        var gm   = GameManager.Instance;
        var wave = gm != null ? gm.WaveManager : null;
        var exp  = gm != null ? gm.ExpManager  : null;

        float best  = wave != null ? wave.BiggestHit  : 0f;
        int   level = exp  != null ? exp.CurrentLevel : 1;

        var sb = new System.Text.StringBuilder();
        sb.Append($"Layer  {LayerScaling.Layer + 1}        Level  {level}\n");
        sb.Append($"Kills  {kills}        Time  {FormatTime(time)}\n");
        sb.Append($"Biggest hit  {Mathf.RoundToInt(best)}        Earned  {gold} G\n");

        // 직업 사슬 — 승급한 판은 여기서 바로 티가 난다.
        var player = PlayerStats.Current;
        if (player != null && player.ClassChain.Count > 0)
        {
            sb.Append('\n');
            for (int i = 0; i < player.ClassChain.Count; i++)
            {
                if (player.ClassChain[i] == null) continue;
                if (i > 0) sb.Append("  →  ");
                sb.Append(player.ClassChain[i].ClassName);
            }
            sb.Append('\n');
        }

        // 🔴 <b>아이템은 여기 안 적는다</b> (D83 · 사용자 요구).
        //    판정은 *"너무 난잡해 — 아이작의 번제 같이 아이콘과 Lv 로 간단하게.
        //    너무 글자를 많이 쓰지 마"* 였다. 아이템 16종이 글자로 늘어서면
        //    이 상자에서 <b>가장 긴 줄</b>이 되고, 그게 난잡함의 대부분이었다.
        //    ⇒ <see cref="BuildItemChips"/> 가 아이콘으로 그린다.
        return sb.ToString();
    }

    /// <summary>
    /// 먹은 아이템을 <b>아이콘 격자</b>로 그린다 (D83 · 사용자 요구).
    ///
    /// <para>🔑 <b>새 부품이 0개다</b> — TAB 스탯 창(<c>D46</c>)과 HUD 줄(<c>D82</c>)이 쓰는
    /// <c>Prefab_ItemChip</c> 을 그대로 쓴다. 여기서는 칸이 넉넉하므로 <b>Lv 글자를 켠 채로</b> 둔다
    /// (HUD 줄은 44px 라 껐다).</para>
    ///
    /// <para>🔴 배선이 없으면 조용히 넘어간다 — 씬을 못 만진 상태에서도 화면은 떠야 한다.</para>
    /// </summary>
    private void BuildItemChips()
    {
        if (itemGrid == null || itemChipPrefab == null) return;

        var lm  = GameManager.Instance != null ? GameManager.Instance.LevelUpManager : null;
        var inv = lm != null ? lm.Inventory : null;

        int used = 0;
        if (inv != null)
            foreach (var kv in inv)
            {
                if (kv.Key == null) continue;
                var chip = GetChip(used++);
                chip.gameObject.SetActive(true);
                chip.SetCompact(false);          // 여기는 자리가 있으니 Lv 를 보여 준다
                chip.Bind(kv.Key, kv.Value);
            }

        for (int i = used; i < _chips.Count; i++)
            _chips[i].gameObject.SetActive(false);
    }

    private ItemChipUI GetChip(int index)
    {
        while (_chips.Count <= index)
        {
            var go   = Instantiate(itemChipPrefab, itemGrid);
            var chip = go.GetComponent<ItemChipUI>();
            if (chip == null) chip = go.AddComponent<ItemChipUI>();
            _chips.Add(chip);
        }
        return _chips[index];
    }

    private static string FormatTime(float seconds)
    {
        int m = (int)(seconds / 60f);
        int s = (int)(seconds % 60f);
        return $"{m:00}:{s:00}";
    }
}
