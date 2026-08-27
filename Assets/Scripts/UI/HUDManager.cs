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

    // ── 옵션 버튼 (우측 상단) ───────────────────────────────
    [Header("옵션 버튼")]
    [SerializeField] private Button optionsButton;

    // ── 레퍼런스 ─────────────────────────────────────────────
    private PlayerStats        _playerStats;
    private ExperienceManager  _expManager;

    // ── 레벨업 연출 ──────────────────────────────────────────
    [Header("레벨업 연출")]
    [SerializeField] private Animator levelUpAnimator;    // "LevelUp" 트리거가 있는 Animator
    [SerializeField] private GameObject levelUpEffect;    // 파티클 등

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
            var meta = gm.MetaProgression;
            int gold = meta != null ? meta.Currency : 0;
            if (gold != _lastCurrency)
            {
                _lastCurrency     = gold;
                currencyText.text = $"{gold} G";
            }
        }
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

        if (levelUpEffect != null)
        {
            var player = FindFirstObjectByType<PlayerController>();
            if (player != null)
                Instantiate(levelUpEffect, player.transform.position, Quaternion.identity);
        }
    }

    private void RefreshXP()
    {
        if (_expManager == null) return;
        levelText.text = $"Lv. {_expManager.CurrentLevel}";
        OnXpChanged(_expManager.CurrentXp, _expManager.XpToNext);
    }
}
