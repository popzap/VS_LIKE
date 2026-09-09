using UnityEngine;

/// <summary>
/// <b>도전 석상을 언제·어디에 세우는지</b> 정한다 (D123).
///
/// <para>🔵 <see cref="RiftDirector"/> 와 같은 모양이고 <b>값만 다르다</b> —
/// 석상은 강제가 아니라 선택이라 <b>더 일찍·더 가까이</b> 세운다.
/// 못 보고 지나가면 그냥 안 쓴 것이지 손해가 아니다.</para>
///
/// <para>🔴 <b>석상은 적이 아니라 그냥 오브젝트다.</b> 그래서
/// <see cref="WaveManager"/> 의 웨이브 종료 정리(<c>EnemyBase</c> 만 걷는다)에 안 걸린다 —
/// <b>여기서 직접 걷어야 한다.</b> 안 걸으면 다음 층에 지난 층 석상이 서 있다.</para>
/// </summary>
public class ChallengeDirector : MonoBehaviour
{
    /// <summary>컴파일 반영 확인용 (D27).</summary>
    public const int Version = 1;

    [Header("무엇을")]
    [Tooltip("석상 프리팹. SceneWiring.csv 가 채운다.")]
    [SerializeField] private GameObject statuePrefab;

    [Header("언제")]
    [Tooltip("웨이브 시작 뒤 첫 석상까지(초).")]
    [SerializeField] private float firstDelay = 12f;

    [Tooltip("그다음부터 세우는 간격(초).")]
    [SerializeField] private float interval = 35f;

    [Tooltip("동시에 서 있을 수 있는 수(쓴 것도 센다).")]
    [SerializeField] private int maxAlive = 2;

    [Header("어디에")]
    [Tooltip("플레이어로부터 최소 거리(유닛). 🔵 균열(9)보다 가깝다 — 선택지는 눈에 보여야 한다.")]
    [SerializeField] private float minDistance = 6f;

    [Tooltip("최대 거리(유닛).")]
    [SerializeField] private float maxDistance = 11f;

    [Tooltip("석상끼리·균열과 이만큼은 떨어뜨린다(유닛).")]
    [SerializeField] private float spacing = 6f;

    [Tooltip("자리를 못 찾으면 몇 번까지 다시 뽑나.")]
    [SerializeField] private int placeTries = 12;

    private float _nextAt = -1f;

    /// <summary>지금 서 있는 석상 수 (검증용).</summary>
    public int ActiveStatues => ChallengeStatue.Active.Count;

    /// <summary>다음 석상까지 남은 초 (검증용). 웨이브가 없으면 -1.</summary>
    public float SecondsToNext => _nextAt < 0f ? -1f : Mathf.Max(0f, _nextAt - Time.time);

    private void Update()
    {
        // 🔴 GameManager.Instance.WaveManager 를 Awake 에서 캐시하지 않는다 (I-8/I-38).
        var gm = GameManager.Instance;
        if (gm == null || gm.WaveManager == null) return;
        var wm = gm.WaveManager;

        if (!wm.IsWaveActive || wm.IsBossWave)
        {
            if (_nextAt >= 0f) ClearAll();     // 웨이브가 끝났다 — 남은 석상을 걷는다
            _nextAt = -1f;
            return;
        }

        if (_nextAt < 0f) { _nextAt = Time.time + Mathf.Max(0f, firstDelay); return; }
        if (Time.time < _nextAt) return;

        // 🔴 상한에 걸려도 시계는 민다 — 안 그러면 하나가 사라지는 순간 밀린 것이 한꺼번에 선다.
        _nextAt = Time.time + Mathf.Max(5f, interval);
        if (ChallengeStatue.Active.Count >= Mathf.Max(1, maxAlive)) return;

        TryPlace();
    }

    private void TryPlace()
    {
        if (statuePrefab == null) return;

        var player = PlayerStats.Current;
        if (player == null) return;
        Vector2 p = player.transform.position;

        for (int i = 0; i < Mathf.Max(1, placeTries); i++)
        {
            Vector2 dir = Random.insideUnitCircle.normalized;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;
            Vector2 at = p + dir * Random.Range(minDistance, Mathf.Max(minDistance, maxDistance));

            if (ArenaBounds.Enabled)
            {
                Vector2 inside = ArenaBounds.Clamp(at, 1f);
                if (Vector2.Distance(inside, p) < minDistance * 0.7f) continue;
                at = inside;
            }

            if (TooClose(at)) continue;

            Instantiate(statuePrefab, at, Quaternion.identity);
            return;
        }
    }

    /// <summary>석상끼리, 그리고 <b>균열과도</b> 떨어뜨린다 — 겹치면 어느 쪽에 눌린 건지 알 수 없다.</summary>
    private bool TooClose(Vector2 at)
    {
        float sqr = spacing * spacing;

        var st = ChallengeStatue.Active;
        for (int i = 0; i < st.Count; i++)
            if (st[i] != null && ((Vector2)st[i].transform.position - at).sqrMagnitude < sqr) return true;

        var rifts = RiftEnemy.Active;
        for (int i = 0; i < rifts.Count; i++)
            if (rifts[i] != null && ((Vector2)rifts[i].transform.position - at).sqrMagnitude < sqr) return true;

        return false;
    }

    /// <summary>🔴 웨이브가 끝나면 남은 석상을 지운다 — 적이 아니라서 아무도 안 걷어 준다.</summary>
    private void ClearAll()
    {
        var list = ChallengeStatue.Active;
        for (int i = list.Count - 1; i >= 0; i--)
            if (list[i] != null) Destroy(list[i].gameObject);
        list.Clear();
    }
}
