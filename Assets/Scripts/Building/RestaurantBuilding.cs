using UnityEngine;

/// <summary>
/// 식당 — 쿨다운마다 회복 아이템을 하나 떨군다.
///
/// <para>단, <b>떨군 것을 주워 가기 전에는 다음 것을 만들지 않는다.</b>
/// 쿨다운 타이머 자체를 얼려서(<see cref="CooldownActive"/>) 힐템이 바닥에 쌓이는 걸 막는다.
/// 주워 가는 순간 다시 쿨다운이 처음부터 돈다.</para>
/// </summary>
public class RestaurantBuilding : BuildingBase
{
    [SerializeField] private GameObject healPickupPrefab;
    [Tooltip("건물에서 이만큼 떨어진 곳에 떨어뜨린다.")]
    [SerializeField] private float      spawnOffset = 1f;

    private HealPickup _pending;

    protected override bool CooldownActive => _pending == null;

    protected override void OnCooldownElapsed()
    {
        if (healPickupPrefab == null) return;

        Vector2 pos = (Vector2)transform.position + Random.insideUnitCircle.normalized * spawnOffset;
        var go     = Pool.Get(healPickupPrefab, pos, Quaternion.identity);
        var pickup = go.GetComponent<HealPickup>();
        if (pickup == null) { Pool.Return(go); return; }

        pickup.Initialize(Data.GetOutput(Level), Pool, () => _pending = null);
        _pending = pickup;
    }

    // 풀에 반환됐다가 다시 꺼내질 때 옛 힐템을 붙들고 있으면 영영 얼어붙는다.
    private void OnDisable() => _pending = null;
}
