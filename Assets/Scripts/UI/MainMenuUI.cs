using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 게임 시작 시 표시되는 메인 메뉴.
/// Start → <see cref="GameManager.StartRun"/> 으로 런을 시작한다.
/// </summary>
public class MainMenuUI : GameStatePanel
{
    [Header("텍스트")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI currencyText;

    [Header("버튼")]
    [SerializeField] private Button startButton;
    [Tooltip("영구 강화 화면으로 간다 (D30). 재화 고리의 소비 쪽 입구다")]
    [SerializeField] private Button metaButton;
    [SerializeField] private Button optionButton;
    [SerializeField] private Button quitButton;

    [Header("옵션 서브 패널 (PauseMenuUI 와 공유)")]
    [SerializeField] private GameObject optionSubPanel;

    protected override bool IsVisibleIn(GameState state) => state == GameState.MainMenu;

    protected override void Awake()
    {
        base.Awake();

        // GameManager.Start(실행 순서 -100)가 곧바로 MainMenu 상태를 방송하므로
        // 리스너 등록은 Start 가 아니라 Awake 에서 해야 한다.
        if (startButton)  startButton .onClick.AddListener(OnStartClicked);
        if (metaButton)   metaButton  .onClick.AddListener(OnMetaClicked);
        if (optionButton) optionButton.onClick.AddListener(OnOptionClicked);
        if (quitButton)   quitButton  .onClick.AddListener(OnQuitClicked);
    }

    protected override void OnShown(GameState state)
    {
        Time.timeScale = 1f;

        if (titleText != null && string.IsNullOrEmpty(titleText.text))
            titleText.text = "VS_LIKE";

        RefreshCurrency();
    }

    // 메인 메뉴를 떠날 때 옵션창을 반드시 닫는다.
    // 안 닫으면 켜진 채로 남아 있다가, 전투 진입에서 앞의 패널들이 전부 꺼지는 순간
    // 갑자기 화면에 튀어나온다 (I-50).
    protected override void OnHidden()
    {
        if (optionSubPanel != null) optionSubPanel.SetActive(false);
    }

    private void RefreshCurrency()
    {
        if (currencyText == null) return;
        int gold = GameManager.Instance?.MetaProgression?.Currency ?? 0;
        currencyText.text = $"{gold} G";
    }

    // ── 버튼 핸들러 ──────────────────────────────────────────

    // Start 는 런을 바로 시작하지 않고 직업 선택 화면으로 넘긴다.
    // 실제 StartRun() 은 ClassSelectUI 의 Start 버튼이 부른다.
    // (직업 목록이 비어 있으면 고를 게 없으므로 예전처럼 곧바로 시작한다)
    private void OnStartClicked()
    {
        Time.timeScale = 1f;

        var classes = GameManager.Instance.Classes;
        if (classes != null && classes.Length > 0)
            GameManager.Instance.ChangeState(GameState.ClassSelect);
        else
            GameManager.Instance.StartRun();
    }

    // 영구 강화 화면. 상태만 바꾸면 MetaScreenUI 가 GameStatePanel 로 알아서 뜬다.
    private void OnMetaClicked()
    {
        Time.timeScale = 1f;
        AudioManager.Play(SfxId.UiSelect);
        GameManager.Instance.ChangeState(GameState.MetaScreen);
    }

    private void OnOptionClicked()
    {
        if (optionSubPanel != null) optionSubPanel.SetActive(true);
    }

    private void OnQuitClicked()
    {
        GameManager.Instance?.MetaProgression?.Save();
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
