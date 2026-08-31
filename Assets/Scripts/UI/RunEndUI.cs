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

        summaryText.text = $"Kills   {kills}\nTime    {FormatTime(time)}\nEarned  {gold} G";
    }

    private static string FormatTime(float seconds)
    {
        int m = (int)(seconds / 60f);
        int s = (int)(seconds % 60f);
        return $"{m:00}:{s:00}";
    }
}
