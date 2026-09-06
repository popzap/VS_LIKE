using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ESC 또는 HUD 톱니바퀴 버튼 → 화면 중앙에 오버레이로 표시.
/// Time.timeScale = 0 으로 게임 일시정지.
/// </summary>
public class PauseMenuUI : MonoBehaviour
{
    // 🔴 컴파일 반영 확인용 (D27).
    public const int Version = 2;   // 2 = 한 상자 + 하단 버튼 4개 (D93) · 1 = 스탯·아이템·조작을 한 화면에 (D92)

    public static PauseMenuUI Instance { get; private set; }

    [Header("한 화면에 모은 것 (D92)")]
    [Tooltip("스탯·보유 아이템. 여닫는 건 이쪽이 하고, 저쪽은 값만 채운다.")]
    [SerializeField] private StatsPanelUI    statsPanel;

    [Tooltip("조작 안내. 🔴 글자는 코드가 정한다 — 씬에 두면 낡는다 (B11).")]
    [SerializeField] private TextMeshProUGUI controlsText;

    [Header("패널")]
    [SerializeField] private GameObject pausePanel;       // 반투명 배경 + 메뉴 카드 포함
    [SerializeField] private CanvasGroup canvasGroup;     // 페이드 인/아웃용

    [Header("버튼")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button optionButton;
    [SerializeField] private Button quitButton;

    [Tooltip("런을 포기하고 메인 메뉴로. 🔴 씬을 다시 로드한다 — 진행 중인 런은 사라진다.")]
    [SerializeField] private Button mainMenuButton;

    [Header("옵션 서브 패널 (별도 패널 연결)")]
    [SerializeField] private GameObject optionSubPanel;

    [Header("페이드 속도")]
    [SerializeField] private float fadeSpeed = 8f;

    private bool      _isOpen;
    private float     _targetAlpha;
    private GameState _stateBeforePause;

    /// <summary>일시정지를 허용하는 상태. 그 외(메인 메뉴·맵·상점·결과창)에서는 ESC 를 무시한다.</summary>
    private static bool CanPause(GameState s) => s == GameState.Wave;

    // ────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        resumeButton.onClick.AddListener(Close);
        optionButton.onClick.AddListener(OpenOption);
        quitButton  .onClick.AddListener(QuitGame);

        // 🔴 <c>?.</c> 를 쓰지 않는다 (I-24). 새로 추가한 버튼이라 배선이 빠져 있을 수 있다.
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(ReturnToMainMenu);

        pausePanel.SetActive(false);
        if (canvasGroup) canvasGroup.alpha = 0;

        ApplyControlsText();
    }

    /// <summary>
    /// 조작 안내 — <b>코드가 원본</b>이다 (B11 의 교훈).
    ///
    /// <para>🔑 참고 이미지(대전략)의 일시정지 화면이 <b>스탯 · 조작 · 버튼을 한 번에</b> 보여 준다.
    /// 사용자 요구가 그 모양이었다. <see cref="HelpPanel"/> 에도 같은 내용이 있지만
    /// 거기는 <b>메인 메뉴에서만</b> 열려서 <b>게임 중에는 볼 방법이 없었다.</b></para>
    ///
    /// <para>🔴 문자열은 영문이다 — 폰트가 Static 115자라 한글 글리프가 없다 (I-60).
    /// 가운뎃점(· U+00B7)은 문자표에 있으므로 써도 된다.</para>
    /// </summary>
    /// <remarks>
    /// 🔴 <b>키와 설명을 <c>&lt;pos&gt;</c> 로 한 줄에 붙이지 않는다</b> (D93).
    /// 한 상자로 합치면서 이 단이 <b>488 → 390</b> 으로 좁아졌는데,
    /// *"WASD · Arrow keys"* 만으로 26pt 에서 약 200px 이라 <c>&lt;pos=52%&gt;</c>(203px) 와 부딪힌다.
    /// <see cref="HelpPanel"/> 이 이미 쓰는 <b>키 한 줄 · 설명 들여쓰기</b> 방식으로 바꾸면
    /// <b>단 너비와 무관</b>해진다 — 그쪽도 같은 이유로 그렇게 바꿨다 (D85).
    /// </remarks>
    private const string ControlsBody =
        "<color=#F0C040>CONTROLS</color>\n" +
        "WASD  ·  Arrow keys\n" +
        "<indent=10%>Move</indent>\n" +
        "1   2   3\n" +
        "<indent=10%>Pick a card</indent>\n" +
        "Z\n" +
        "<indent=10%>Place a building</indent>\n" +
        "F7  ·  F8\n" +
        "<indent=10%>Zoom in  ·  out</indent>\n" +
        "ESC\n" +
        "<indent=10%>Pause  ·  Resume</indent>";

    private void ApplyControlsText()
    {
        if (controlsText == null) return;
        controlsText.text             = ControlsBody;
        controlsText.alignment        = TextAlignmentOptions.TopLeft;
        controlsText.enableAutoSizing = false;
        controlsText.fontSize         = 26f;
        controlsText.lineSpacing      = 10f;
        controlsText.overflowMode     = TextOverflowModes.Overflow;
        controlsText.richText         = true;
    }

    private void Update()
    {
        // ESC 토글
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (optionSubPanel != null && optionSubPanel.activeSelf)
                CloseOption();
            else if (_isOpen) Close();
            else              Open();
        }

        // 페이드 처리
        if (canvasGroup == null) return;
        canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, _targetAlpha, fadeSpeed * Time.unscaledDeltaTime);
        if (!_isOpen && Mathf.Approximately(canvasGroup.alpha, 0f))
            pausePanel.SetActive(false);
    }

    // ── Public API ───────────────────────────────────────────

    public void Open()
    {
        if (_isOpen) return;

        var gm = GameManager.Instance;
        if (gm == null || !CanPause(gm.CurrentState)) return;

        _stateBeforePause = gm.CurrentState;
        _isOpen           = true;
        pausePanel.SetActive(true);
        _targetAlpha      = 1f;
        Time.timeScale    = 0f;
        gm.ChangeState(GameState.Paused);

        // 🔑 <b>여는 김에 값을 채운다</b> (D92). 스탯 창이 TAB 에서 여기로 옮겨 왔다 —
        //    사용자 요구가 *"ESC 눌렀을때 게임도 멈추고 한번에 보여주는게 나을거 같아"* 였다.
        //    🔴 <c>SetActive(true)</c> <b>다음</b>에 부른다. 꺼져 있는 오브젝트에서는
        //    <c>TextMeshProUGUI</c> 의 레이아웃이 갱신되지 않아 첫 프레임이 빈 칸으로 뜬다.
        if (statsPanel != null) statsPanel.Refresh();
    }

    public void Close()
    {
        if (!_isOpen) return;

        _isOpen        = false;
        _targetAlpha   = 0f;
        Time.timeScale = 1f;

        // 옵션창을 열어 둔 채 Resume 을 누르면 옵션창만 전투 위에 남는다 (I-50).
        CloseOption();

        var gm = GameManager.Instance;
        // 무조건 Wave 로 되돌리면 맵·상점에서 일시정지했을 때 상태가 깨진다.
        if (gm != null && gm.CurrentState == GameState.Paused)
            gm.ChangeState(_stateBeforePause);
    }

    // ── 옵션 ────────────────────────────────────────────────

    private void OpenOption()
    {
        if (optionSubPanel != null) optionSubPanel.SetActive(true);
    }

    private void CloseOption()
    {
        if (optionSubPanel != null) optionSubPanel.SetActive(false);
    }

    // ── 메인 메뉴 ────────────────────────────────────────────

    /// <summary>
    /// 런을 포기하고 메인 메뉴로 (D93 · 사용자 요구 — 참고 이미지의 *"타이틀 화면"*).
    ///
    /// <para>🔑 <b><see cref="RunEndUI"/> 와 같은 경로를 쓴다</b> — <c>GameManager.ReloadScene(false)</c>.
    /// 흉내 내지 않는다: 그 안에 <c>timeScale</c> 복구와 <c>_autoStartRunOnLoad = false</c> 가 들어 있다.</para>
    ///
    /// <para>🔴 <b>나가기 전에 메타를 저장한다</b> — <see cref="QuitGame"/> 와 같은 이유다.
    /// 씬을 다시 로드하면 <b>이번 런에서 번 메타 골드가 그냥 사라진다.</b></para>
    ///
    /// <para>⚠️ <b>진행 중인 런은 버려진다.</b> 확인 창은 두지 않았다 — 참고 이미지도 바로 나간다.
    /// 되돌릴 수 없는 버튼이므로 <b>Quit 옆이 아니라 끝자리</b>에 뒀다.</para>
    /// </summary>
    private void ReturnToMainMenu()
    {
        var gm = GameManager.Instance;
        if (gm != null && gm.MetaProgression != null) gm.MetaProgression.Save();

        Time.timeScale = 1f;
        GameManager.ReloadScene(false);
    }

    // ── 종료 ────────────────────────────────────────────────

    private void QuitGame()
    {
        GameManager.Instance.MetaProgression.Save();
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
