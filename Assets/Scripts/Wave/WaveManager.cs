using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// WaveData / WaveSpawnEntry 는 WaveData.cs 로 분리했다 (ScriptableObject 는 동명 파일이어야 한다).

/// <summary>
/// 스테이지 노드에 따라 웨이브를 구성·진행·클리어 판정한다.
/// </summary>
public class WaveManager : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private WaveData[]  normalWaves;   // 노말 웨이브 풀
    [SerializeField] private WaveData[]  eliteWaves;
    [SerializeField] private WaveData    bossWave;
    [SerializeField] private ObjectPool  enemyPool;
    [SerializeField] private Transform   playerTransform;

    [Tooltip("보스 내려찍기의 예고+폭발 프리팹 (D31). 비어 있으면 내려찍기만 조용히 안 나간다")]
    [SerializeField] private GameObject  bossSlamPrefab;

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

        _currentWaveData = node.StageType switch
        {
            StageType.Elite => eliteWaves[Random.Range(0, eliteWaves.Length)],
            StageType.Boss  => bossWave,
            _               => normalWaves[Random.Range(0, normalWaves.Length)]
        };

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

        if (_currentWaveData.UseKillClear && _killCount >= _currentWaveData.KillTarget)
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

        for (int i = 0; i < entry.Count; i++)
        {
            if (!_waveActive) yield break;

            // 시간창을 넘겼으면 남은 마리수는 버린다. 이게 난이도 곡선의 핵심이다 —
            // 초반 잡몹은 중반에 끊기고 후반 강적으로 교대한다.
            if (entry.EndTime > 0f && _waveElapsed >= entry.EndTime) yield break;

            yield return WaitForSpawnSlot();
            if (!_waveActive) yield break;

            SpawnEnemy(entry.Enemy);
            yield return new WaitForSeconds(interval);
        }
    }

    /// <summary>일시정지가 풀리고 소환 상한에 자리가 날 때까지 기다린다.</summary>
    private IEnumerator WaitForSpawnSlot()
    {
        int cap = Mathf.Max(1, _currentWaveData.MaxAlive);

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
        float remaining = _currentWaveData.SurvivalTime;
        while (remaining > 0 && _waveActive)
        {
            if (!_wavePaused)
            {
                remaining -= Time.deltaTime;
                TotalElapsedTime += Time.deltaTime;
                WaveRemainingTime = Mathf.Max(0f, remaining);
                OnTimerUpdated?.Invoke(remaining);
            }
            yield return null;
        }
        if (_waveActive) ClearWave();
    }

    private EnemyBase SpawnEnemy(EnemyData data, bool isElite = false, bool isBoss = false)
    {
        Vector2 spawnPos = GetSpawnPosition();
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
        if (_alive.Count >= _currentWaveData.MaxAlive) return;   // 상한은 보스도 못 넘는다

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

    private void ClearWave()
    {
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
