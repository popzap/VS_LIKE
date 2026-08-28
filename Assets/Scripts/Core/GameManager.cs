using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public enum GameState
{
    MainMenu,
    StageMap,
    Wave,
    LevelUp,
    Shop,
    Event,
    Paused,
    GameOver,
    Victory,
    MetaScreen,

    // ⚠️ 새 상태는 반드시 "맨 끝"에 추가할 것.
    // enum 은 씬에 정수로 직렬화된다 (StateVisibilityBinder.visibleStates 등).
    // 중간에 끼워 넣으면 그 뒤 값이 전부 한 칸씩 밀려 기존 배선이 조용히 어긋난다.
    ClassSelect
}

/// <summary>
/// 게임 전체 상태를 관리하는 싱글톤 매니저.
/// 모든 시스템의 진입점 역할을 한다.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentState { get; private set; }

    [Header("Events")]
    public UnityEvent<GameState> OnStateChanged = new();

    [Header("직업")]
    [Tooltip("선택 가능한 직업 목록. SceneWiring.csv 의 GameManager,classes 로 배선한다.")]
    [SerializeField] private CharacterClassData[] classes;
    [Tooltip("선택 화면에서 아무것도 고르지 않았을 때 쓰이는 인덱스 (초기 하이라이트 위치이기도 하다).")]
    [SerializeField] private int defaultClassIndex;

    [Header("스테이지 클리어 골드 보상")]
    [SerializeField] private int normalClearReward = 8;
    [SerializeField] private int eliteClearReward  = 20;
    [SerializeField] private int bossClearReward   = 60;

    // ── 런타임 참조 ──────────────────────────────────────────────
    public StageMapManager        StageMap        { get; private set; }
    public WaveManager            WaveManager     { get; private set; }
    public LevelUpManager         LevelUpManager  { get; private set; }
    public MetaProgressionManager MetaProgression { get; private set; }
    public ExperienceManager      ExpManager      { get; private set; }
    public BuildingManager        BuildingMgr     { get; private set; }
    public ShopManager            ShopMgr         { get; private set; }

    // 씬 리로드를 넘어 살아남아야 하는 값 (Retry 여부 / 고른 직업)
    private static bool _autoStartRunOnLoad;
    private static int  _selectedClassIndex = -1;   // -1 = 아직 고르지 않음

    // 레벨업 패널이 뜨기 직전의 상태. 선택이 끝나면 이 상태로 돌아간다.
    private GameState _stateBeforeLevelUp = GameState.Wave;

    private void Awake()
    {
        // 단일 씬 구성이라 DontDestroyOnLoad 를 쓰지 않는다.
        // 씬을 다시 로드하면 매니저 전체가 새로 생성되어 런타임 상태가 완전히 초기화된다.
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        StageMap        = FindFirstObjectByType<StageMapManager>();
        WaveManager     = FindFirstObjectByType<WaveManager>();
        LevelUpManager  = FindFirstObjectByType<LevelUpManager>();
        MetaProgression = FindFirstObjectByType<MetaProgressionManager>();
        ExpManager      = FindFirstObjectByType<ExperienceManager>();
        BuildingMgr     = FindFirstObjectByType<BuildingManager>();
        ShopMgr         = FindFirstObjectByType<ShopManager>();

        MetaProgression.Load();

        if (_autoStartRunOnLoad)
        {
            _autoStartRunOnLoad = false;
            StartRun();
        }
        else
        {
            ChangeState(GameState.MainMenu);
        }
    }

    /// <summary>
    /// 씬을 다시 로드해 런을 초기화한다.
    /// </summary>
    /// <param name="autoStartRun">true 면 로드 직후 곧바로 새 런을 시작(재도전), false 면 메인 메뉴.</param>
    public static void ReloadScene(bool autoStartRun)
    {
        _autoStartRunOnLoad = autoStartRun;
        Time.timeScale      = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ── 상태 전환 ────────────────────────────────────────────────

    public void ChangeState(GameState newState)
    {
        // 레벨업은 "덮어쓰는" 상태가 아니라 "끼어드는" 상태다.
        // 마을(VillageBuilding)이 스테이지 맵에서도 경험치를 올리기 때문에
        // 웨이브 밖에서 레벨업이 터질 수 있고, 그때 돌아갈 곳을 기억해 둬야 한다.
        if (newState == GameState.LevelUp && CurrentState != GameState.LevelUp)
            _stateBeforeLevelUp = CurrentState;

        CurrentState = newState;
        OnStateChanged.Invoke(newState);
        Debug.Log($"[GameManager] State → {newState}");

        UpdateBgm(newState);
    }

    /// <summary>
    /// 상태에 맞는 배경음으로 갈아탄다. 모든 화면 전환이 <see cref="ChangeState"/> 를
    /// 지나므로 여기 한 곳에만 두면 된다 — 화면마다 흩어 놓으면 빠뜨리는 경로가 생긴다.
    ///
    /// <para>같은 곡이면 <c>PlayBgm</c> 이 알아서 무시하므로, 웨이브 도중 레벨업으로
    /// 상태가 들락거려도 음악이 처음부터 다시 시작되지 않는다.</para>
    /// </summary>
    private void UpdateBgm(GameState state)
    {
        switch (state)
        {
            case GameState.MainMenu:
            case GameState.MetaScreen:
                AudioManager.PlayMusic(BgmId.MainMenu);
                break;

            case GameState.StageMap:
            case GameState.Shop:
            case GameState.Event:
            case GameState.Victory:
                AudioManager.PlayMusic(BgmId.Shop);
                break;

            case GameState.Wave:
                AudioManager.PlayMusic(WaveManager != null && WaveManager.IsBossWave
                                       ? BgmId.WaveBoss : BgmId.WaveNormal);
                break;

            case GameState.GameOver:
                if (AudioManager.Instance != null) AudioManager.Instance.StopBgm();
                break;

            // LevelUp · Paused 는 웨이브 위에 "끼어드는" 상태다. 음악을 바꾸지 않는다 —
            // 레벨업이 뜰 때마다 곡이 끊기면 전투의 흐름이 매번 잘린다.
            case GameState.LevelUp:
            case GameState.Paused:
                break;
        }
    }

    // ── 런 흐름 ──────────────────────────────────────────────────

    /// <summary>메인 메뉴에서 "시작" 버튼 클릭 시 호출.</summary>
    public void StartRun()
    {
        // ItemData.CurrentLevel 은 SO 애셋에 남는 런타임 값이라 씬 리로드만으로는 지워지지 않는다.
        LevelUpManager.ResetRunState();
        if (BuildingMgr != null) BuildingMgr.ResetRunState();
        ApplySelectedClass();
        StageMap.GenerateMap();
        ChangeState(GameState.StageMap);
    }

    // ── 직업 ─────────────────────────────────────────────────────

    public CharacterClassData[] Classes => classes;

    /// <summary>현재 고른 직업의 인덱스. 아직 고르지 않았으면 <c>defaultClassIndex</c>.</summary>
    public int SelectedClassIndex => _selectedClassIndex >= 0 ? _selectedClassIndex : defaultClassIndex;

    /// <summary>이번 런에 쓸 직업. 목록이 비어 있으면 null.</summary>
    public CharacterClassData SelectedClass
    {
        get
        {
            if (classes == null || classes.Length == 0) return null;
            int idx = _selectedClassIndex >= 0 ? _selectedClassIndex : defaultClassIndex;
            return classes[Mathf.Clamp(idx, 0, classes.Length - 1)];
        }
    }

    /// <summary>
    /// 직업 선택 (<see cref="ClassSelectUI"/> 가 호출).
    /// static 이라 Retry(씬 리로드) 를 넘어 유지된다 — 재도전은 직업 선택을 다시 거치지 않는다.
    /// </summary>
    public void SelectClass(int index) => _selectedClassIndex = index;

    /// <summary>런 시작 시 직업 스탯을 반영하고 시작 무기를 지급한다.</summary>
    private void ApplySelectedClass()
    {
        var cls = SelectedClass;
        if (cls == null)
        {
            Debug.LogWarning("[GameManager] 직업 목록이 비어 있다. 시작 무기 없이 진행한다.");
            return;
        }

        PlayerStats.Current?.ApplyClass(cls);

        if (cls.StartingWeapon == null)
        {
            Debug.LogWarning($"[GameManager] 직업 '{cls.name}' 에 StartingWeapon 이 없다.");
            return;
        }

        WeaponManager.Instance?.AddOrUpgradeWeapon(cls.StartingWeapon, Mathf.Max(1, cls.StartingWeaponLevel));
        Debug.Log($"[GameManager] 직업 '{cls.ClassName}' — 시작 무기 {cls.StartingWeapon.WeaponName} Lv{cls.StartingWeaponLevel}");
    }

    /// <summary>플레이어가 스테이지 노드를 선택할 때 StageMapUI에서 호출.</summary>
    public void OnStageNodeSelected(StageNode node)
    {
        switch (node.StageType)
        {
            case StageType.Normal:
            case StageType.Elite:
            case StageType.Boss:
                WaveManager.StartWave(node);
                ChangeState(GameState.Wave);
                break;

            case StageType.Shop:
                ShopMgr?.OpenShop(node);
                ChangeState(GameState.Shop);
                break;

            case StageType.Event:
                // 이벤트는 즉시 해소되면서 스스로 StageMap 으로 전환하므로
                // 상태 전환을 먼저 하고 트리거해야 덮어쓰기가 발생하지 않는다.
                ChangeState(GameState.Event);
                if (EventManager.Instance != null)
                {
                    EventManager.Instance.TriggerRandomEvent(node);
                }
                else
                {
                    StageMap.AdvanceToNext(node);
                    ChangeState(GameState.StageMap);
                }
                break;
        }
    }

    /// <summary>웨이브 클리어 시 WaveManager에서 호출.</summary>
    public void OnWaveCleared(StageNode clearedNode)
    {
        int reward = clearedNode.StageType switch
        {
            StageType.Boss  => bossClearReward,
            StageType.Elite => eliteClearReward,
            _               => normalClearReward
        };
        GrantGold(reward);

        if (clearedNode.StageType == StageType.Boss)
        {
            MetaProgression.RegisterRunResult(WaveManager.TotalKillCount);
            MetaProgression.Save();
            ChangeState(GameState.Victory);
            return;
        }

        StageMap.AdvanceToNext(clearedNode);

        // 결과창이 있으면 먼저 띄우고, 닫힐 때 StageClearUI 가 StageMap 으로 전환한다.
        if (StageClearUI.Instance != null) StageClearUI.Instance.Show(clearedNode);
        else                               ChangeState(GameState.StageMap);
    }

    /// <summary>플레이어 사망 시 PlayerStats에서 호출.</summary>
    public void OnPlayerDied()
    {
        MetaProgression.RegisterRunResult(WaveManager.TotalKillCount);
        MetaProgression.Save();
        ChangeState(GameState.GameOver);
    }

    /// <summary>
    /// 게임플레이로 획득하는 골드는 전부 이 함수를 거친다.
    /// 플레이어의 GoldGain 배율(패시브/메타 강화)이 여기서 적용된다.
    /// </summary>
    public void GrantGold(int baseAmount)
    {
        if (baseAmount <= 0) return;

        float mult  = PlayerStats.Current != null ? PlayerStats.Current.Final.GoldGain : 1f;
        int   final = Mathf.Max(1, Mathf.RoundToInt(baseAmount * mult));
        MetaProgression.AddCurrency(final);
    }

    // ── 히트스톱 ─────────────────────────────────────────────────
    //
    // 큰 타격(엘리트/보스 처치) 순간에 시간을 아주 짧게 멈춰 "묵직함"을 만든다.
    //
    // ⚠️ Time.timeScale 은 이 프로젝트에서 여러 곳이 공유한다 —
    //    WaveManager.PauseWave() 가 0, 일시정지·레벨업·스테이지 결과창이 각각 0/1 을 쓴다.
    //    아무 때나 끼어들어 1f 로 되돌리면 일시정지가 저절로 풀린다.
    //    그래서 (1) 웨이브 진행 중일 때만 시작하고 (2) 이미 1f 가 아니면 (누가 멈춰 놨으면)
    //    아예 손대지 않으며 (3) 복구 시점에도 여전히 Wave 인지 다시 확인한다.

    private Coroutine _hitstopRoutine;

    /// <summary>지정한 실시간(초) 동안 게임을 멈춘다. 웨이브 진행 중에만 동작한다.</summary>
    public void DoHitstop(float seconds)
    {
        if (CurrentState != GameState.Wave) return;
        if (!Mathf.Approximately(Time.timeScale, 1f)) return;   // 누가 이미 멈춰 놨다
        if (_hitstopRoutine != null) return;                    // 겹치면 무시 (연장하지 않는다)

        _hitstopRoutine = StartCoroutine(HitstopRoutine(seconds));
    }

    private IEnumerator HitstopRoutine(float seconds)
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(seconds);

        // 멈춰 있는 사이에 레벨업 패널이 뜨거나 웨이브가 끝났을 수 있다.
        // 그런 경우 timeScale 은 그쪽 주인에게 맡기고 손대지 않는다.
        if (CurrentState == GameState.Wave) Time.timeScale = 1f;
        _hitstopRoutine = null;
    }

    /// <summary>레벨업 선택 완료 후 LevelUpManager에서 호출.</summary>
    public void OnLevelUpCompleted()
    {
        ChangeState(_stateBeforeLevelUp);

        // ResumeWave 는 웨이브가 돌고 있지 않으면 아무것도 하지 않는다 (timeScale 도 안 돌린다).
        // 웨이브 밖에서 레벨업했다면 여기서 직접 풀어 주지 않으면 게임이 얼어붙는다.
        if (_stateBeforeLevelUp == GameState.Wave) WaveManager.ResumeWave();
        else                                       Time.timeScale = 1f;
    }
}
