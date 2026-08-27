using UnityEngine;

/// <summary>
/// 곡사포 — 사거리 안의 적을 향해 폭탄을 <b>쏜다</b>. 즉시 피해가 아니라 날아가는 시간이 있다.
///
/// <para>예전에는 <c>OverlapCircleAll</c> 로 그 자리에서 피해를 주고 연출 프리팹을
/// <c>Instantiate</c> 만 했다. 그 인스턴스는 아무도 지우지 않아서 폭발 그림이 맵에
/// 영구히 쌓였다 (I-39).</para>
/// </summary>
public class BombardBuilding : BuildingBase
{
    [SerializeField] private GameObject bombPrefab;
    [SerializeField] private GameObject aoeEffectPrefab;
    [SerializeField] private float      aoeRadius = 2f;
    [Tooltip("폭탄이 날아가는 속도(유닛/초). 사거리 끝까지 1초 안에 닿는 값이면 답답하지 않다.")]
    [SerializeField] private float      bombSpeed = 6f;

    protected override void Attack(Transform target)
    {
        // 목표는 좌표로 굳혀서 넘긴다. 날아가는 동안 적이 죽으면 그 Transform 은
        // 풀에서 재사용돼 엉뚱한 자리로 옮겨 간다.
        Vector2 spot = target.position;

        if (bombPrefab == null) { SpawnExplosion(spot); return; }

        var go   = Pool.Get(bombPrefab, transform.position, Quaternion.identity);
        var bomb = go.GetComponent<BombProjectile>();
        if (bomb != null)
            bomb.Initialize(spot, Data.GetDamage(Level), aoeRadius, bombSpeed, aoeEffectPrefab, Pool);
        else
            Pool.Return(go);
    }

    /// <summary>폭탄 프리팹이 없을 때의 대비책 — 그 자리에서 바로 터뜨린다.</summary>
    private void SpawnExplosion(Vector2 spot)
    {
        if (aoeEffectPrefab == null) return;

        var fx  = Pool.Get(aoeEffectPrefab, spot, Quaternion.identity);
        var aoe = fx.GetComponent<AoeProjectile>();
        if (aoe != null) aoe.Initialize(Data.GetDamage(Level), aoeRadius, Pool);
        else             Pool.Return(fx);
    }
}
