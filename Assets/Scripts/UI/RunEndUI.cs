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
            // 🔴 상자를 카드의 빈 공간에 맞춘다. 제목 아래(+110) ~ 버튼 위(-124) 사이 234px 다.
            //    예전 값 600x180(y +20)은 세 줄짜리 요약에 맞춘 크기라 아이템 줄이 들어갈 자리가 없다.
            var rt = summaryText.rectTransform;
            rt.sizeDelta        = new Vector2(680f, 230f);
            rt.anchoredPosition = new Vector2(0f, -5f);

            summaryText.enableAutoSizing = true;
            summaryText.fontSizeMin      = 15f;
            summaryText.fontSizeMax      = 28f;
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

        // 보유 아이템 — "내가 뭘 모았나" 가 이 화면의 절반이다.
        var lm = gm != null ? gm.LevelUpManager : null;
        if (lm != null && lm.Inventory.Count > 0)
        {
            sb.Append('\n');
            bool first = true;
            foreach (var kv in lm.Inventory)
            {
                if (kv.Key == null) continue;
                if (!first) sb.Append("  ·  ");
                sb.Append($"{kv.Key.ItemName} Lv{kv.Value}");
                first = false;
            }
        }
        return sb.ToString();
    }

    private static string FormatTime(float seconds)
    {
        int m = (int)(seconds / 60f);
        int s = (int)(seconds % 60f);
        return $"{m:00}:{s:00}";
    }
}
