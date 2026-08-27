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
    public static PauseMenuUI Instance { get; private set; }

    [Header("패널")]
    [SerializeField] private GameObject pausePanel;       // 반투명 배경 + 메뉴 카드 포함
    [SerializeField] private CanvasGroup canvasGroup;     // 페이드 인/아웃용

    [Header("버튼")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button optionButton;
    [SerializeField] private Button quitButton;

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

        pausePanel.SetActive(false);
        if (canvasGroup) canvasGroup.alpha = 0;
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
    }

    public void Close()
    {
        if (!_isOpen) return;

        _isOpen        = false;
        _targetAlpha   = 0f;
        Time.timeScale = 1f;

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
