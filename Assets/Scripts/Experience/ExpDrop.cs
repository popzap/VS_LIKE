using System.Collections.Generic;
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

    // ── 활성 구슬 목록 ───────────────────────────────────────────
    //
    // 🔴 씬 전체 순회(FindObjectsByType)를 쓰지 않기 위한 목록이다 (D27 의 방침).
    //    풀에서 꺼내지면 OnEnable 로 들어오고, 회수되면 OnDisable 로 빠진다 —
    //    풀 반환도 SetActive(false) 라 같은 경로를 탄다.
    //
    // ⚠️ static 이라 도메인 리로드로 비워질 수 있다. 읽는 쪽은 null·비활성을 건너뛸 것.

    private static readonly List<ExpDrop> ActiveDrops = new();

    /// <summary>지금 필드에 떠 있는 구슬들. 순회 중 회수하려면 <b>뒤에서부터</b> 돌 것.</summary>
    public static IReadOnlyList<ExpDrop> Active => ActiveDrops;

    private void OnEnable()  => ActiveDrops.Add(this);
    private void OnDisable() => ActiveDrops.Remove(this);

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
        // 씬 전체 순회 대신 활성 목록을 쓴다 (D27). 목록에 있는 것은 정의상 활성이다.
        for (int i = ActiveDrops.Count - 1; i >= 0; i--)
        {
            var d = ActiveDrops[i];
            if (d != null && !d._collected) d._forcePull = true;
        }
    }

    /// <summary>
    /// 멀어진 구슬을 <b>날아오게 하지 않고</b> 그 자리에서 거둔다 (요청-21).
    ///
    /// <para>🔴 <b>경험치를 직접 더하지 않고 양만 돌려준다.</b> 부르는 쪽이 합산해
    /// <see cref="ExperienceManager.CollectXp"/> 를 <b>한 번만</b> 부르기 위해서다 —
    /// 수백 개가 각자 부르면 그것대로 비용이고, 획득음도 그만큼 큐에 들어간다.</para>
    ///
    /// <para>🔴 <c>_forcePull</c> 을 켜지 않는 이유: 38유닛을 <c>magnetSpeed 22</c> 로 오면
    /// 1.7초가 걸리고 그동안 계속 <c>Update</c> 를 돈다. <b>개체 수를 줄이려는 목적에 정면으로 반한다.</b></para>
    /// </summary>
    /// <returns>거둔 경험치. 이미 회수된 구슬이면 <c>0</c>.</returns>
    public int Harvest()
    {
        if (_collected) return 0;
        _collected = true;

        int amount = _amount;
        if (SharedPool != null) SharedPool.Return(gameObject);
        else                    gameObject.SetActive(false);
        return amount;
    }

    private void Update()
    {
        if (_collected || _player == null) return;

        // sqrt 를 쓰지 않는다 — 거리는 비교에만 쓰이므로 제곱끼리 비교하면 된다.
        // 스탯도 캐시된 것을 쓴다 (Initialize 에서 한 번 잡는다).
        float distSqr = ((Vector2)transform.position - (Vector2)_player.position).sqrMagnitude;
        float radius  = _playerStats != null ? _playerStats.Final.PickupRadius : 2f;

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

    /// <summary>
    /// 구슬을 회수한다.
    ///
    /// <para>🔴 <b>XP 지급이 실패해도 반납은 반드시 돈다</b> (B15 · D84).
    /// 예전에는 <c>ExperienceManager.Instance.CollectXp(...)</c> 를 <b>가드 없이</b> 불렀다.
    /// 매니저가 없는 순간(런 종료 정산·씬 전환 중)에 구슬이 플레이어에 닿으면
    /// <c>NullReferenceException</c> 이 <b>여기서</b> 터지고, 그러면 아래 반납 코드가
    /// <b>실행되지 않는다</b> — 게다가 <c>_collected</c> 는 이미 <c>true</c> 라
    /// 다음 프레임에는 <see cref="Collect"/> 가 <b>바로 돌아간다.</b>
    /// ⇒ 구슬이 <b>화면에 영원히 남고</b> XP 도 안 들어간다.</para>
    ///
    /// <para>🔑 사용자 플레이 로그에서 실제로 2건 나왔다. 예외를 없애는 게 아니라
    /// <b>반납이 예외와 무관하게 돌도록</b> 순서를 바꾼 것이 고침의 핵심이다.</para>
    /// </summary>
    private void Collect()
    {
        if (_collected) return;
        _collected = true;

        // 🔴 ?. 를 쓰지 않는다 — 파괴된 UnityEngine.Object 는 "가짜 null" 이다 (I-24).
        var xp = ExperienceManager.Instance;
        if (xp != null) xp.CollectXp(_amount);
        else Debug.LogWarning("[ExpDrop] ExperienceManager 가 없다 — XP 는 버리고 구슬만 반납한다 (B15)");

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
