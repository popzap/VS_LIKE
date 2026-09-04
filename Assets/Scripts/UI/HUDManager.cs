using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 인게임 HUD 전체를 관리한다.
/// ┌──────────────────────────────────────────────────────────┐
/// │ [포트레이트+HP바]      [Lv.N / XP바]      [톱니바퀴]  │
/// └──────────────────────────────────────────────────────────┘
/// </summary>
public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; private set; }

    // 🔴 컴파일 반영 확인용 (D27).
    public const int Version = 2;   // 2 = 레벨업 파동을 패널 닫힘으로 옮김 (D43)

    // ── 포트레이트 (좌측 상단) ───────────────────────────────
    [Header("포트레이트")]
    [SerializeField] private Image   portraitImage;        // 원형 마스크 안의 얼굴 이미지
    [SerializeField] private Sprite  faceHealthy;          // HP 75% 이상
    [SerializeField] private Sprite  faceNormal;           // HP 50~75%
    [SerializeField] private Sprite  faceWorried;          // HP 25~50%
    [SerializeField] private Sprite  faceCritical;         // HP 25% 이하
    [SerializeField] private Image   statusDot;            // 포트레이트 우하단 상태 점

    // ── HP 바 ────────────────────────────────────────────────
    [Header("HP 바")]
    [SerializeField] private Slider           hpSlider;
    [SerializeField] private Image            hpFill;
    [SerializeField] private TextMeshProUGUI  hpText;

    private static readonly Color ColorHpHigh    = new(0.13f, 0.77f, 0.37f); // 초록
    private static readonly Color ColorHpMid     = new(0.92f, 0.70f, 0.00f); // 노랑
    private static readonly Color ColorHpLow     = new(0.96f, 0.60f, 0.09f); // 주황
    private static readonly Color ColorHpCrit    = new(0.93f, 0.30f, 0.29f); // 빨강

    // ── 경험치 / 레벨 (상단 중앙) ───────────────────────────
    [Header("레벨 & 경험치")]
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Slider          xpSlider;
    [SerializeField] private TextMeshProUGUI xpText;

    // ── 웨이브 정보 (상단 중앙 / 우측 상단) ─────────────────
    [Header("웨이브 정보")]
    [SerializeField] private TextMeshProUGUI timerText;      // 클리어까지 남은 시간
    [SerializeField] private TextMeshProUGUI killText;       // 런 누적 처치 수
    [SerializeField] private TextMeshProUGUI currencyText;   // 보유 골드

    // 타이머가 이 시간 아래로 내려가면 붉게 물들여 "곧 끝난다"를 알린다.
    private const float TimerUrgentThreshold = 10f;
    private static readonly Color ColorTimerNormal = new(0.92f, 0.92f, 0.88f);
    private static readonly Color ColorTimerUrgent = new(0.96f, 0.35f, 0.30f);

    // ── 설치 대기 건물 (B3) ─────────────────────────────────
    //
    // `Z` 를 누르면 대기열 맨 앞이 나가는데, 그게 뭔지 화면에 안 나와서
    // "Village 를 얻었는데 Turret 이 나온다"로 보였다. 대기열은 정상 동작 중이고
    // (건물은 레벨이 오르면 MaxCount 가 늘어 대기열에 조용히 더 쌓인다)
    // 진짜 문제는 **무엇이 설치될지 모른다**는 것이라 표시만 한다.
    [Header("설치 대기 건물")]
    [SerializeField] private TextMeshProUGUI buildPromptText;

    // ── 옵션 버튼 (우측 상단) ───────────────────────────────
    [Header("옵션 버튼")]
    [SerializeField] private Button optionsButton;

    // ── 레퍼런스 ─────────────────────────────────────────────
    private PlayerStats        _playerStats;
    private ExperienceManager  _expManager;

    // ── 레벨업 연출 ──────────────────────────────────────────
    [Header("레벨업 연출")]
    [SerializeField] private Animator levelUpAnimator;    // "LevelUp" 트리거가 있는 Animator
    [Tooltip("레벨업 파동(Fx_LevelUp). SceneWiring.csv 의 HUDManager,levelUpEffect 로 배선한다. "
           + "🔴 여기서 바로 띄우지 않는다 — PlayLevelUpEffect() 를 LevelUpManager 가 부른다.")]
    [SerializeField] private GameObject levelUpEffect;

    // ────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        _playerStats = FindFirstObjectByType<PlayerStats>();
        _expManager  = ExperienceManager.Instance;

        optionsButton.onClick.AddListener(PauseMenuUI.Instance.Open);

        // 이벤트 구독
        _expManager.OnXpChanged  += OnXpChanged;
        _expManager.OnLevelUp    += OnLevelUp;

        // 초기값 세팅
        RefreshHP();
        RefreshXP();
    }

    private void Update()
    {
        RefreshWaveInfo();   // 타이머 / 킬 / 골드

        if (_playerStats == null) return;
        RefreshHP();   // HP는 매 프레임 체크 (공격 받을 때마다 변함)
    }

    private void OnDestroy()
    {
        if (_expManager == null) return;
        _expManager.OnXpChanged -= OnXpChanged;
        _expManager.OnLevelUp   -= OnLevelUp;
    }

    // ── 웨이브 정보 갱신 (타이머 / 킬 / 골드) ────────────────
    //
    // ⚠️ WaveManager 를 이벤트로 구독하지 않고 폴링한다.
    //    GameManager.Instance.WaveManager 는 GameManager.Start() 에서 채워지는데
    //    모든 Awake 가 모든 Start 보다 먼저 돌고, Start 끼리의 순서는 보장되지 않는다.
    //    Start 에서 구독하면 조용히 null 을 잡고 기능만 죽는다 (I-8 · I-38 과 같은 함정).

    private int _lastKills    = -1;
    private int _lastCurrency = -1;
    private int _lastTimerSec = -1;

    private void RefreshWaveInfo()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        // 남은 시간 ─ 초 단위가 바뀔 때만 문자열을 다시 만든다 (매 프레임 할당 방지)
        if (timerText != null)
        {
            var wave = gm.WaveManager;
            bool show = wave != null && wave.IsWaveActive && wave.WaveRemainingTime >= 0f;

            if (timerText.gameObject.activeSelf != show)
                timerText.gameObject.SetActive(show);

            if (show)
            {
                int sec = Mathf.CeilToInt(wave.WaveRemainingTime);
                if (sec != _lastTimerSec)
                {
                    _lastTimerSec  = sec;
                    timerText.text  = $"{sec / 60:0}:{sec % 60:00}";
                    timerText.color = sec <= TimerUrgentThreshold ? ColorTimerUrgent : ColorTimerNormal;
                }
            }
            else _lastTimerSec = -1;
        }

        // 처치 수 ─ 런 누적
        if (killText != null)
        {
            var wave  = gm.WaveManager;
            int kills = wave != null ? wave.TotalKillCount : 0;
            if (kills != _lastKills)
            {
                _lastKills   = kills;
                killText.text = $"{kills} Kills";
            }
        }

        // 골드
        if (currencyText != null)
        {
            // HUD 는 **런 골드**를 보여 준다 — 지금 상점에서 쓸 수 있는 돈이 이쪽이다 (TODO §2-B).
            int gold = gm.RunGold;
            if (gold != _lastCurrency)
            {
                _lastCurrency     = gold;
                currencyText.text = $"{gold} G";
            }
        }

        RefreshBuildPrompt(gm);
    }

    // ── 설치 대기 건물 (B3) ──────────────────────────────────

    private BuildingData _lastPending;
    private int          _lastPendingCount = -1;

    /// <summary>
    /// 다음에 `Z` 로 나갈 건물을 알린다.
    ///
    /// <para>원래 대기열이 순수 FIFO 라, 레벨업으로 조용히 쌓인 예전 건물이
    /// 새로 얻은 건물보다 먼저 나왔다(B3). 이 표시는 그 <b>B안</b>이었다 —
    /// 순서는 그대로 두고 무엇이 나올지 보이게만 했다.
    /// 순서 자체는 나중에 <b>C안</b>으로 고쳤다(<see cref="BuildingManager"/> 참고):
    /// 처음 해금된 것이 증설분보다 먼저 나온다.</para>
    ///
    /// <para>그래도 이 표시는 남긴다. 대기열이 여러 개일 때 <b>다음에 무엇이 나오는지</b>는
    /// 여전히 알 수 없기 때문이다.</para>
    /// </summary>
    private void RefreshBuildPrompt(GameManager gm)
    {
        if (buildPromptText == null) return;

        var bm      = gm.BuildingMgr;
        var pending = bm != null ? bm.NextPending : null;
        int count   = bm != null ? bm.PendingCount : 0;

        if (pending == null || count <= 0)
        {
            if (buildPromptText.gameObject.activeSelf)
                buildPromptText.gameObject.SetActive(false);
            _lastPending      = null;
            _lastPendingCount = -1;
            return;
        }

        if (!buildPromptText.gameObject.activeSelf)
            buildPromptText.gameObject.SetActive(true);

        // 문자열은 바뀔 때만 만든다 — 이 메서드는 매 프레임 돈다.
        if (pending == _lastPending && count == _lastPendingCount) return;
        _lastPending      = pending;
        _lastPendingCount = count;

        string name = string.IsNullOrEmpty(pending.BuildingName) ? pending.name : pending.BuildingName;
        // 남은 수를 같이 보여야 "왜 새로 얻은 게 안 나오나"가 설명된다.
        buildPromptText.text = count > 1 ? $"[Z] Build: {name}  (+{count - 1} queued)"
                                         : $"[Z] Build: {name}";
    }

    // ── HP 갱신 ──────────────────────────────────────────────

    private float _lastHp = -1;

    private void RefreshHP()
    {
        if (_playerStats == null) return;
        float hp    = _playerStats.CurrentHp;
        float maxHp = _playerStats.Final.MaxHp;
        if (Mathf.Approximately(hp, _lastHp)) return;
        _lastHp = hp;

        float ratio = Mathf.Clamp01(hp / maxHp);
        hpSlider.value = ratio;
        hpText.text    = $"{Mathf.CeilToInt(hp)} / {Mathf.RoundToInt(maxHp)}";

        // 색상 + 표정
        UpdateHpColor(ratio);
        UpdatePortrait(ratio);
    }

    private void UpdateHpColor(float ratio)
    {
        if      (ratio > 0.75f) hpFill.color = ColorHpHigh;
        else if (ratio > 0.50f) hpFill.color = ColorHpMid;
        else if (ratio > 0.25f) hpFill.color = ColorHpLow;
        else                    hpFill.color = ColorHpCrit;
    }

    private void UpdatePortrait(float ratio)
    {
        if (portraitImage == null) return;

        if      (ratio > 0.75f) portraitImage.sprite = faceHealthy;
        else if (ratio > 0.50f) portraitImage.sprite = faceNormal;
        else if (ratio > 0.25f) portraitImage.sprite = faceWorried;
        else                    portraitImage.sprite = faceCritical;

        // 상태 점 색상
        if (statusDot != null)
            statusDot.color = ratio > 0.25f ? ColorHpHigh : ColorHpCrit;
    }

    // ── XP 갱신 ──────────────────────────────────────────────

    private void OnXpChanged(int current, int toNext)
    {
        xpSlider.value = toNext > 0 ? (float)current / toNext : 0f;
        xpText.text    = $"{current} / {toNext}";
    }

    private void OnLevelUp(int newLevel)
    {
        levelText.text = $"Lv. {newLevel}";

        // ⚠️ C# 의 ?. 은 Unity 의 "가짜 null"(미할당 직렬화 필드)을 걸러내지 못한다.
        // 반드시 != null 로 검사할 것. ?. 를 쓰면 UnassignedReferenceException 이 터지고,
        // 그 예외가 CollectXp 밖으로 전파되어 레벨업 패널이 아예 안 뜬다.
        if (levelUpAnimator != null) levelUpAnimator.SetTrigger("LevelUp");

        // 🔴 파동은 여기서 안 띄운다 (D43).
        //    이 함수가 도는 바로 그 프레임에 레벨업 패널이 열리는데, 캔버스가
        //    ScreenSpaceOverlay 라 월드 스프라이트는 무조건 그 아래다 — sortingOrder 로도 못 이긴다.
        //    D41 에서 파동이 "생기기는 하는데 한 번도 안 보이는" 상태였다.
        //    ⇒ 패널이 실제로 닫히는 시점에 LevelUpManager 가 PlayLevelUpEffect() 를 부른다.
    }

    /// <summary>
    /// 레벨업 파동을 플레이어 자리에 띄운다. <see cref="LevelUpManager.HidePanel"/> 이 부른다.
    ///
    /// <para>🔴 <c>?.</c> 를 쓰지 않는다 — 미할당 직렬화 필드는 "가짜 null" 이라
    /// <c>?.</c> 가 통과시키고 예외가 호출 사슬 밖으로 샌다 (I-24).
    /// 여기서 새면 <b>레벨업 패널이 닫히다 만다.</b></para>
    /// </summary>
    public void PlayLevelUpEffect()
    {
        if (levelUpEffect == null) return;
        var player = FindFirstObjectByType<PlayerController>();
        if (player == null) return;
        Instantiate(levelUpEffect, player.transform.position, Quaternion.identity);
    }

    private void RefreshXP()
    {
        if (_expManager == null) return;
        levelText.text = $"Lv. {_expManager.CurrentLevel}";
        OnXpChanged(_expManager.CurrentXp, _expManager.XpToNext);
    }
}
