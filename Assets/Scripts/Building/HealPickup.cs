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

        // 🔴 떨어뜨릴 때가 아니라 주울 때 운다. 식당이 힐템을 뱉는 건 주기적인 건물 산출이라
        // 소리를 달면 잔소리가 된다 (Farm·Village 에 소리를 안 단 것과 같은 이유).
        // 여기는 플레이어가 밟아서 일어난 일이라 소리가 정보가 된다.
        AudioManager.Play(SfxId.Heal);

        // 식당에게 "다 먹었다"를 알려야 다음 쿨다운이 돌기 시작한다.
        _onCollected?.Invoke();
        _onCollected = null;

        if (_pool != null) _pool.Return(gameObject);
        else               gameObject.SetActive(false);
    }
}
