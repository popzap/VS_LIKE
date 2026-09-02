using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Profiling;
using UnityEngine;

/// <summary>
/// 🔬 <b>임시 측정 하네스 (D27).</b> 적을 N마리 세워 두고 프레임 타임을 잰다.
///
/// <para>측정이 끝나면 <b>이 파일과 씬 오브젝트를 지운다</b> (<c>CLAUDE.md</c> §4).
/// 프로토콜·판정 기준은 <c>Docs/PERF.md</c> 에 있다.</para>
///
/// <para><b>시나리오 A</b>(적만)는 웨이브를 쓰지 않는다: <c>WaveManager</c> 는
/// <c>MaxAlive</c>(60~130)로 상한을 걸고 시간창에 맞춰 나눠 소환하므로
/// "적 800마리"라는 조건 자체를 만들 수 없고, 소환 코루틴 비용이 측정에 섞인다.</para>
///
/// <para><b>시나리오 B</b>(적+무기)는 <b>어쩔 수 없이</b> 웨이브를 켠다 — 부하 때문이 아니라
/// <b>B5</b> 때문이다. 무기를 켜면 적이 죽는데, 웨이브 밖에서 죽으면 NRE 가 난다.
/// 그래서 웨이브가 자기 적을 얹는 만큼 <b>적 수가 목표치보다 많아진다</b> —
/// 로그의 `씬 전체 적` 이 실제 조건이고, `하네스 적` 은 내가 세운 수다.</para>
/// </summary>
[DisallowMultipleComponent]
public class PerfHarness : MonoBehaviour
{
    /// <summary>
    /// 🔴 이 파일을 고칠 때마다 **올린다.** 측정 전에 이 값을 조회해
    /// "내가 방금 고친 코드가 실제로 로드됐는가"를 확인한다.
    ///
    /// <para>콘솔 에러 0 은 근거가 못 된다 — 컴파일이 아직 안 돌았을 수도 있고,
    /// 실패하면 <b>옛 어셈블리가 그대로 남아</b> 타입 조회도 성공한다.
    /// 실제로 이번 세션에서 컴파일 에러가 난 채로 측정을 한 번 돌렸다.</para>
    /// </summary>
    public const int Version = 11;

    [Header("적 구성")]
    [Tooltip("실제 웨이브와 같은 6종을 넣는다. 순서대로 돌아가며 소환된다")]
    [SerializeField] private EnemyData[] enemyTypes;
    [Tooltip("측정할 적 수 구간")]
    [SerializeField] private int[] counts = { 200, 400, 800 };
    [Tooltip("구간당 반복 횟수")]
    [SerializeField] private int trials = 3;

    [Header("배치")]
    [Tooltip("플레이어 기준 소환 반경. 링이 아니라 꽉 찬 원 — 난전 중반 분포를 흉내낸다")]
    [SerializeField] private float spawnRadius = 12f;
    [Tooltip("난수 시드. 시행 간 배치를 똑같이 만든다")]
    [SerializeField] private int seed = 12345;

    [Header("표본")]
    [Tooltip("버리는 프레임 수. 풀 워밍업·셰이더 컴파일·적이 자리를 잡는 시간")]
    [SerializeField] private int warmupFrames = 300;
    [Tooltip("실제로 재는 프레임 수")]
    [SerializeField] private int sampleFrames = 600;
    [Tooltip("시행 사이에 전부 치우고 쉬는 프레임 수")]
    [SerializeField] private int settleFrames = 60;

    [Header("플레이어")]
    [Tooltip("켜면 플레이어가 원을 그리며 카이팅한다. 끄면 제자리(= 최대 뭉침, 최악 조건)")]
    [SerializeField] private bool kiting = true;
    [SerializeField] private float kitingRadius = 6f;
    [SerializeField] private float kitingSpeed  = 3.5f;

    [Header("시나리오 B — 무기까지 돌린다")]
    [Tooltip("켜면 실제 런을 시작하고 웨이브에 진입한 뒤 무기를 슬롯 한도까지 채운다.\n" +
             "🔴 웨이브를 켜야 하는 이유는 부하가 아니라 B5 다 — 웨이브 밖에서 적이 죽으면 NRE 가 난다")]
    [SerializeField] private bool scenarioB;
    [Tooltip("무기를 이 레벨까지 올린다")]
    [SerializeField] private int  weaponLevel = 5;

    [Header("대조군 — 렌더 비용 분리")]
    [Tooltip("켜면 적의 SpriteRenderer 를 전부 끈다. 로직은 그대로 돌고 그림만 안 그려진다.\n" +
             "🔴 이걸 켠 것과 끈 것의 차이 = 적 렌더 비용. 샘플러 이름으로 못 잡아서 대조군으로 뺀다")]
    [SerializeField] private bool disableEnemyRenderers;

    [Header("A/B — ExpDrop·WorldPickup 의 Update 단가")]
    [Tooltip("켜면 홀수 시행은 최적화 전 경로, 짝수 시행은 최적화 후 경로로 돈다.\n" +
             "🔴 한 실행 안에서 번갈아 재야 인스턴스 수가 같은 조건에서 단가만 비교된다 —\n" +
             "다른 실행으로 비교했더니 BEFORE 966개 vs AFTER 306개로 조건이 달라져 무효였다")]
    [SerializeField] private bool abDropPath;

    [Header("실행")]
    [SerializeField] private bool  autoRun = true;
    [Tooltip("결과 로그에 붙는 꼬리표. 예: A-before")]
    [SerializeField] private string label = "A-before";

    private ObjectPool       _pool;
    private Transform        _player;
    private Rigidbody2D      _playerRb;
    private PlayerStats      _playerStats;
    private PlayerController _playerCtrl;

    private readonly List<GameObject> _spawned = new();
    private float _kiteAngle;
    private bool  _driveKiting;
    private StageNode _waveNode;
    private int   _b9Recoveries;   // 🔬 B9 강제 복구 횟수 (임시)
    private int   _vSyncBefore = -1;
    private int   _fpsBefore   = -1;

    // ── 수집기 ──────────────────────────────────────────────────
    private ProfilerRecorder _gcAlloc, _setPass, _drawCalls, _batches;

    /// <summary>
    /// 프레임을 구간으로 쪼갠다. 🔴 <b>이게 없으면 "느리다"까지만 알고 "어디가"를 모른다.</b>
    /// 이름은 Unity 내장 샘플러다 — 없는 이름은 <c>isValid=false</c> 로 걸러진다.
    /// </summary>
    private static readonly string[] SectionNames =
    {
        "Physics2D.Simulate",        // 2D 물리 시뮬레이션 (Rigidbody2D 수백 개)
        "FixedBehaviourUpdate",      // 모든 MonoBehaviour.FixedUpdate — 여기에 EnemyBase 가 산다
        "BehaviourUpdate",           // 모든 MonoBehaviour.Update — 무기 쿨다운·투사체
        "LateBehaviourUpdate",       // 모든 LateUpdate — EnemyVisual 이 여기 산다
        // 🔴 URP 는 "Camera.Render" 로 안 잡힌다 (1차 측정에서 0.003 ms 로 나왔다).
        //    SRP 의 렌더 루프는 이 이름이다.
        "RenderPipelineManager.DoRenderLoop_Internal",
        "Camera.Render",             // 대조용으로 남긴다 — 0 이면 위 이름이 맞다는 증거
        "Gfx.WaitForPresentOnGfxThread", // GPU 대기 (여기가 크면 GPU 병목)
        "GC.Collect",                // GC 정지
        "EditorLoop",                // 에디터 자체 비용 (빌드에는 없다)
        "PlayerLoop",                // 전체 (합이 맞는지 대조)
    };
    private UnityEngine.Profiling.Recorder[] _sections;
    private double[] _sectionMsSum;

    private void Start()
    {
        if (autoRun) StartCoroutine(RunAll());
    }

    private void OnDisable()
    {
        EnemyBase.PerfProbeOn = false;
        DespawnAll();
        RestorePlayer();
        DisposeRecorders();
    }

    // ── 본체 ────────────────────────────────────────────────────

    private IEnumerator RunAll()
    {
        if (!Prepare()) yield break;

        if (scenarioB) yield return SetupScenarioB();

        Debug.Log($"[PERF] ===== START label={label} kiting={kiting} scenarioB={scenarioB} " +
                  $"warmup={warmupFrames} sample={sampleFrames} seed={seed} " +
                  $"bufSize={EnemyBase.NeighborBufSize} =====");
        LogEnvironment();

        foreach (int n in counts)
        {
            for (int t = 1; t <= trials; t++)
            {
                yield return RunOne(n, t);

                DespawnAll();
                for (int i = 0; i < settleFrames; i++) yield return null;
            }
        }

        Debug.Log($"[PERF] ===== DONE label={label} =====");
        RestorePlayer();
    }

    private IEnumerator RunOne(int enemyCount, int trial)
    {
        // 시행마다 같은 배치가 나오도록 시드를 다시 심는다.
        Random.InitState(seed + trial * 7919);
        Spawn(enemyCount);

        _driveKiting = kiting;

        // ── 워밍업 (버린다) ─────────────────────────────────
        for (int i = 0; i < warmupFrames; i++)
        {
            KeepAlive();
            DismissLevelUpIfOpen();
            EnsureWaveRunning();
            yield return null;
        }

        // ── 표본 ────────────────────────────────────────────
        var frameMs = new float[sampleFrames];
        long gcSum = 0, setPassSum = 0, drawSum = 0, batchSum = 0;
        int  counterFrames = 0;

        int sceneEnemiesBefore = CountAllEnemiesInScene();
        string updatersBefore = CountUpdaters();

        for (int i = 0; i < _sectionMsSum.Length; i++) _sectionMsSum[i] = 0.0;

        EnemyBase.PerfReset();
        EnemyBase.PerfProbeOn = true;
        float wallStart = Time.realtimeSinceStartup;

        // 🔬 D27 — 프레임마다 "무슨 일이 몇 번 일어났는지". 스파이크는 평균에 안 잡힌다.
        var fPoolGets  = new int[sampleFrames];
        var fPoolMakes = new int[sampleFrames];
        var fDeaths    = new int[sampleFrames];
        var fPopups    = new int[sampleFrames];
        long wqCount = 0, wqHits = 0, wqTicks = 0, wsTicks = 0;
        // 🔬 A/B — 홀수 시행은 최적화 전 경로, 짝수 시행은 최적화 후 경로.
        //    같은 실행 안에서 번갈아 재야 인스턴스 수가 같은 조건에서 단가만 비교된다.
        if (abDropPath) PerfCounters.SlowPath = (trial % 2 == 1);
        PerfCounters.ResetDropTrial();

        PerfCounters.ResetFrame();
        PerfCounters.On = true;

        // 🔴 표본에서 "게임이 멈춰 있던 프레임"을 뺀다.
        //    레벨업 패널이 뜨면 Time.timeScale 이 0 이 되고 FixedUpdate 가 통째로 멈춘다.
        //    그 프레임까지 세면 "적이 400마리인데 분리 질의가 0회"인 표가 나온다 (실제로 나왔다).
        int levelUps = 0, skipped = 0, waveRestarts = 0, guard = sampleFrames * 20;
        for (int i = 0; i < sampleFrames && guard-- > 0; )
        {
            KeepAlive();
            if (DismissLevelUpIfOpen()) levelUps++;
            if (EnsureWaveRunning()) waveRestarts++;
            yield return null;

            if (!IsGameRunning()) { PerfCounters.ResetFrame(); skipped++; continue; }

            frameMs[i]    = Time.unscaledDeltaTime * 1000f;
            fPoolGets[i]  = PerfCounters.PoolGets;
            fPoolMakes[i] = PerfCounters.PoolCreates;
            fDeaths[i]    = PerfCounters.Deaths;
            fPopups[i]    = PerfCounters.Popups;

            wqCount += PerfCounters.WeaponQueryCount;
            wqHits  += PerfCounters.WeaponQueryHits;
            wqTicks += PerfCounters.WeaponQueryTicks;
            wsTicks += PerfCounters.WeaponScanTicks;

            PerfCounters.ResetFrame();

            if (_gcAlloc.Valid)   { gcSum      += _gcAlloc.LastValue;   counterFrames++; }
            if (_setPass.Valid)     setPassSum += _setPass.LastValue;
            if (_drawCalls.Valid)   drawSum    += _drawCalls.LastValue;
            if (_batches.Valid)     batchSum   += _batches.LastValue;

            for (int k = 0; k < _sections.Length; k++)
                if (_sections[k] != null && _sections[k].isValid)
                    _sectionMsSum[k] += _sections[k].elapsedNanoseconds / 1e6;
            i++;
        }

        float wallSec = Time.realtimeSinceStartup - wallStart;
        EnemyBase.PerfProbeOn = false;
        PerfCounters.On = false;
        int sceneEnemiesAfter = CountAllEnemiesInScene();
        Debug.Log($"[PERF-UPDATERS] n={enemyCount} trial={trial}\n" +
                  $"  before | {updatersBefore}\n" +
                  $"  after  | {CountUpdaters()}");
        Debug.Log($"[PERF-SPACING] n={enemyCount} trial={trial}  bufSize={EnemyBase.NeighborBufSize}\n" +
                  $"  {NearestNeighborStats()}");

        if (abDropPath)
        {
            double tickUs = 1_000_000.0 / System.Diagnostics.Stopwatch.Frequency;
            long calls = PerfCounters.DropUpdateCalls;
            double us  = calls > 0 ? PerfCounters.DropUpdateTicks * tickUs / calls : 0;
            Debug.Log($"[PERF-DROP-AB] n={enemyCount} trial={trial}  " +
                      $"경로={(PerfCounters.SlowPath ? "최적화 전(GetComponent+sqrt)" : "최적화 후(캐시+sqrMagnitude)")}\n" +
                      $"  Update 호출={calls}  단가={us:F3} µs/call  " +
                      $"합={PerfCounters.DropUpdateTicks * tickUs / 1000.0:F1} ms");
        }

        ReportSpikes(enemyCount, trial, frameMs, fPoolGets, fPoolMakes, fDeaths, fPopups);
        ReportWeaponQueries(enemyCount, trial, sampleFrames, wallSec, wqCount, wqHits, wqTicks, wsTicks);

        Report(enemyCount, trial, frameMs, wallSec,
               gcSum, setPassSum, drawSum, batchSum, counterFrames,
               sceneEnemiesBefore, sceneEnemiesAfter, levelUps, skipped, waveRestarts, guard > 0);
    }

    /// <summary>
    /// 실제 런 → 웨이브 진입 → 무기 지급. <b>실제 게임 경로만 쓴다</b>
    /// (<c>StartRun</c> · <c>OnStageNodeSelected</c> · <c>ApplyItemFromShop</c>).
    ///
    /// <para>🔴 웨이브를 켜는 이유는 적을 더 뽑으려는 게 아니라 <b>B5</b> 때문이다 —
    /// <c>WaveManager._currentWaveData</c> 가 null 인 상태에서 적이 죽으면 NRE 가 나고
    /// 광역기가 첫 사망자에서 통째로 멈춘다. 무기를 켜면 적이 죽으므로 피할 수 없다.</para>
    /// </summary>
    private IEnumerator SetupScenarioB()
    {
        var gm = GameManager.Instance;
        if (gm == null) { Debug.LogError("[PERF] GameManager 가 없다."); yield break; }

        gm.StartRun();
        yield return null;

        var map = gm.StageMap;
        StageNode pick = null;
        if (map != null && map.Layers != null && map.Layers.Count > 0)
        {
            foreach (var n in map.Layers[0])
                if (n.StageType == StageType.Normal) { pick = n; break; }
            if (pick == null && map.Layers[0].Count > 0) pick = map.Layers[0][0];
        }
        if (pick == null) { Debug.LogError("[PERF] 진입할 스테이지 노드를 못 찾았다."); yield break; }

        _waveNode = pick;
        gm.OnStageNodeSelected(pick);
        yield return null;

        // 무기를 슬롯 한도까지 채운다. ApplyItem 이 CanAcquire 로 초과를 막으므로
        // 여기서 한도를 따로 계산하지 않는다(= 게임 규칙 그대로).
        var lum = gm.LevelUpManager;
        int granted = 0;
        if (lum != null && lum.AllItems != null)
        {
            foreach (var item in lum.AllItems)
            {
                if (item == null || item.Category != ItemCategory.Weapon) continue;
                for (int lv = 0; lv < weaponLevel; lv++) lum.ApplyItemFromShop(item);
                granted++;
            }
        }

        // StartRun 이 조종권을 돌려줬을 수 있다. 다시 가져온다.
        if (_playerCtrl != null) _playerCtrl.SetInputEnabled(false);

        int wm = WeaponManager.Instance != null ? WeaponManager.Instance.GetEquippedWeapons().Count : -1;
        Debug.Log($"[PERF] 시나리오 B 준비: state={gm.CurrentState} node={pick.StageType} " +
                  $"무기시도={granted}종 x Lv{weaponLevel} → 실제 장착={wm}");
        yield return null;
    }

    /// <summary>
    /// 레벨업 패널이 떠 있으면 <b>아이템을 고르지 않고</b> 닫는다.
    ///
    /// <para>🔴 이게 없으면 측정이 통째로 거짓말이 된다. 레벨업은 <c>Time.timeScale = 0</c> 이라
    /// <c>FixedUpdate</c> 가 멈추고, 그동안의 프레임까지 세면
    /// <b>"적 200마리인데 분리 질의 0회"</b> 같은 표가 나온다 (첫 시나리오 B 에서 실제로 나왔다).</para>
    ///
    /// <para>아이템을 <b>안 고르는</b> 이유: 고르면 표본 도중에 무기 구성이 바뀌어
    /// 조건이 흔들린다. <c>HidePanel()</c> 은 선택 없이 닫고 상태를 되돌리는 공개 API 다.</para>
    /// </summary>
    private bool DismissLevelUpIfOpen()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.CurrentState != GameState.LevelUp) return false;

        var lum = gm.LevelUpManager;
        if (lum == null) return false;

        lum.HidePanel();
        return true;
    }

    /// <summary>게임이 실제로 굴러가는 프레임인가. 멈춰 있으면 표본에서 뺀다.</summary>
    private static bool IsGameRunning()
    {
        if (Time.timeScale <= 0f) return false;
        var gm = GameManager.Instance;
        return gm == null || gm.CurrentState == GameState.Wave || gm.CurrentState == GameState.MainMenu;
    }

    /// <summary>
    /// 웨이브가 시간 만료로 끝났으면 다시 시작한다.
    ///
    /// <para>🔴 없으면 측정이 조용히 죽는다. <c>Normal1</c> 의 <c>SurvivalTime</c> 은 60초인데
    /// 전체 매트릭스는 그보다 오래 걸린다. 웨이브가 끝나면 상태가 <c>Wave</c> 를 벗어나
    /// 모든 프레임이 표본에서 빠지고 <b>0으로 가득 찬 행</b>이 나온다 (실제로 나왔다 — B 800 시행 2).</para>
    /// </summary>
    private bool EnsureWaveRunning()
    {
        if (!scenarioB) return false;

        var gm = GameManager.Instance;
        if (gm == null || _waveNode == null) return false;

        // 🔴 state 가 Wave 인데 timeScale 이 0 인 상태가 있다.
        //    B9 로 등재했다가 오진으로 철회했다 — 버그가 아니라 StageClearUI 가
        //    Continue 를 기다리며 정상적으로 멈춘 것이다 (state 는 Wave 그대로 둔다).
        //    사람은 버튼을 누르면 되지만 하네스는 누를 수가 없어 5분을 날렸다.
        //    ⇒ 결과창을 치우고 웨이브를 다시 시작한다.
        if (gm.CurrentState == GameState.Wave && Time.timeScale <= 0f)
        {
            var clearUI = StageClearUI.Instance;
            if (clearUI != null) clearUI.gameObject.SetActive(false);

            gm.WaveManager.StartWave(_waveNode);
            Time.timeScale = 1f;
            _b9Recoveries++;
            return true;
        }

        if (gm.CurrentState == GameState.Wave || gm.CurrentState == GameState.LevelUp) return false;

        gm.WaveManager.StartWave(_waveNode);
        gm.ChangeState(GameState.Wave);
        Time.timeScale = 1f;
        return true;
    }

    /// <summary>
    /// 🔬 D27 — 살아있는 적들의 <b>평균 최근접 거리</b>. `B8` 수정의 성공 조건 2번이다.
    ///
    /// <para>무리 분리가 실제로 세졌는지는 "덜 겹쳐 보인다"가 아니라 <b>이 숫자</b>로 판정한다.
    /// 분리가 제대로 돌면 적들이 서로 더 떨어져 있으므로 값이 <b>커진다.</b></para>
    ///
    /// <para>O(n²) 이라 적 800이면 64만 번이다. 🔴 <b>표본 구간 밖에서 한 번만</b> 부른다.</para>
    /// </summary>
    private static string NearestNeighborStats()
    {
        var pos = new List<Vector2>(1024);
        foreach (var e in FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
            if (e.gameObject.activeInHierarchy) pos.Add(e.transform.position);

        int n = pos.Count;
        if (n < 2) return $"적={n} (측정 불가)";

        double sum = 0;
        float  min = float.MaxValue;
        int    under05 = 0;          // 0.5 유닛 안에 붙어 있는 개체 수 = 뭉침의 직접 지표

        for (int i = 0; i < n; i++)
        {
            float bestSqr = float.MaxValue;
            for (int j = 0; j < n; j++)
            {
                if (i == j) continue;
                float d = (pos[i] - pos[j]).sqrMagnitude;
                if (d < bestSqr) bestSqr = d;
            }
            float best = Mathf.Sqrt(bestSqr);
            sum += best;
            if (best < min) min = best;
            if (best < 0.5f) under05++;
        }

        return $"적={n}  평균최근접={sum / n:F4}  최소={min:F4}  " +
               $"0.5유닛내={under05} ({100.0 * under05 / n:F1} %)";
    }

    /// <summary>
    /// 🔬 D27 — <c>BehaviourUpdate</c> 에 사는 클래스들이 <b>각각 몇 개나 살아 있는지</b> 센다.
    ///
    /// <para><c>BehaviourUpdate</c> 가 A 대비 16~65배로 뛰었는데(§7-H 결과 ④)
    /// 무기 질의는 프레임의 1.3 % 뿐이었다(결과 ⑤). 남은 건 <b>인스턴스 수</b>다 —
    /// 하나가 싼 <c>Update</c> 라도 수천 개면 합이 커진다.</para>
    ///
    /// <para>🔴 <b>표본 구간에는 부르지 않는다.</b> <c>FindObjectsByType</c> 는 비싸다.</para>
    /// </summary>
    private static string CountUpdaters()
    {
        int Count<T>() where T : MonoBehaviour
        {
            int c = 0;
            foreach (var o in FindObjectsByType<T>(FindObjectsSortMode.None))
                if (o.isActiveAndEnabled) c++;
            return c;
        }

        return $"Projectile={Count<ProjectileBase>()}  EnemyProjectile={Count<EnemyProjectile>()}  " +
               $"ExpDrop={Count<ExpDrop>()}  DamagePopup={Count<DamagePopup>()}  " +
               $"Pickup={Count<WorldPickup>()}  ToxinField={Count<ToxinField>()}  " +
               $"SummonVisual={Count<SummonVisual>()}  Weapon={Count<WeaponBase>()}";
    }

    /// <summary>
    /// 씬 전체의 살아있는 적 수. 🔴 <b>표본 구간에는 절대 부르지 않는다</b> —
    /// <c>FindObjectsByType</c> 는 비싸서 그 자체가 측정을 오염시킨다.
    /// </summary>
    private static int CountAllEnemiesInScene()
    {
        int c = 0;
        foreach (var e in FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
            if (e.gameObject.activeInHierarchy) c++;
        return c;
    }

    // ── 준비 / 정리 ─────────────────────────────────────────────

    private bool Prepare()
    {
        _pool = FindFirstObjectByType<ObjectPool>();
        if (_pool == null) { Debug.LogError("[PERF] ObjectPool 을 못 찾았다."); return false; }

        var pgo = GameObject.FindGameObjectWithTag("Player");
        if (pgo == null) { Debug.LogError("[PERF] Player 태그 오브젝트가 없다."); return false; }

        _player      = pgo.transform;
        _playerRb    = pgo.GetComponent<Rigidbody2D>();
        _playerStats = pgo.GetComponent<PlayerStats>();
        _playerCtrl  = pgo.GetComponent<PlayerController>();

        if (enemyTypes == null || enemyTypes.Length == 0)
        {
            Debug.LogError("[PERF] enemyTypes 가 비었다. 인스펙터에 EnemyData 를 넣을 것.");
            return false;
        }
        for (int i = 0; i < enemyTypes.Length; i++)
        {
            if (enemyTypes[i] == null || enemyTypes[i].Prefab == null)
            {
                Debug.LogError($"[PERF] enemyTypes[{i}] 가 null 이거나 Prefab 이 없다.");
                return false;
            }
        }

        // 사람이 안 누르는 동안 플레이어가 멋대로 서 있지 않게 조종권을 가져온다.
        if (_playerCtrl != null) _playerCtrl.SetInputEnabled(false);

        // 🔴 vSync 를 반드시 끈다. 켜져 있으면 프레임 타임이 모니터 주사율(144 Hz = 6.94 ms)에
        //    붙어 버려 병목이 통째로 안 보인다. Quality 레벨을 Ultra 로 올리면 vSync 가
        //    같이 켜지므로(레벨마다 설정이 다르다) 여기서 매번 다시 끈다.
        _vSyncBefore = QualitySettings.vSyncCount;
        _fpsBefore   = Application.targetFrameRate;
        QualitySettings.vSyncCount  = 0;
        Application.targetFrameRate = -1;

        _sections     = new UnityEngine.Profiling.Recorder[SectionNames.Length];
        _sectionMsSum = new double[SectionNames.Length];
        for (int i = 0; i < SectionNames.Length; i++)
        {
            _sections[i] = UnityEngine.Profiling.Recorder.Get(SectionNames[i]);
            if (_sections[i] != null) _sections[i].enabled = true;
        }

        _gcAlloc   = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
        _setPass   = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
        _drawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
        _batches   = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
        return true;
    }

    private void RestorePlayer()
    {
        _driveKiting = false;
        if (_playerCtrl != null) _playerCtrl.SetInputEnabled(true);
        if (_vSyncBefore >= 0) QualitySettings.vSyncCount  = _vSyncBefore;
        if (_fpsBefore  != -1) Application.targetFrameRate = _fpsBefore;
    }

    private void DisposeRecorders()
    {
        if (_gcAlloc.Valid)   _gcAlloc.Dispose();
        if (_setPass.Valid)   _setPass.Dispose();
        if (_drawCalls.Valid) _drawCalls.Dispose();
        if (_batches.Valid)   _batches.Dispose();
    }

    private void Spawn(int n)
    {
        for (int i = 0; i < n; i++)
        {
            var data = enemyTypes[i % enemyTypes.Length];
            Vector2 pos = (Vector2)_player.position + Random.insideUnitCircle * spawnRadius;

            var go = _pool.Get(data.Prefab, pos, Quaternion.identity);
            go.GetComponent<EnemyBase>().Initialize(data);
            ApplyRendererFlag(go);
            _spawned.Add(go);
        }
    }

    /// <summary>
    /// 표본 구간 동안 적 수가 변하지 않게 한다 — 변하면 조건이 흔들려 비교가 무효다.
    /// 플레이어 무적도 여기서 갱신한다(접촉 피해로 죽으면 측정이 끊긴다).
    /// </summary>
    private void KeepAlive()
    {
        if (_playerStats != null) _playerStats.GrantInvincibility(1f);

        for (int i = 0; i < _spawned.Count; i++)
        {
            var go = _spawned[i];
            if (go != null && !go.activeSelf)
            {
                // 풀로 돌아간 개체가 있으면 같은 자리에서 되살린다.
                var data = enemyTypes[i % enemyTypes.Length];
                Vector2 pos = (Vector2)_player.position + Random.insideUnitCircle * spawnRadius;
                var revived = _pool.Get(data.Prefab, pos, Quaternion.identity);
                revived.GetComponent<EnemyBase>().Initialize(data);
                ApplyRendererFlag(revived);
                _spawned[i] = revived;
            }
        }
    }

    /// <summary>
    /// 🔴 <b>항상 양쪽 값을 명시적으로 쓴다.</b> 끄기만 하면 그 개체가 풀로 돌아간 뒤
    /// 다음 측정(대조군 off)에서 <b>꺼진 채로 되살아나</b> 두 조건이 섞인다.
    /// </summary>
    private void ApplyRendererFlag(GameObject go)
    {
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = !disableEnemyRenderers;
    }

    private void DespawnAll()
    {
        foreach (var go in _spawned)
        {
            if (go == null) continue;
            var e = go.GetComponent<EnemyBase>();
            if (e != null) e.ForceDespawn(); else _pool.Return(go);
        }
        _spawned.Clear();
    }

    private void FixedUpdate()
    {
        if (!_driveKiting || _playerRb == null) return;

        // 실제 플레이처럼 원을 그리며 돈다. 제자리에 서 있으면 적이 한 점에 뭉쳐
        // 실제보다 극단적인 조건이 된다.
        _kiteAngle += kitingSpeed / Mathf.Max(0.1f, kitingRadius) * Time.fixedDeltaTime;
        Vector2 tangent = new Vector2(-Mathf.Sin(_kiteAngle), Mathf.Cos(_kiteAngle));
        _playerRb.linearVelocity = tangent * kitingSpeed;
    }

    // ── 보고 ────────────────────────────────────────────────────

    private void Report(int enemyCount, int trial, float[] frameMs, float wallSec,
                        long gcSum, long setPassSum, long drawSum, long batchSum, int counterFrames,
                        int sceneBefore, int sceneAfter, int levelUps, int skipped,
                        int waveRestarts, bool complete)
    {
        var sorted = (float[])frameMs.Clone();
        System.Array.Sort(sorted);

        float p50 = Pct(sorted, 0.50f);
        float p95 = Pct(sorted, 0.95f);
        float p99 = Pct(sorted, 0.99f);
        float max = sorted[sorted.Length - 1];
        float mean = 0f; foreach (var v in frameMs) mean += v; mean /= frameMs.Length;

        int f = Mathf.Max(1, frameMs.Length);
        double tickMs = 1000.0 / System.Diagnostics.Stopwatch.Frequency;

        long q = EnemyBase.PerfQueryCount;
        double queryMsTotal = EnemyBase.PerfQueryTicks * tickMs;
        double totalMsTotal = EnemyBase.PerfTotalTicks * tickMs;
        double nAvg = q > 0 ? (double)EnemyBase.PerfNeighborSum / q : 0.0;

        int satCount = EnemyBase.PerfNHist[EnemyBase.NeighborBufSize];
        double satPct = q > 0 ? 100.0 * satCount / q : 0.0;

        var hist = new StringBuilder();
        for (int i = 0; i < EnemyBase.PerfNHist.Length; i++)
        {
            if (i > 0) hist.Append(' ');
            hist.Append(i).Append(':').Append(EnemyBase.PerfNHist[i]);
        }

        Debug.Log(
            $"[PERF] {label} n={enemyCount} trial={trial}\n" +
            $"  frames={frameMs.Length} wall={wallSec:F2}s  하네스 적={CountAlive()}  " +
            $"씬 전체 적={sceneBefore}->{sceneAfter}  레벨업={levelUps}  버린프레임={skipped}  " +
            $"웨이브재시작={waveRestarts}  B9복구={_b9Recoveries}" +
            $"{(complete ? "" : "  🔴 INVALID(표본 못 채움)")}\n" +
            $"  frame ms | p50={p50:F3} p95={p95:F3} p99={p99:F3} max={max:F3} (mean={mean:F3})\n" +
            $"  분리질의 | count={q} ({q / Mathf.Max(0.001f, wallSec):F0}/s)  " +
            $"query={queryMsTotal / f:F4} ms/frame  total={totalMsTotal / f:F4} ms/frame  " +
            $"(= 프레임의 {(p50 > 0 ? totalMsTotal / f / p50 * 100.0 : 0):F1} %)\n" +
            $"  이웃 n   | avg={nAvg:F2}  포화(n={EnemyBase.NeighborBufSize})={satCount} ({satPct:F2} %)\n" +
            $"  n 히스토그램 | {hist}\n" +
            $"  구간 ms/frame | {SectionLine(f)}\n" +
            $"  GC/frame={(counterFrames > 0 ? gcSum / (double)counterFrames : -1):F0} B  " +
            $"SetPass/frame={setPassSum / (double)f:F1}  " +
            $"DrawCalls/frame={drawSum / (double)f:F1}  Batches/frame={batchSum / (double)f:F1}");
    }

    private int CountAlive()
    {
        int c = 0;
        foreach (var go in _spawned) if (go != null && go.activeSelf) c++;
        return c;
    }

    /// <summary>
    /// 🔬 D27 — <b>가장 느린 프레임 10개</b>에 무엇이 몰려 있었는지 본다.
    ///
    /// <para>구간 프로파일링은 전 프레임 평균이라 250 ms 짜리 몇 개가 묻힌다.
    /// 스파이크의 원인은 "평균적으로 무엇이 비싼가"가 아니라
    /// <b>"느린 그 프레임에 무엇이 있었나"</b>로만 잡힌다.</para>
    ///
    /// <para>판정 기준은 <c>Docs/PERF.md</c> §7-H 에 <b>측정 전에</b> 적었다 —
    /// 상위 10개의 평균이 나머지의 <b>5배 이상</b>인 항목이 범인이다.</para>
    /// </summary>
    private static void ReportSpikes(int enemyCount, int trial, float[] frameMs,
                                     int[] gets, int[] makes, int[] deaths, int[] popups)
    {
        int n = frameMs.Length;
        if (n < 20) return;

        // 느린 순으로 인덱스를 고른다 (부분 선택 — 10개뿐이라 단순 반복으로 충분하다)
        const int TopK = 10;
        var top = new int[TopK];
        var used = new bool[n];
        for (int k = 0; k < TopK; k++)
        {
            int best = -1;
            for (int i = 0; i < n; i++)
                if (!used[i] && (best < 0 || frameMs[i] > frameMs[best])) best = i;
            used[best] = true;
            top[k] = best;
        }

        double msTop = 0, getTop = 0, makeTop = 0, deathTop = 0, popTop = 0;
        foreach (int i in top)
        {
            msTop += frameMs[i]; getTop += gets[i]; makeTop += makes[i];
            deathTop += deaths[i]; popTop += popups[i];
        }

        double msRest = 0, getRest = 0, makeRest = 0, deathRest = 0, popRest = 0;
        int restCount = 0;
        for (int i = 0; i < n; i++)
        {
            if (used[i]) continue;
            msRest += frameMs[i]; getRest += gets[i]; makeRest += makes[i];
            deathRest += deaths[i]; popRest += popups[i];
            restCount++;
        }

        float R(double a, double b) => b > 0.0001 ? (float)(a / b) : -1f;

        var sb = new StringBuilder();
        sb.Append($"[PERF-SPIKE] n={enemyCount} trial={trial}\n");
        sb.Append($"  느린 10프레임 평균 | ms={msTop / TopK:F1}  풀Get={getTop / TopK:F1}  " +
                  $"🔴 풀Create={makeTop / TopK:F1}  사망={deathTop / TopK:F1}  팝업={popTop / TopK:F1}\n");
        sb.Append($"  나머지 {restCount}프레임 평균 | ms={msRest / restCount:F1}  풀Get={getRest / restCount:F1}  " +
                  $"풀Create={makeRest / restCount:F1}  사망={deathRest / restCount:F1}  팝업={popRest / restCount:F1}\n");
        sb.Append($"  배수(느린/나머지) | ms={R(msTop / TopK, msRest / restCount):F1}x  " +
                  $"풀Get={R(getTop / TopK, getRest / restCount):F1}x  " +
                  $"🔴 풀Create={R(makeTop / TopK, makeRest / restCount):F1}x  " +
                  $"사망={R(deathTop / TopK, deathRest / restCount):F1}x  " +
                  $"팝업={R(popTop / TopK, popRest / restCount):F1}x   (5배 이상 = 범인)\n");

        sb.Append("  느린 프레임 개별 | ");
        foreach (int i in top)
            sb.Append($"[{frameMs[i]:F0}ms get={gets[i]} make={makes[i]} die={deaths[i]} pop={popups[i]}] ");

        long totMake = 0, totDeath = 0, totPop = 0, totGet = 0;
        for (int i = 0; i < n; i++) { totGet += gets[i]; totMake += makes[i]; totDeath += deaths[i]; totPop += popups[i]; }
        sb.Append($"\n  표본 합계 | 풀Get={totGet}  풀Create={totMake}  사망={totDeath}  팝업={totPop}");

        Debug.Log(sb.ToString());
    }

    /// <summary>
    /// 🔬 D27 — 무기 질의(<c>OverlapCircleAll</c>)와 그 뒤 선형 탐색의 비용.
    ///
    /// <para><c>BehaviourUpdate</c> 가 A 대비 16~65배로 뛴 것이 확인됐고(§7-H 결과 ④),
    /// <c>Update()</c> 안에서 무기가 하는 일은 <b>이 질의</b>와 <b>투사체 이동</b> 둘뿐이다.
    /// 여기 나오는 ms 가 <c>BehaviourUpdate</c> 의 대부분이면 질의가 범인이고,
    /// 작으면 투사체 쪽이다.</para>
    /// </summary>
    private static void ReportWeaponQueries(int enemyCount, int trial, int frames, float wallSec,
                                            long count, long hits, long queryTicks, long scanTicks)
    {
        double tickMs = 1000.0 / System.Diagnostics.Stopwatch.Frequency;
        double qMs = queryTicks * tickMs;
        double sMs = scanTicks  * tickMs;

        Debug.Log(
            $"[PERF-WEAPON] n={enemyCount} trial={trial}\n" +
            $"  질의 | count={count} ({count / Mathf.Max(0.001f, wallSec):F0}/s)  " +
            $"반환 콜라이더 합={hits}  1회 평균={(count > 0 ? hits / (double)count : 0):F1}개\n" +
            $"  질의 시간      | {qMs / frames:F4} ms/frame  (합 {qMs:F1} ms)\n" +
            $"  FindNearest 전체 | {sMs / frames:F4} ms/frame  (합 {sMs:F1} ms)\n" +
            $"  ↳ 선형 탐색(sqrt) 몫 | {(sMs - qMs) / frames:F4} ms/frame");
    }

    private string SectionLine(int frames)
    {
        var sb = new StringBuilder();
        for (int k = 0; k < SectionNames.Length; k++)
        {
            if (k > 0) sb.Append("  ");
            bool ok = _sections != null && _sections[k] != null && _sections[k].isValid;
            sb.Append(SectionNames[k]).Append('=');
            if (ok) sb.Append((_sectionMsSum[k] / frames).ToString("F3"));
            else    sb.Append("n/a");
        }
        return sb.ToString();
    }

    private static float Pct(float[] sorted, float p)
    {
        int idx = Mathf.Clamp(Mathf.RoundToInt(p * (sorted.Length - 1)), 0, sorted.Length - 1);
        return sorted[idx];
    }

    private void LogEnvironment()
    {
        Debug.Log(
            $"[PERF] ENV | quality={QualitySettings.names[QualitySettings.GetQualityLevel()]}" +
            $"({QualitySettings.GetQualityLevel()}) vSync={QualitySettings.vSyncCount} " +
            $"targetFps={Application.targetFrameRate} screen={Screen.width}x{Screen.height} " +
            $"fixedDt={Time.fixedDeltaTime} timeScale={Time.timeScale}\n" +
            $"[PERF] REC | gcAlloc={_gcAlloc.Valid} setPass={_setPass.Valid} " +
            $"drawCalls={_drawCalls.Valid} batches={_batches.Valid}");
    }
}
