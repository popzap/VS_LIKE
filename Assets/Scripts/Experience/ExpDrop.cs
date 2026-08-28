using UnityEngine;

/// <summary>
/// 씬에 떨어지는 경험치 오브젝트
/// </summary>
public class ExpDrop : MonoBehaviour
{
    private int   _amount;
    private Transform _player;
    private bool  _collected;

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

        float dist = Vector2.Distance(transform.position, _player.position);
        var stats  = _player.GetComponent<PlayerStats>();
        float radius = stats != null ? stats.Final.PickupRadius : 2f;

        // 흡수 반경 이내 → 자동 이동. 자석에 걸렸으면 거리 조건 없이 무조건 이동.
        if (_forcePull || dist < Mathf.Max(radius, magnetActivationRange))
        {
            float speed = _forcePull ? magnetSpeed : moveSpeed;
            transform.position = Vector2.MoveTowards(transform.position, _player.position, speed * Time.deltaTime);
        }

        // 획득
        if (dist < 0.3f) Collect();
    }

    private void Collect()
    {
        if (_collected) return;
        _collected = true;
        ExperienceManager.Instance.CollectXp(_amount);
        FindFirstObjectByType<ObjectPool>()?.Return(gameObject);
        // 간단하게 비활성화
        gameObject.SetActive(false);
    }
}
