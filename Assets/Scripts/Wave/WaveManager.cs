using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// WaveData / WaveSpawnEntry 는 WaveData.cs 로 분리했다 (ScriptableObject 는 동명 파일이어야 한다).

/// <summary>
/// 스테이지 노드에 따라 웨이브를 구성·진행·클리어 판정한다.
/// </summary>
public class WaveManager : MonoBehaviour
{
    /// <summary>컴파일 반영 확인용 (D27). 새 심볼을 넣을 때마다 올린다.</summary>
    public const int Version = 3;

    [Header("참조")]
    [SerializeField] private WaveData[]  normalWaves;   // 노말 웨이브 풀
    [SerializeField] private WaveData[]  eliteWaves;
    [SerializeField] private WaveData    bossWave;

    [Tooltip("이벤트 전투용 웨이브 (D37). 비어 있으면 노말로 떨어진다")]
    [SerializeField] private WaveData[]  eventWaves;
    [SerializeField] private ObjectPool  enemyPool;
    [SerializeField] private Transform   playerTransform;

    [Tooltip("보스 내려찍기의 예고+폭발 프리팹 (D31). 비어 있으면 내려찍기만 조용히 안 나간다")]
    [SerializeField] private GameObject  bossSlamPrefab;

    // ── 층별 난이도 (ROADMAP §3 결정 4 · B안) ────────────────────
    // 🔴 수치는 Economy.csv 의 WaveManager 행이 들고 있다. 여기 기본값은 자리표시다.
    [Header("층별 난이도 — 값은 Economy.csv 가 덮는다")]
    [Tooltip("층당 적 최대체력 증가율. 0.10 = 층마다 +10 %. 0 이면 층 스케일링이 꺼진다.")]
    [SerializeField] private float layerHpGrowth = 0.10f;

    [Tooltip("층당 적 접촉 피해 증가율.")]
    [SerializeField] private float layerDamageGrowth = 0.06f;

    [Tooltip("층당 소환량 증가율. 소환 수와 동시 생존 상한에 같이 걸린다 — "
           + "상한을 같이 안 올리면 대기줄만 길어지고 화면은 그대로다.")]
    [SerializeField] private float layerSpawnGrowth = 0.08f;

    [Tooltip("동시 생존 상한의 천장. 🔴 EnemyBase 의 무리 분리 버퍼가 64칸이라 "
           + "적 400 이상에서는 그 값을 다시 판단해야 한다(B8) — 그 선 아래로 묶어 둔다.")]
    [SerializeField] private int maxAliveCeiling = 300;

    // ── 출현 패턴 (D50) ──────────────────────────────────────────
    // 🔴 수치는 Economy.csv 의 WaveManager 행이 들고 있다.
    [Header("출현 패턴 — 값은 Economy.csv 가 덮는다")]
    [Tooltip("Ring: 한 번에 원 위에 놓는 마리수. 이 수만큼 뿌리고 쉬었다가 다음 무리를 놓는다.")]
    [Min(2)] [SerializeField] private int ringBatch = 8;

    [Tooltip("Squad: 대형의 가로 칸 수. 6 이면 6열로 줄을 맞춘다.")]
    [Min(1)] [SerializeField] private int squadColumns = 4;

    [Tooltip("Squad: 대형 안에서 옆 적과의 간격(월드 유닛). 너무 좁으면 서로 밀어내며 흩어진다.")]
    [SerializeField] private float squadSpacing = 1.1f;

    [Tooltip("Burrow: 플레이어로부터 이만큼 떨어진 곳에서 솟는다(최소). "
           + "🔴 너무 가까우면 예고를 봐도 못 피한다.")]
    [SerializeField] private float burrowMinDistance = 3f;

    [Tooltip("Burrow: 솟는 거리(최대). 소환 반경보다 훨씬 안쪽이라 '발밑에서 나온다'는 느낌이 난다.")]
    [SerializeField] private float burrowMaxDistance = 6f;

    [Tooltip("Burrow: 예고가 떠 있는 시간. 🔴 0 이면 피할 수 없는 기습이 된다 — 최소 0.35 로 묶는다.")]
    [SerializeField] private float burrowWindup = 0.85f;

    [Tooltip("Burrow: 한 번에 몇 군데서 솟는가.")]
    [Min(1)] [SerializeField] private int burrowBatch = 4;

    [Tooltip("Burrow 예고 표시(Fx_Burrow). SceneWiring.csv 의 WaveManager,burrowTelegraphPrefab 로 배선한다. "
           + "🔴 비어 있으면 예고 없이 솟는다 — 그건 난이도가 아니라 사고다.")]
    [SerializeField] private GameObject burrowTelegraphPrefab;

    // ── 런타임 상태 ──────────────────────────────────────────────
    private StageNode   _currentNode;
    private WaveData    _currentWaveData;
    private bool        _waveActive;
    private bool        _wavePaused;

    private int         _killCount;
    private float       _waveElapsed;   // 웨이브 시작 후 경과 초. 소환 시간창의 기준

    // 살아 있는 적. 소환 상한 판정과 "멀리 간 적 재배치"에 둘 다 쓴다.
    //
    // ⚠️ 단순 카운터(int)로 하면 안 된다. 적이 사라지는 경로가 두 가지(처치 / ForceDespawn)라
    //    한쪽에서만 빼면 수가 어긋나고, 상한이 걸린 순간 소환이 영원히 막힌다.
    //    그래서 "실제 오브젝트가 켜져 있는가"를 매번 다시 보고 판단한다.
    private readonly List<EnemyBase> _alive = new();

    private readonly List<Coroutine> _routines = new();
    private Coroutine   _timerRoutine;

    /// <summary>
    /// 타이머 웨이브의 남은 초. <b>지역 변수가 아니라 필드다</b> (D64) —
    /// 개발용 시간 조절(<c>DevSetRemainingTime</c>)이 코루틴 밖에서 이 값을 만져야 한다.
    /// <see cref="WaveRemainingTime"/> 는 이걸 그대로 비추는 읽기 전용 창이다.
    /// </summary>
    private float       _timerRemaining;

    // ── 런 누적 통계 (상점 NPC 패널 등에서 참조) ────────────────
    /// <summary>런 전체 누적 처치 수.</summary>
    public int   TotalKillCount    { get; private set; }
    /// <summary>런 전체 누적 경과 시간 (초).</summary>
    public float TotalElapsedTime  { get; private set; }

    /// <summary>
    /// 현재 웨이브 클리어까지 남은 시간 (초). 시간 클리어가 아닌 웨이브면 <c>-1</c>.
    /// <para>HUD 가 매 프레임 폴링한다. <c>OnTimerUpdated</c> 이벤트도 있지만, 구독은
    /// <c>GameManager.Start()</c> 가 참조를 채우기 전에 일어날 수 있어(I-8·I-38 과 같은
    /// 실행 순서 경쟁) HUD 쪽은 폴링으로 간다.</para>
    /// </summary>
    public float WaveRemainingTime { get; private set; } = -1f;

    /// <summary>현재 웨이브가 진행 중인지. HUD 가 표시 여부를 정할 때 쓴다.</summary>
    public bool  IsWaveActive => _waveActive;

    /// <summary>
    /// 이 게임에 실제로 나오는 적 전부 (D54 · 도감).
    ///
    /// <para>🔑 <b>애셋 폴더를 훑지 않는다.</b> 배선된 웨이브를 걸어서 모으므로
    /// <c>Waves.csv</c> 에 안 적힌 적은 도감에도 안 뜬다 — 목록이 게임과 어긋날 수 없다.</para>
    ///
    /// <para>엘리트/보스 <c>Override</c> 도 같이 담는다. <c>Bonecaller</c> 처럼
    /// 소환 항목에는 없고 보스로만 나오는 적이 있기 때문이다.</para>
    /// </summary>
    public List<EnemyData> CollectEnemies()
    {
        var seen = new List<EnemyData>();

        void Add(EnemyData e)
        {
            if (e != null && !seen.Contains(e)) seen.Add(e);
        }

        void Walk(WaveData[] pool)
        {
            if (pool == null) return;
            foreach (var w in pool)
            {
                if (w == null) continue;
                if (w.Spawns != null)
                    foreach (var s in w.Spawns) if (s != null) Add(s.Enemy);
                Add(w.EliteOverride);
                Add(w.BossOverride);
            }
        }

        Walk(normalWaves);
        Walk(eliteWaves);
        Walk(eventWaves);
        Walk(new[] { bossWave });   // bossWave 는 배열이 아니라 한 개다
        return seen;
    }

    /// <summary>지금 필드에 살아 있는 적 수.</summary>
    public int   EnemiesAlive => _alive.Count;

    /// <summary>지금 도는 웨이브가 보스전인지. BGM 선택에 쓴다.</summary>
    public bool  IsBossWave => _currentNode != null && _currentNode.StageType == StageType.Boss;

    // ── 이벤트 ───────────────────────────────────────────────────
    public System.Action<float> OnTimerUpdated;   // 남은 시간
    public System.Action<int>   OnKillCountUpdated;

    /// <summary>
    /// 보스가 등장했다 (D31). 보스 HP 바가 구독한다.
    ///
    /// <para>HP 바가 <c>FindObjectsByType&lt;EnemyBase&gt;</c> 로 보스를 찾아다니지 않게
    /// <b>등장하는 쪽이 알려 준다.</b> 보스는 한 판에 한 번 나오므로 이벤트 하나면 충분하다.</para>
    /// </summary>
    public System.Action<EnemyBase, BossBrain> OnBossSpawned;

    // ── Public API ───────────────────────────────────────────────

    public void StartWave(StageNode node)
    {
        _currentNode = node;
        _waveActive  = true;
        _wavePaused  = false;
        _killCount   = 0;
        _waveElapsed = 0f;
        _alive.Clear();
        _routines.Clear();
        // 누적 통계는 런 내내 유지 (리셋하지 않음)

        // 스테이지 결과창용 스냅샷 (웨이브 시작 직전 스탯/재화 기록)
        StageClearUI.Instance?.TakeSnapshot();

        // 🔴 Event 분기가 없으면 이벤트 전투가 노말로 떨어진다 (D37).
        //    eventWaves 가 비어 있으면 노말로 되돌아간다 — 조용히 안 도는 것보다 낫다.
        _currentWaveData = node.StageType switch
        {
            StageType.Elite => eliteWaves[Random.Range(0, eliteWaves.Length)],
            StageType.Boss  => bossWave,
            StageType.Event => eventWaves != null && eventWaves.Length > 0
                                 ? eventWaves[Random.Range(0, eventWaves.Length)]
                                 : normalWaves[Random.Range(0, normalWaves.Length)],
            _               => normalWaves[Random.Range(0, normalWaves.Length)]
        };

        // 🔴 웨이브 데이터를 고른 "뒤" 에 층 배율을 세운다.
        //    적이 Initialize 될 때 LayerScaling 을 읽으므로 첫 소환보다 먼저여야 한다.
        LayerScaling.Set(node.Layer, layerHpGrowth, layerDamageGrowth, layerSpawnGrowth);

        // 소환 항목마다 코루틴을 따로 띄운다 = 병렬 소환.
        foreach (var entry in _currentWaveData.Spawns)
            _routines.Add(StartCoroutine(SpawnEntryRoutine(entry)));

        _routines.Add(StartCoroutine(OverrideSpawnRoutine()));
        _routines.Add(StartCoroutine(MaintainRoutine()));

        if (_currentWaveData.UseTimerClear)
        {
            WaveRemainingTime = _currentWaveData.SurvivalTime;
            _timerRoutine     = StartCoroutine(TimerRoutine());
        }
        else
        {
            WaveRemainingTime = -1f;   // 킬 클리어 웨이브 — HUD 는 타이머를 숨긴다
        }
    }

    public void ResumeWave()
    {
        if (!_waveActive) return;
        _wavePaused = false;
        Time.timeScale = 1f;
    }

    public void PauseWave()
    {
        _wavePaused = true;
        Time.timeScale = 0f;
    }

    private void Update()
    {
        if (!_waveActive || _wavePaused) return;

        // 소환 시간창의 기준. 킬 클리어 웨이브에는 타이머 코루틴이 없으므로
        // 여기서 따로 센다. (일시정지 중에는 흐르지 않는다)
        _waveElapsed += Time.deltaTime;
        StageClearUI.Instance?.Tick(Time.deltaTime);
    }

    /// <summary>적 사망 시 EnemyBase에서 호출.</summary>
    public void OnEnemyKilled(EnemyBase enemy)
    {
        // 살아 있는 수는 여기서 빼지 않는다. 사망 연출이 끝나고 오브젝트가
        // 실제로 꺼질 때 PruneAlive() 가 걸러낸다 — 경로가 하나여야 어긋나지 않는다.
        _killCount++;
        TotalKillCount++;
        OnKillCountUpdated?.Invoke(_killCount);
        StageClearUI.Instance?.AddKill();

        // 🔴 웨이브가 안 도는 중에도 적이 죽을 수 있다 (B5).
        //    _currentWaveData 는 StartWave 에서만 채워지므로 첫 웨이브 전에는 null 이고,
        //    여기서 NRE 가 나면 그게 Die() 를 통째로 걷어찬다 —
        //    사망 연출·소리·시체 정리가 전부 건너뛰어져 **시체가 살아 있는 채로 남는다.**
        //    광역기에서는 더 나쁘다: OverlapCircleAll 루프 밖으로 예외가 나가
        //    **같은 반경 안의 나머지 적이 피해를 아예 안 받는다** (D20 에서 실제로 봤다).
        // 🔴 보스를 잡으면 그 자리에서 끝난다 (D65 · 사용자 요구 14).
        //    예전에는 보스를 눕히고도 남은 잡몹 19마리를 마저 잡거나 240초를 채워야 했다 —
        //    **판의 절정이 끝난 뒤에 청소가 남는다.** 킬 목표보다 먼저 본다.
        if (enemy != null && enemy.IsBoss && IsBossWave)
        {
            Debug.Log("[WaveManager] 보스 처치 — 웨이브 즉시 클리어");
            ClearWave();
            return;
        }

        if (_currentWaveData != null && _currentWaveData.UseKillClear
            && _killCount >= ScaledKillTarget())
            ClearWave();
    }

    // ── 내부 로직 ────────────────────────────────────────────────

    /// <summary>
    /// 소환 항목 하나를 자기 시간창 안에서 소환한다. 항목마다 이 코루틴이 하나씩 돈다.
    /// </summary>
    private IEnumerator SpawnEntryRoutine(WaveSpawnEntry entry)
    {
        if (entry == null || entry.Enemy == null) yield break;

        while (_waveActive && _waveElapsed < entry.StartTime) yield return null;

        float interval = Mathf.Max(0.05f, entry.SpawnInterval);
        int   count    = LayerScaling.ScaleCount(entry.Count);

        // 출현 패턴 (D50). Scatter 가 예전 동작이고 나머지는 그 위에 얹었다.
        switch (entry.Pattern)
        {
            case SpawnPattern.Ring:   yield return RingRoutine   (entry, count, interval); break;
            case SpawnPattern.Squad:  yield return SquadRoutine  (entry, count, interval); break;
            case SpawnPattern.Burrow: yield return BurrowRoutine (entry, count, interval); break;
            default:                  yield return ScatterRoutine(entry, count, interval); break;
        }
    }

    /// <summary>시간창을 넘겼나. 넘겼으면 남은 마리수는 버린다 — 그게 난이도 곡선의 핵심이다.</summary>
    private bool EntryExpired(WaveSpawnEntry entry)
        => !_waveActive || (entry.EndTime > 0f && _waveElapsed >= entry.EndTime);

    /// <summary>흩뿌리기 — 원 위 무작위, 한 마리씩. <b>D50 이전의 동작 그대로다.</b></summary>
    private IEnumerator ScatterRoutine(WaveSpawnEntry entry, int count, float interval)
    {
        for (int i = 0; i < count; i++)
        {
            if (EntryExpired(entry)) yield break;

            yield return WaitForSpawnSlot();
            if (!_waveActive) yield break;

            SpawnEnemy(entry.Enemy);
            yield return new WaitForSeconds(interval);
        }
    }

    /// <summary>
    /// 포위 — 원 위에 <b>고르게 한꺼번에</b> 놓는다.
    ///
    /// <para>🔑 조여오는 것은 따로 만들지 않았다. 적은 이미 플레이어를 향해 걸어오므로
    /// <b>고르게 둘러싸 놓기만 하면</b> 저절로 좁혀 온다. 흩뿌리기와 다른 건
    /// "사방에서 하나씩"이 아니라 <b>"한꺼번에 빙 둘러선다"</b>는 것이다.</para>
    /// </summary>
    private IEnumerator RingRoutine(WaveSpawnEntry entry, int count, float interval)
    {
        int spawned = 0;
        while (spawned < count)
        {
            if (EntryExpired(entry)) yield break;

            int n = Mathf.Min(Mathf.Max(2, ringBatch), count - spawned);

            // 시작 각도를 무작위로 돌린다 — 안 그러면 매번 같은 자리에서 나온다.
            float baseAngle = Random.value * Mathf.PI * 2f;
            for (int i = 0; i < n; i++)
            {
                yield return WaitForSpawnSlot();
                if (!_waveActive) yield break;

                float a = baseAngle + (i / (float)n) * Mathf.PI * 2f;
                SpawnEnemyAt(entry.Enemy, RadiusPoint(a));
                spawned++;
            }

            // 한 마리씩 낼 때와 총 속도를 맞춘다 — 무리로 낸다고 더 빨라지면 그건 난이도 변경이다.
            yield return new WaitForSeconds(interval * n);
        }
    }

    /// <summary>부대 — 한 방향에서 대형(줄 x 칸)을 이뤄 뭉쳐 온다.</summary>
    private IEnumerator SquadRoutine(WaveSpawnEntry entry, int count, float interval)
    {
        int cols = Mathf.Max(1, squadColumns);
        int spawned = 0;
        while (spawned < count)
        {
            if (EntryExpired(entry)) yield break;

            int n = Mathf.Min(cols * Mathf.Max(1, ringBatch / cols + 1), count - spawned);

            float a       = Random.value * Mathf.PI * 2f;
            Vector2 dir   = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            Vector2 right = new Vector2(dir.y, -dir.x);
            Vector2 head  = RadiusPoint(a);

            for (int i = 0; i < n; i++)
            {
                yield return WaitForSpawnSlot();
                if (!_waveActive) yield break;

                int col = i % cols;
                int row = i / cols;
                // 가운데 정렬 — 안 하면 대형이 한쪽으로 쏠린다.
                Vector2 offset = right * ((col - (cols - 1) * 0.5f) * squadSpacing)
                               + dir   * (row * squadSpacing);
                SpawnEnemyAt(entry.Enemy, head + offset);
                spawned++;
            }

            yield return new WaitForSeconds(interval * n);
        }
    }

    /// <summary>
    /// 땅굴 — 플레이어 <b>가까이</b>에서 솟아오른다.
    ///
    /// <para>🔴 <b>예고가 이 패턴의 전부다.</b> 소환 반경(20) 밖이 아니라 3~6 유닛 앞에서 나오므로
    /// 예고 없이 솟으면 "피할 수 없는 기습"이 된다 — <see cref="BossSlam"/> 이 세운 규칙과 같다.
    /// 그래서 표시가 안 배선돼 있으면 <b>거리를 소환 반경까지 밀어낸다</b>(= 그냥 흩뿌리기).</para>
    /// </summary>
    private IEnumerator BurrowRoutine(WaveSpawnEntry entry, int count, float interval)
    {
        int spawned = 0;
        while (spawned < count)
        {
            if (EntryExpired(entry)) yield break;

            int n = Mathf.Min(Mathf.Max(1, burrowBatch), count - spawned);
            var spots = new Vector2[n];

            bool hasTelegraph = burrowTelegraphPrefab != null;   // 🔴 ?. 금지 (I-24)
            for (int i = 0; i < n; i++)
            {
                spots[i] = BurrowPoint(hasTelegraph);
                if (hasTelegraph) Instantiate(burrowTelegraphPrefab, spots[i], Quaternion.identity);
            }

            // 예고를 보고 비킬 시간. 표시가 없으면 기다릴 이유도 없다(멀리서 나오므로).
            if (hasTelegraph) yield return new WaitForSeconds(Mathf.Max(0.35f, burrowWindup));
            if (!_waveActive) yield break;

            for (int i = 0; i < n; i++)
            {
                yield return WaitForSpawnSlot();
                if (!_waveActive) yield break;
                SpawnEnemyAt(entry.Enemy, spots[i]);
                spawned++;
            }

            yield return new WaitForSeconds(interval * n);
        }
    }

    /// <summary>소환 반경 위의 한 점. 플레이어가 없으면 원점 기준으로 둔다.</summary>
    private Vector2 RadiusPoint(float angle)
    {
        Vector2 c = playerTransform != null ? (Vector2)playerTransform.position : Vector2.zero;
        return c + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * _currentWaveData.SpawnRadius;
    }

    /// <summary>땅굴이 솟을 자리. 표시가 없으면 안전하게 소환 반경까지 밀어낸다.</summary>
    private Vector2 BurrowPoint(bool hasTelegraph)
    {
        Vector2 c = playerTransform != null ? (Vector2)playerTransform.position : Vector2.zero;
        float min = Mathf.Max(1f, burrowMinDistance);
        float max = Mathf.Max(min + 0.5f, burrowMaxDistance);
        float d   = hasTelegraph ? Random.Range(min, max) : _currentWaveData.SpawnRadius;
        return c + Random.insideUnitCircle.normalized * d;
    }

    // ── 층 배율이 걸린 값 ────────────────────────────────────────
    // 🔴 WaveData 는 ScriptableObject 다. 여기서 필드를 고치면 **애셋 파일이 더러워지고
    //    다음 런까지 남는다.** 그래서 원본은 안 건드리고 읽을 때마다 곱한다.

    /// <summary>
    /// 동시 생존 상한. <b>소환 수만 늘리고 이걸 안 늘리면 아무 일도 안 일어난다</b> —
    /// 넘치는 소환은 <see cref="WaitForSpawnSlot"/> 에서 대기할 뿐이라 화면은 그대로다.
    /// </summary>
    private int ScaledMaxAlive()
    {
        int raw = Mathf.Max(1, LayerScaling.ScaleCount(_currentWaveData.MaxAlive));
        return Mathf.Min(raw, Mathf.Max(1, maxAliveCeiling));
    }

    /// <summary>
    /// 처치 목표. 소환량이 늘었는데 이게 그대로면 <b>웨이브가 상대적으로 짧아진다</b> —
    /// 층이 깊어질수록 오히려 빨리 끝난다.
    /// </summary>
    private int ScaledKillTarget() => LayerScaling.ScaleCount(_currentWaveData.KillTarget);

    /// <summary>일시정지가 풀리고 소환 상한에 자리가 날 때까지 기다린다.</summary>
    private IEnumerator WaitForSpawnSlot()
    {
        int cap = ScaledMaxAlive();

        while (_waveActive)
        {
            if (!_wavePaused)
            {
                PruneAlive();
                if (_alive.Count < cap) yield break;
            }
            yield return null;
        }
    }

    /// <summary>엘리트 / 보스 오버라이드 소환. 이쪽은 <b>소환 상한을 무시</b>한다.</summary>
    private IEnumerator OverrideSpawnRoutine()
    {
        var data = _currentWaveData;

        if (_currentNode.StageType == StageType.Elite && data.EliteOverride != null)
        {
            while (_waveActive && _waveElapsed < data.EliteTime) yield return null;

            for (int i = 0; i < data.EliteCount; i++)
            {
                if (!_waveActive) yield break;
                while (_wavePaused) yield return null;
                SpawnEnemy(data.EliteOverride, isElite: true);
                yield return new WaitForSeconds(1.5f);
            }
        }

        if (_currentNode.StageType == StageType.Boss && data.BossOverride != null)
        {
            while (_waveActive && _waveElapsed < data.BossTime) yield return null;
            if (!_waveActive) yield break;
            while (_wavePaused) yield return null;
            var boss = SpawnEnemy(data.BossOverride, isBoss: true);

            // 등장 직후 1회. 처치음이 아니다 — 보스가 죽을 때는 EnemyDieElite 가 운다.
            AudioManager.Play(SfxId.BossAppear);

            AttachBossBrain(boss, data.BossOverride);
        }
    }

    // ── 필드 유지보수 (상한 정리 + 멀어진 적 재배치) ──────────────

    /// <summary>이 배수를 넘게 멀어진 적은 반대편으로 옮긴다. 소환 반경 기준.</summary>
    private const float RecycleRadiusMult = 1.9f;

    private IEnumerator MaintainRoutine()
    {
        var wait = new WaitForSeconds(0.25f);

        while (_waveActive)
        {
            yield return wait;
            PruneAlive();
            RecycleFarEnemies();
            RecycleFarExpDrops();
        }
    }

    /// <summary>꺼졌거나 파괴된 적을 목록에서 걷어낸다.</summary>
    private void PruneAlive()
    {
        for (int i = _alive.Count - 1; i >= 0; i--)
        {
            var e = _alive[i];
            if (e == null || !e.gameObject.activeInHierarchy)
                _alive.RemoveAt(i);
        }
    }

    /// <summary>
    /// 너무 멀어진 적을 <b>죽이지 않고</b> 플레이어 주변으로 옮긴다.
    ///
    /// <para>넉백으로 밀려났거나 플레이어가 한 방향으로 계속 달리면 적이 화면 밖에
    /// 줄줄이 남는다. 그것들은 상한만 잡아먹고 게임에 관여하지 않는다.
    /// 지우면 경험치·골드가 증발하니 옮겨서 다시 쓴다.</para>
    /// </summary>
    private void RecycleFarEnemies()
    {
        if (playerTransform == null) return;

        float limitSqr = _currentWaveData.SpawnRadius * RecycleRadiusMult;
        limitSqr *= limitSqr;
        Vector2 p = playerTransform.position;

        foreach (var e in _alive)
        {
            // 보스는 옮기지 않는다 — 갑자기 등 뒤에 나타나면 반칙처럼 느껴진다.
            if (e == null || e.IsBoss) continue;
            if (((Vector2)e.transform.position - p).sqrMagnitude < limitSqr) continue;

            e.Reposition(GetSpawnPosition());
        }
    }

    /// <summary>
    /// 너무 멀어진 경험치 구슬을 <b>그 자리에서 거둬</b> 풀로 돌려보낸다 (요청-21).
    ///
    /// <para><see cref="RecycleFarEnemies"/> 와 <b>같은 원칙, 같은 상수</b>다.
    /// 적에게는 "지우면 경험치·골드가 증발하니 옮겨서 다시 쓴다"는 규칙이 이미 있었는데
    /// 구슬에는 없었다. 그래서 구슬은 웨이브 하나가 아니라 <b>한 판 10층 전체</b>에 걸쳐 쌓인다 —
    /// <c>ClearWave</c> 가 적만 걷어내고 구슬은 안 건드리기 때문이다.</para>
    ///
    /// <para>🔴 <b>수명 상한을 두지 않는 이유</b>는 따로 있다. 구슬이 만료돼 사라지면
    /// 자석 픽업(<see cref="ExpDrop.PullAllToPlayer"/>)의 가치가 "그동안 흘린 것 전부"에서
    /// "최근 N초에 흘린 것"으로 바뀌는데, 흡수 반경이 이미 8이라 후자는 거의 0이다.
    /// 여기서는 <b>버리지 않고 거둔다</b> — 경험치는 플레이어에게 그대로 간다.</para>
    ///
    /// <para>🔴 합산해서 <see cref="ExperienceManager.CollectXp"/> 를 <b>한 번만</b> 부른다.
    /// 수백 개가 각자 부르면 그것대로 비용이고 획득음도 그만큼 쌓인다.</para>
    /// </summary>
    private void RecycleFarExpDrops()
    {
        if (playerTransform == null || _currentWaveData == null) return;

        float limitSqr = _currentWaveData.SpawnRadius * RecycleRadiusMult;
        limitSqr *= limitSqr;
        Vector2 p = playerTransform.position;

        int total = 0;
        int count = 0;

        // 🔴 뒤에서부터 돈다 — Harvest() 가 OnDisable 로 목록에서 자기를 뺀다.
        var drops = ExpDrop.Active;
        for (int i = drops.Count - 1; i >= 0; i--)
        {
            var d = drops[i];
            if (d == null) continue;
            if (((Vector2)d.transform.position - p).sqrMagnitude < limitSqr) continue;

            int amount = d.Harvest();
            if (amount <= 0) continue;

            total += amount;
            count++;
        }

        if (count == 0) return;

        // ⚠️ 여기서 레벨이 여러 번 오를 수 있다. LevelUpManager 가 대기열로 받는다 (B10).
        if (ExperienceManager.Instance != null) ExperienceManager.Instance.CollectXp(total);

        // 밸런스 판정용 숫자다 — 회수가 경험치 수입을 얼마나 늘리는지는 이 로그로만 알 수 있다.
        Debug.Log($"[WaveManager] 원거리 구슬 회수 {count}개 · XP +{total}");
    }

    private IEnumerator TimerRoutine()
    {
        _timerRemaining = _currentWaveData.SurvivalTime;
        while (_timerRemaining > 0 && _waveActive)
        {
            if (!_wavePaused)
            {
                _timerRemaining  -= Time.deltaTime;
                TotalElapsedTime += Time.deltaTime;
                WaveRemainingTime = Mathf.Max(0f, _timerRemaining);
                OnTimerUpdated?.Invoke(_timerRemaining);
            }
            yield return null;
        }
        if (_waveActive) ClearWave();
    }

    private EnemyBase SpawnEnemy(EnemyData data, bool isElite = false, bool isBoss = false)
        => SpawnEnemyAt(data, GetSpawnPosition(), isElite, isBoss);

    /// <summary>자리를 직접 지정해 소환한다. 출현 패턴(D50)이 쓴다.</summary>
    private EnemyBase SpawnEnemyAt(EnemyData data, Vector2 spawnPos, bool isElite = false, bool isBoss = false)
    {
        var go = enemyPool.Get(data.Prefab, spawnPos, Quaternion.identity);
        var enemy = go.GetComponent<EnemyBase>();
        enemy.Initialize(data, isElite, isBoss);
        _alive.Add(enemy);
        return enemy;
    }

    /// <summary>
    /// 보스에게 <see cref="BossBrain"/> 을 붙인다 (D31).
    ///
    /// <para>🔴 <c>GetComponent</c> 결과에 <c>??</c> 를 쓰지 않는다 — Unity 의 "가짜 null" 이라
    /// 동작하지 않는다 (<c>CLAUDE.md</c> §3). 보스 프리팹은 풀에서 재사용되므로
    /// <b>이미 붙어 있을 수 있다.</b></para>
    ///
    /// <para>패턴이 없으면(<c>BossPattern == null</c>) 아무것도 안 한다 —
    /// 그 보스는 예전처럼 <b>HP 만 큰 잡몹</b>으로 남는다. 조용히 실패하지 않게 경고를 남긴다.</para>
    /// </summary>
    private void AttachBossBrain(EnemyBase boss, EnemyData data)
    {
        if (boss == null) return;

        if (data.BossPattern == null)
        {
            Debug.LogWarning($"[WaveManager] '{data.name}' 에 BossPattern 이 없다 — 패턴 없는 보스로 나온다. " +
                             "Bosses.csv 에 행이 있는지, Import 를 돌렸는지 확인할 것 (D31)");
            return;
        }

        var brain = boss.GetComponent<BossBrain>();
        if (brain == null) brain = boss.gameObject.AddComponent<BossBrain>();

        // 🔴 소환 대상은 BossPatternData 가 참조로 들고 있다 (Bosses.csv 의 SummonEnemyId 를
        //    임포터가 꽂아 둔다). 예전에는 이번 웨이브 소환 목록에서 이름으로 뒤졌는데,
        //    그러면 그 웨이브에 없는 적은 못 불러서 소환이 통째로 안 돌았다 (D31 에서 실측).
        var summon = data.BossPattern.SummonEnemy;
        if (summon == null && !string.IsNullOrEmpty(data.BossPattern.SummonEnemyId))
            Debug.LogWarning($"[WaveManager] 보스 소환 대상 '{data.BossPattern.SummonEnemyId}' 참조가 비어 있다 — " +
                             "Bosses.csv 의 SummonEnemyId 가 EnemyData Id 와 맞는지 확인하고 Import 를 다시 돌릴 것 (D31)");

        brain.Initialize(data.BossPattern, enemyPool, bossSlamPrefab, summon);
        OnBossSpawned?.Invoke(boss, brain);
    }

    // ── E11 지뢰밭 (D37) ─────────────────────────────────────────

    private Coroutine _mineRoutine;

    /// <summary>
    /// 바닥에 예고 원을 계속 깐다 (E11).
    ///
    /// <para>🔑 <b>새 시스템을 만들지 않았다.</b> 보스 내려찍기(<see cref="BossSlam"/>, D31)를
    /// 그대로 쓴다 — 예고 → 폭발 → 풀 반환이 이미 검증된 코드다(D34 판정 ③).
    /// 이벤트가 다른 건 <b>규칙</b>이지 <b>부품</b>이 아니다.</para>
    ///
    /// <para>피해·반경·예고 시간은 보스 값(<c>BossPatternData</c>)을 쓰지 않고
    /// <b>여기서 약하게 고정</b>한다 — 이건 보스전이 아니라 "설 자리가 주는" 웨이브다.</para>
    /// </summary>
    public void BeginMinefield(int count, float interval, float spread)
    {
        if (bossSlamPrefab == null)
        {
            Debug.LogWarning("[WaveManager] bossSlamPrefab 이 없어 지뢰밭이 안 돈다 (D37)");
            return;
        }
        if (_mineRoutine != null) StopCoroutine(_mineRoutine);
        _mineRoutine = StartCoroutine(MinefieldRoutine(count, interval, spread));
    }

    private IEnumerator MinefieldRoutine(int count, float interval, float spread)
    {
        var wait = new WaitForSeconds(Mathf.Max(0.5f, interval));

        // 첫 무리까지 한 박자 준다 — 시작하자마자 발밑에서 터지면 예고를 볼 새가 없다.
        yield return new WaitForSeconds(1.5f);

        while (_waveActive)
        {
            while (_wavePaused) yield return null;
            if (playerTransform == null) { yield return wait; continue; }

            Vector2 p = playerTransform.position;
            for (int i = 0; i < Mathf.Max(1, count); i++)
            {
                Vector2 at = p + Random.insideUnitCircle * Mathf.Max(1f, spread);
                var go = enemyPool.Get(bossSlamPrefab, at, Quaternion.identity);
                var slam = go.GetComponent<BossSlam>();
                // 보스보다 약하게 — 반경 2.0 · 피해 12 · 예고 1.3초(보스 1.15 보다 관대하다).
                if (slam != null) slam.Initialize(12f, 2.0f, 1.3f, enemyPool);
            }
            yield return wait;
        }
        _mineRoutine = null;
    }

    /// <summary>
    /// 보스가 부른 잡몹을 놓는다 (D31).
    ///
    /// <para>🔴 <see cref="SpawnEnemy"/> 와 달리 <b>소환 반경을 무시하고 지정 좌표에 놓는다</b> —
    /// 보스 옆에서 나와야 "불러냈다"로 보인다. 대신 <c>_alive</c> 에는 똑같이 넣어
    /// 상한·재배치·처치 판정이 그대로 걸리게 한다.</para>
    /// </summary>
    public void SpawnMinion(EnemyData data, Vector2 at)
    {
        if (data == null || data.Prefab == null || enemyPool == null) return;

        // 🔴 OnEnemyKilled 와 같은 이유로 null 을 본다 (B5). 이건 공개 API 라
        //    웨이브 밖에서 불릴 수 있고, 그때 NRE 가 나면 부르는 쪽(BossBrain)이 끊긴다.
        if (_currentWaveData == null) return;
        if (_alive.Count >= ScaledMaxAlive()) return;   // 상한은 보스도 못 넘는다

        var go = enemyPool.Get(data.Prefab, at, Quaternion.identity);
        var enemy = go.GetComponent<EnemyBase>();
        enemy.Initialize(data);
        _alive.Add(enemy);
    }

    private Vector2 GetSpawnPosition()
    {
        if (playerTransform == null) return Random.insideUnitCircle * _currentWaveData.SpawnRadius;
        Vector2 dir = Random.insideUnitCircle.normalized;
        return (Vector2)playerTransform.position + dir * _currentWaveData.SpawnRadius;
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    /// <summary>
    /// 개발용 — <b>보스만 지금 부른다</b> (D64). 웨이브는 그대로 돈다.
    ///
    /// <para>사용자 요구: *"보스 소환 기능만 추가해서 따로 확인만 할 수 있게"*.
    /// <c>Clear Node</c> 와 <b>엮지 않는다</b> — 클리어는 넘어가는 일이고 이건 보는 일이다.</para>
    ///
    /// <para>🔑 정규 소환 경로(<see cref="OverrideSpawnRoutine"/>)와 <b>같은 세 줄</b>을 쓴다 —
    /// <c>SpawnEnemy(isBoss:true)</c> → 등장음 → <see cref="AttachBossBrain"/>.
    /// 하나라도 빼면 <b>패턴 없는 큰 잡몹</b>이 나와서 확인이 의미가 없어진다.</para>
    ///
    /// <para>보스 노드가 아니면 <c>_currentWaveData.BossOverride</c> 가 비어 있으므로
    /// <b><c>bossWave</c> 의 것으로 대신</b>한다 — 1층에서도 보스를 볼 수 있어야 한다.</para>
    /// </summary>
    public void DevSpawnBoss()
    {
        if (!_waveActive) return;

        EnemyData data = null;
        if (_currentWaveData != null) data = _currentWaveData.BossOverride;
        if (data == null && bossWave != null) data = bossWave.BossOverride;

        if (data == null)
        {
            Debug.LogWarning("[WaveManager] DevPanel 보스 소환 실패 — BossOverride 가 비어 있다. "
                           + "Waves.csv 의 보스 행과 SceneWiring 을 확인할 것");
            return;
        }

        var boss = SpawnEnemy(data, isBoss: true);
        AudioManager.Play(SfxId.BossAppear);
        AttachBossBrain(boss, data);
        Debug.Log($"[WaveManager] DevPanel 보스 소환 — {data.name}");
    }

    /// <summary>
    /// 개발용 — 남은 웨이브 시간을 바꾼다 (D64).
    ///
    /// <para>🔴 <b>타이머 웨이브에만 먹는다.</b> 킬 목표 웨이브는 <c>TimerRoutine</c> 자체가
    /// 안 돌아서 <see cref="WaveRemainingTime"/> 이 <c>-1</c> 이다 — 여기서 값을 넣으면
    /// HUD 에 없던 타이머가 생기고 클리어 조건과도 어긋난다. 그래서 <b>거절하고 알린다.</b></para>
    ///
    /// <para><c>0</c> 이하로 내리면 코루틴이 다음 프레임에 <c>ClearWave</c> 로 간다 —
    /// 그게 정상 종료 경로라 따로 손대지 않는다.</para>
    /// </summary>
    public void DevSetRemainingTime(float seconds)
    {
        if (!_waveActive) return;

        if (_timerRoutine == null)
        {
            Debug.LogWarning("[WaveManager] DevPanel 시간 조절 불가 — 이 웨이브는 킬 목표라 타이머가 없다");
            return;
        }

        _timerRemaining   = Mathf.Max(0f, seconds);
        WaveRemainingTime = _timerRemaining;
        OnTimerUpdated?.Invoke(_timerRemaining);
        Debug.Log($"[WaveManager] DevPanel 남은 시간 → {_timerRemaining:0.0}s");
    }

    /// <summary>지금 웨이브가 타이머로 도는지 (D64). DevPanel 이 버튼을 켤지 정할 때 본다.</summary>
    public bool HasWaveTimer => _waveActive && _timerRoutine != null;

    /// <summary>
    /// 개발용 — 지금 웨이브를 <b>즉시 클리어 처리</b>한다 (D63). <see cref="DevPanel"/> 만 부른다.
    ///
    /// <para>🔴 <c>#if UNITY_EDITOR || DEVELOPMENT_BUILD</c> 안에 둔다.
    /// <see cref="DevPanel"/> 이 파일 전체를 그 조건으로 감싸는 것과 같은 이유다 —
    /// <b>릴리즈 빌드에 치트 입구를 남기지 않는다.</b></para>
    ///
    /// <para>🔑 <c>ClearWave</c> 를 그대로 부른다. 보상 지급·이벤트 효과 해제·스테이지 진행이
    /// 전부 그 안에 있으므로, <b>따로 흉내 내면 정상 클리어와 다른 상태가 된다.</b>
    /// 특히 <see cref="GameManager.OnWaveCleared"/> 를 건너뛰면 맵이 안 넘어간다.</para>
    /// </summary>
    public void DevForceClearWave()
    {
        if (!_waveActive) return;
        Debug.Log("[WaveManager] DevPanel 강제 클리어");
        ClearWave();
    }
#endif

    private void ClearWave()
    {
        // 🔴 이벤트의 전투 한정 효과는 여기서 끝난다 (D37 · E9).
        //    처음엔 StageMapManager.AdvanceToNext 에 뒀는데, EventManager.FinishEvent 가
        //    바로 그 함수를 부르는 바람에 **켜자마자 꺼졌다.** "이번 층" 이라는 말이 애매했던 것이다 —
        //    이벤트 노드 자체가 그 층의 내용물이라 효과가 쓰일 층이 남지 않는다.
        //    ⇒ "다음 전투 동안" 으로 다시 정의했다. 이 웨이브가 끝나면 꺼진다.
        EventManager.Instance?.ClearLayerEffects();

        if (!_waveActive) return;
        _waveActive = false;

        foreach (var r in _routines)
            if (r != null) StopCoroutine(r);
        _routines.Clear();

        if (_timerRoutine != null) StopCoroutine(_timerRoutine);

        // 남은 적 처리 (퇴장 연출 후 풀로 반환).
        // _alive 가 아니라 씬 전체를 뒤지는 이유 — 목록에 안 잡힌 적(이전 웨이브 잔재 등)이
        // 남아 다음 웨이브로 넘어가는 일을 막는다.
        foreach (var enemy in FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
            enemy.ForceDespawn();
        _alive.Clear();

        Debug.Log("[WaveManager] Wave Cleared!");
        AudioManager.Play(SfxId.WaveClear);
        GameManager.Instance.OnWaveCleared(_currentNode);
    }
}
