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

    // ── 런타임 상태 ──────────────────────────────────────────────
    private StageNode   _currentNode;
    private WaveData    _currentWaveData;
    private bool        _waveActive;
    private bool        _wavePaused;

    private int         _killCount;
    private int         _enemiesAlive;

    private Coroutine   _spawnRoutine;
    private Coroutine   _timerRoutine;

    // ── 런 누적 통계 (상점 NPC 패널 등에서 참조) ────────────────
    /// <summary>런 전체 누적 처치 수.</summary>
    public int   TotalKillCount    { get; private set; }
    /// <summary>런 전체 누적 경과 시간 (초).</summary>
    public float TotalElapsedTime  { get; private set; }

    // ── 이벤트 ───────────────────────────────────────────────────
    public System.Action<float> OnTimerUpdated;   // 남은 시간
    public System.Action<int>   OnKillCountUpdated;

    // ── Public API ───────────────────────────────────────────────

    public void StartWave(StageNode node)
    {
        _currentNode = node;
        _waveActive  = true;
        _wavePaused  = false;
        _killCount   = 0;
        _enemiesAlive = 0;
        // 누적 통계는 런 내내 유지 (리셋하지 않음)

        // 스테이지 결과창용 스냅샷 (웨이브 시작 직전 스탯/재화 기록)
        StageClearUI.Instance?.TakeSnapshot();

        _currentWaveData = node.StageType switch
        {
            StageType.Elite => eliteWaves[Random.Range(0, eliteWaves.Length)],
            StageType.Boss  => bossWave,
            _               => normalWaves[Random.Range(0, normalWaves.Length)]
        };

        _spawnRoutine = StartCoroutine(SpawnRoutine());

        if (_currentWaveData.UseTimerClear)
            _timerRoutine = StartCoroutine(TimerRoutine());
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
        if (_waveActive && !_wavePaused)
            StageClearUI.Instance?.Tick(Time.deltaTime);
    }

    /// <summary>적 사망 시 EnemyBase에서 호출.</summary>
    public void OnEnemyKilled(EnemyBase enemy)
    {
        _enemiesAlive = Mathf.Max(0, _enemiesAlive - 1);
        _killCount++;
        TotalKillCount++;
        OnKillCountUpdated?.Invoke(_killCount);
        StageClearUI.Instance?.AddKill();

        if (_currentWaveData.UseKillClear && _killCount >= _currentWaveData.KillTarget)
            ClearWave();
    }

    // ── 내부 로직 ────────────────────────────────────────────────

    private IEnumerator SpawnRoutine()
    {
        foreach (var entry in _currentWaveData.Spawns)
        {
            for (int i = 0; i < entry.Count; i++)
            {
                while (_wavePaused) yield return null;
                SpawnEnemy(entry.Enemy, isElite: false);
                yield return new WaitForSeconds(entry.SpawnInterval);
            }
        }

        // 엘리트 추가 스폰
        if (_currentNode.StageType == StageType.Elite && _currentWaveData.EliteOverride != null)
        {
            for (int i = 0; i < _currentWaveData.EliteCount; i++)
            {
                while (_wavePaused) yield return null;
                SpawnEnemy(_currentWaveData.EliteOverride, isElite: true);
                yield return new WaitForSeconds(1.5f);
            }
        }

        // 보스 스폰
        if (_currentNode.StageType == StageType.Boss && _currentWaveData.BossOverride != null)
        {
            while (_wavePaused) yield return null;
            SpawnEnemy(_currentWaveData.BossOverride, isElite: false, isBoss: true);
        }
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
                OnTimerUpdated?.Invoke(remaining);
            }
            yield return null;
        }
        if (_waveActive) ClearWave();
    }

    private void SpawnEnemy(EnemyData data, bool isElite = false, bool isBoss = false)
    {
        Vector2 spawnPos = GetSpawnPosition();
        var go = enemyPool.Get(data.Prefab, spawnPos, Quaternion.identity);
        var enemy = go.GetComponent<EnemyBase>();
        enemy.Initialize(data, isElite, isBoss);
        _enemiesAlive++;
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

        if (_spawnRoutine != null) StopCoroutine(_spawnRoutine);
        if (_timerRoutine != null) StopCoroutine(_timerRoutine);

        // 남은 적 처리 (퇴장 연출 후 풀로 반환)
        foreach (var enemy in FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
            enemy.ForceDespawn();

        Debug.Log("[WaveManager] Wave Cleared!");
        GameManager.Instance.OnWaveCleared(_currentNode);
    }
}
