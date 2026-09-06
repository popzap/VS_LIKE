using System;
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

    // 🔴 이 셋만 메타 골드다 (영구 강화용). 처치·픽업 골드는 런 골드로 간다.
    [Header("스테이지 클리어 보상 (메타 골드)")]
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
    public EvolutionManager       EvolutionMgr    { get; private set; }

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

        // 🔴 저장된 화면 설정은 여기서 적용한다 (D47).
        //    OptionPanel 은 "열려야" Start 가 도는데, 해상도·품질은 게임이 켜지자마자 먹어야 한다.
        //    AudioManager 가 볼륨을 다루는 방식과 같다.
        DisplaySettings.ApplySaved();
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
        EvolutionMgr    = FindFirstObjectByType<EvolutionManager>();

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

            // 레벨업은 웨이브 위에 "끼어드는" 상태다. 음악을 바꾸지 않는다 —
            // 레벨업이 뜰 때마다 곡이 끊기면 전투의 흐름이 매번 잘린다.
            case GameState.LevelUp:
                break;

            // 일시정지는 다르다. 플레이어가 게임을 손에서 놓는 순간이므로 소리도 같이 멈춘다.
            // StopBgm 이 아니라 PauseBgm 인 이유 — 재개할 때 곡이 처음부터 다시 나오면
            // ESC 를 누를 때마다 도입부만 반복해서 듣게 된다.
            case GameState.Paused:
                if (AudioManager.Instance != null) AudioManager.Instance.PauseBgm();
                break;
        }
    }

    // ── 런 흐름 ──────────────────────────────────────────────────

    /// <summary>메인 메뉴에서 "시작" 버튼 클릭 시 호출.</summary>
    public void StartRun()
    {
        // ItemData.CurrentLevel 은 SO 애셋에 남는 런타임 값이라 씬 리로드만으로는 지워지지 않는다.
        // 🔴 안 되돌리면 다음 런 1층이 지난 런 10층 난이도로 시작한다.
        LayerScaling.Reset();

        LevelUpManager.ResetRunState();
        if (BuildingMgr != null)  BuildingMgr.ResetRunState();
        if (EvolutionMgr != null) EvolutionMgr.ResetRunState();

        // 런 골드는 런과 함께 태어나고 죽는다. Retry 는 씬을 다시 로드하지만
        // 메인 메뉴에서 곧바로 다시 시작하는 경로는 리로드가 없어 여기서 직접 지운다.
        RunGold          = 0;
        _pendingMetaGold = 0;
        OnRunGoldChanged?.Invoke(RunGold);

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

        // 🔴 WeaponManager 를 직접 부르지 말 것 (B6). 그러면 시작 무기가 LevelUpManager 의
        // 장부에 안 잡혀 무기 칸이 매 판 하나씩 모자라고, 카드에 신규로 다시 뜨며,
        // 진화 재료 판정에서도 빠진다. 반드시 이 경로로 지급한다.
        LevelUpManager.GrantStartingWeapon(cls.StartingWeapon, Mathf.Max(1, cls.StartingWeaponLevel));
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
        GrantMetaGold(reward);

        if (clearedNode.StageType == StageType.Boss)
        {
            MetaProgression.RegisterRunResult(WaveManager.TotalKillCount, WaveManager.TotalElapsedTime);
            SettleRun();
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
        MetaProgression.RegisterRunResult(WaveManager.TotalKillCount, WaveManager.TotalElapsedTime);
        SettleRun();
        MetaProgression.Save();
        ChangeState(GameState.GameOver);
    }

    // ── 재화 ─────────────────────────────────────────────────────
    //
    // 🔴 지갑이 둘이다 (TODO §2-B, 결정 3). 섞지 말 것.
    //
    //   런 골드 (RunGold)          — 처치·픽업·농장·이벤트로 번다. 상점과 리롤이 쓴다.
    //                                런이 끝나면 **소멸**한다. 저장되지 않는다.
    //   메타 골드 (MetaProgression) — 스테이지 클리어 보상으로만 번다. 영구 강화·해금이 쓴다.
    //
    // 예전에는 둘이 MetaProgression.Currency 하나였다. 10층 런의 처치 보상만 1,000G 를
    // 넘는데 상점 아이템이 5~12G 라, 상점도 메타도 값을 잡을 수가 없었다.
    //
    // ⚠️ 클리어 보상은 곧바로 MetaProgression 에 들어가지 않고 _pendingMetaGold 에 쌓였다가
    //    런이 끝날 때(SettleRun) 한 번에 넘어간다. 런 도중에 저장 없이 나가면 남지 않는다 —
    //    "런을 끝내야 메타 골드를 번다"가 규칙이다.

    /// <summary>이번 런에서 쓸 수 있는 골드. 상점 전용이며 런이 끝나면 사라진다.</summary>
    public int RunGold { get; private set; }

    /// <summary>런 골드가 바뀔 때마다 호출된다 (상점 UI 가 구독한다).</summary>
    public Action<int> OnRunGoldChanged;

    private int _pendingMetaGold;

    /// <summary>이번 런에서 정산될 메타 골드 (아직 MetaProgression 에 들어가지 않았다).</summary>
    public int PendingMetaGold => _pendingMetaGold;

    /// <summary>직전 런이 정산한 메타 골드. 런 종료 화면이 표시한다.</summary>
    public int LastSettledMetaGold { get; private set; }

    /// <summary>
    /// 게임플레이로 획득하는 <b>런 골드</b>는 전부 이 함수를 거친다.
    /// 플레이어의 GoldGain 배율(패시브/메타 강화)이 여기서 적용된다.
    /// </summary>
    public void GrantGold(int baseAmount)
    {
        if (baseAmount <= 0) return;

        float mult = PlayerStats.Current != null ? PlayerStats.Current.Final.GoldGain : 1f;
        AddRunGold(Mathf.Max(1, Mathf.RoundToInt(baseAmount * mult)));
    }

    /// <summary>배율 없이 런 골드를 더한다 (환급처럼 이미 계산이 끝난 금액용).</summary>
    public void AddRunGold(int amount)
    {
        if (amount <= 0) return;
        RunGold += amount;
        OnRunGoldChanged?.Invoke(RunGold);
    }

    /// <summary>런 골드를 쓴다. 모자라면 아무것도 하지 않고 false.</summary>
    public bool SpendRunGold(int amount)
    {
        if (amount <= 0 || RunGold < amount) return false;
        RunGold -= amount;
        OnRunGoldChanged?.Invoke(RunGold);
        return true;
    }

    /// <summary>
    /// 스테이지 클리어 보상 — <b>메타 골드</b>로 적립한다. 런이 끝날 때 정산된다.
    /// GoldGain 배율은 런 골드와 똑같이 적용된다.
    /// </summary>
    public void GrantMetaGold(int baseAmount)
    {
        if (baseAmount <= 0) return;

        float mult = PlayerStats.Current != null ? PlayerStats.Current.Final.GoldGain : 1f;
        AddPendingMetaGold(Mathf.Max(1, Mathf.RoundToInt(baseAmount * mult)));
    }

    /// <summary>
    /// 적립 메타 골드에 <b>배율 없이</b> 더한다 (D71 · 상점 골드 전환).
    ///
    /// <para>🔴 <see cref="GrantMetaGold"/> 를 쓰면 <c>GoldGain</c> 이 <b>두 번</b> 곱해진다 —
    /// 런 골드는 벌 때 이미 그 배율을 받았고, 전환은 그 돈을 옮기는 것뿐이다.
    /// Excalibur 특전(골드 2배)이 붙으면 차이가 그대로 두 배로 벌어진다.</para>
    /// </summary>
    public void AddPendingMetaGold(int amount)
    {
        if (amount <= 0) return;
        _pendingMetaGold += amount;
    }

    /// <summary>런 종료(사망/승리) 정산. 적립된 메타 골드를 넘기고 런 골드를 버린다.</summary>
    private void SettleRun()
    {
        LastSettledMetaGold = _pendingMetaGold;
        if (_pendingMetaGold > 0) MetaProgression.AddCurrency(_pendingMetaGold);
        Debug.Log($"[GameManager] 런 정산 — 메타 골드 +{_pendingMetaGold} · 남은 런 골드 {RunGold} 소멸");

        _pendingMetaGold = 0;
        RunGold          = 0;
        OnRunGoldChanged?.Invoke(RunGold);
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
        // 🔴 <b>아직 레벨업 중일 때만 되돌린다</b> (B17 안전망).
        //    <c>_stateBeforeLevelUp</c> 은 패널을 <b>열 때</b> 찍힌다. 그 사이에 다른 경로가
        //    상태를 바꿔 놨다면 그건 <b>더 최신 사실</b>이라 덮어쓰면 안 된다.
        //
        //    🔴 실제로 그렇게 깨졌다 — 상점에서 레벨업이 뜬 채 상점을 닫고 맵에서 노드를 골라
        //    <c>Wave</c> 로 들어갔는데, 뒤늦게 카드를 고르자 여기가 <b>게임을 다시 `Shop` 으로</b>
        //    끌어냈다. 상점은 이미 닫혔으니 <b>상태만 `Shop` 인 유령</b>이 된다.
        //
        //    ⚠️ 이 가드가 걸리면 <c>timeScale</c> 은 건드리지 않는다 — 지금 상태의 주인이
        //    따로 있다는 뜻이고, 남의 시간을 되돌리면 그쪽이 깨진다 (`DoHitstop` 과 같은 계약).
        if (CurrentState != GameState.LevelUp)
        {
            Debug.LogWarning($"[GameManager] 레벨업이 끝났는데 상태가 이미 {CurrentState} 다 — "
                           + $"'{_stateBeforeLevelUp}' 로 되돌리지 않는다 (B17)");
            return;
        }

        ChangeState(_stateBeforeLevelUp);

        // ResumeWave 는 웨이브가 돌고 있지 않으면 아무것도 하지 않는다 (timeScale 도 안 돌린다).
        // 웨이브 밖에서 레벨업했다면 여기서 직접 풀어 주지 않으면 게임이 얼어붙는다.
        if (_stateBeforeLevelUp == GameState.Wave) WaveManager.ResumeWave();
        else                                       Time.timeScale = 1f;
    }
}
