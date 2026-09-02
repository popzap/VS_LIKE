using UnityEngine;

/// <summary>
/// 씬에 떨어지는 경험치 오브젝트
/// </summary>
public class ExpDrop : MonoBehaviour
{
    private int   _amount;
    private Transform _player;
    private bool  _collected;

    /// <summary>
    /// 플레이어 스탯. 🔴 <b>매 프레임 <c>GetComponent</c> 하지 않는다</b> (D27).
    ///
    /// <para>예전에는 <c>Update()</c> 안에서 개체마다 <c>_player.GetComponent&lt;PlayerStats&gt;()</c>
    /// 를 불렀다. 구슬이 554개면 <b>프레임당 554회</b>다 — 그런데 이 참조는 절대 안 바뀐다.
    /// 실측에서 <c>BehaviourUpdate</c> 의 40~46 %가 이 계열 비용이었다
    /// (<c>Docs/PERF.md</c> §7-H 결과 ⑥).</para>
    /// </summary>
    private PlayerStats _playerStats;

    /// <summary>자석 픽업으로 강제 회수 중인가. 켜지면 거리에 상관없이 플레이어에게 붙는다.</summary>
    private bool _forcePull;

    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float magnetActivationRange = 8f; // 근처 오면 자동 이동

    [Tooltip("자석으로 끌려올 때의 속도. 화면 끝에서 오는 것도 있어서 훨씬 빠르다")]
    [SerializeField] private float magnetSpeed = 22f;

    public void Initialize(int amount)
    {
        _amount    = amount;
        _collected = false;
        _forcePull = false;                 // 풀 재사용 — 이전 생애의 자석 상태를 지운다
        _player    = GameObject.FindGameObjectWithTag("Player")?.transform;

        // 여기서 한 번만 잡는다. 풀에서 재사용될 때마다 다시 불리므로
        // 플레이어가 교체돼도(Retry 등) 따라간다.
        _playerStats = _player != null ? _player.GetComponent<PlayerStats>() : null;
    }

    /// <summary>
    /// 필드에 있는 경험치를 전부 플레이어에게 끌어온다 (자석 픽업).
    ///
    /// <para>지속 시간이나 static 플래그를 두지 않고 <b>그 순간 살아 있는 구슬에만</b>
    /// 표시를 남긴다. 자석은 "지금 화면에 있는 걸 쓸어 담는" 물건이지
    /// 잠시 흡수 반경이 넓어지는 버프가 아니고, static 상태는 플레이 모드를 다시
    /// 켰을 때 남아 있는 사고가 잦다.</para>
    /// </summary>
    public static void PullAllToPlayer()
    {
        foreach (var d in FindObjectsByType<ExpDrop>(FindObjectsSortMode.None))
            if (d.gameObject.activeInHierarchy && !d._collected)
                d._forcePull = true;
    }

    private void Update()
    {
        if (_collected || _player == null) return;

        // 🔬 D27 A/B 계측 (임시). 최적화 전/후를 같은 실행 안에서 비교하기 위한 것이다.
        long _t0 = PerfCounters.On ? System.Diagnostics.Stopwatch.GetTimestamp() : 0L;

        float distSqr;
        float radius;

        if (PerfCounters.SlowPath)
        {
            // 🔬 최적화 전 코드 경로 — 매 프레임 GetComponent + sqrt
            float dist   = Vector2.Distance(transform.position, _player.position);
            var   stats  = _player.GetComponent<PlayerStats>();
            distSqr = dist * dist;
            radius  = stats != null ? stats.Final.PickupRadius : 2f;
        }
        else
        {
            // 🔴 sqrt 를 안 쓴다 (D27). 거리는 비교에만 쓰이므로 제곱끼리 비교하면 된다 —
            //    구슬이 수백 개면 프레임당 그만큼의 sqrt 가 사라진다.
            distSqr = ((Vector2)transform.position - (Vector2)_player.position).sqrMagnitude;
            radius  = _playerStats != null ? _playerStats.Final.PickupRadius : 2f;
        }

        if (PerfCounters.On)
        {
            PerfCounters.DropUpdateTicks += System.Diagnostics.Stopwatch.GetTimestamp() - _t0;
            PerfCounters.DropUpdateCalls++;
        }

        // 흡수 반경 이내 → 자동 이동. 자석에 걸렸으면 거리 조건 없이 무조건 이동.
        float pull = Mathf.Max(radius, magnetActivationRange);
        if (_forcePull || distSqr < pull * pull)
        {
            float speed = _forcePull ? magnetSpeed : moveSpeed;
            transform.position = Vector2.MoveTowards(transform.position, _player.position, speed * Time.deltaTime);
        }

        // 획득
        if (distSqr < 0.3f * 0.3f) Collect();
    }

    private void Collect()
    {
        if (_collected) return;
        _collected = true;
        ExperienceManager.Instance.CollectXp(_amount);

        // 🔴 씬 전체 순회를 하지 않는다 (D27). EnemyBase 가 같은 이유로 이미
        //    SharedPool 캐시를 쓰고 있었는데(EnemyBase.cs:598 주석) 여기엔 안 옮겨져 있었다.
        if (SharedPool != null) SharedPool.Return(gameObject);
        else                    gameObject.SetActive(false);
    }

    // ── 오브젝트 풀 ──────────────────────────────────────────────
    //
    // 파괴된 오브젝트는 Unity 가 == null 을 true 로 만들어 주므로 씬을 다시 로드해도
    // 알아서 다시 찾는다. (?. 는 그 판정을 못 한다 — I-24)

    private static ObjectPool _sharedPool;

    private static ObjectPool SharedPool
    {
        get
        {
            if (_sharedPool == null) _sharedPool = FindFirstObjectByType<ObjectPool>();
            return _sharedPool;
        }
    }
}
