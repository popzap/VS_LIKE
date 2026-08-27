using UnityEngine;

/// <summary>
/// 식당이 떨구는 회복 아이템. 플레이어가 가까이 오면 회복시키고 풀로 돌아간다.
/// 경험치와 달리 <b>끌려오지 않는다</b> — 직접 밟으러 가야 한다.
/// </summary>
public class HealPickup : MonoBehaviour
{
    [Tooltip("플레이어와 이 거리 안이면 회복된다.")]
    [SerializeField] private float pickupRadius = 0.7f;

    private float           _amount;
    private ObjectPool      _pool;
    private System.Action   _onCollected;
    private bool            _collected;

    public void Initialize(float amount, ObjectPool pool, System.Action onCollected)
    {
        _amount      = amount;
        _pool        = pool;
        _onCollected = onCollected;
        _collected   = false;
    }

    private void Update()
    {
        if (_collected) return;

        var player = PlayerStats.Current;
        if (player == null || player.IsDead) return;
        if (Vector2.Distance(transform.position, player.transform.position) > pickupRadius) return;

        _collected = true;
        player.Heal(_amount);

        // 식당에게 "다 먹었다"를 알려야 다음 쿨다운이 돌기 시작한다.
        _onCollected?.Invoke();
        _onCollected = null;

        if (_pool != null) _pool.Return(gameObject);
        else               gameObject.SetActive(false);
    }
}
