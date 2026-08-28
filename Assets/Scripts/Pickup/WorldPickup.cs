using UnityEngine;

/// <summary>픽업의 종류. 프리팹마다 하나씩 고정해 둔다.</summary>
public enum PickupKind
{
    /// <summary>보물상자. 엘리트/보스가 떨군다. 먹으면 아이템 카드를 한 번 더 고른다.</summary>
    Chest,
    /// <summary>자석. 필드에 흩어진 경험치를 전부 끌어온다.</summary>
    Magnet
}

/// <summary>
/// 바닥에 떨어져 있다가 플레이어가 다가오면 획득되는 물건.
///
/// <para>경험치 구슬(<see cref="ExpDrop"/>)과 로직이 비슷하지만 합치지 않았다.
/// 구슬은 초당 수십 개가 생겼다 사라지는 "숫자"고, 이쪽은 한 판에 몇 개 안 나오는
/// "사건"이라 획득 시 하는 일(패널 열기·시간 정지)이 완전히 다르다.</para>
///
/// <para>끌려오는 범위는 <see cref="StatBlock.PickupRadius"/> 를 그대로 쓴다.
/// 구슬처럼 8유닛짜리 넉넉한 보정을 주지 않는 이유는, 상자는 <b>일부러 가지러 가는</b>
/// 목표물이어야 하기 때문이다. 알아서 날아오면 화면에 놓아 둔 의미가 없다.</para>
/// </summary>
public class WorldPickup : MonoBehaviour
{
    [SerializeField] private PickupKind kind;

    [Tooltip("이 거리 안에 들어오면 획득한다")]
    [SerializeField] private float touchRadius = 0.6f;

    [Tooltip("끌려오는 속도")]
    [SerializeField] private float moveSpeed = 5f;

    private Transform _player;
    private bool      _collected;

    private void OnEnable()
    {
        // 풀에서 재사용되므로 Initialize 를 따로 두지 않고 켜질 때마다 초기화한다.
        _collected = false;
        var p = GameObject.FindGameObjectWithTag("Player");
        _player = p != null ? p.transform : null;
    }

    private void Update()
    {
        if (_collected || _player == null) return;

        // 레벨업 패널이 떠 있는 동안에는 먹히지 않게 한다.
        // timeScale 이 0 이어도 Update 는 계속 돌기 때문에, 막지 않으면
        // 상자를 밟은 순간 패널이 이미 떠 있는 상태에서 또 한 번 열려 상태가 꼬인다.
        var gm = GameManager.Instance;
        if (gm != null && gm.CurrentState == GameState.LevelUp) return;

        float dist = Vector2.Distance(transform.position, _player.position);

        var   stats  = _player.GetComponent<PlayerStats>();
        float radius = stats != null ? stats.Final.PickupRadius : 2f;

        if (dist < radius)
            transform.position = Vector2.MoveTowards(
                transform.position, _player.position, moveSpeed * Time.deltaTime);

        if (dist < touchRadius) Collect();
    }

    private void Collect()
    {
        _collected = true;

        switch (kind)
        {
            case PickupKind.Chest:  ExperienceManager.Instance.GrantChestReward(); break;
            case PickupKind.Magnet: ExpDrop.PullAllToPlayer();                     break;
        }

        Despawn();
    }

    private void Despawn()
    {
        var pool = FindFirstObjectByType<ObjectPool>();
        if (pool != null) pool.Return(gameObject);
        else              gameObject.SetActive(false);
    }
}
