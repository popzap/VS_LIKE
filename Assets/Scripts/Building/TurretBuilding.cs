using UnityEngine;

public class TurretBuilding : BuildingBase
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float      projectileSpeed = 7f;

    protected override void Attack(Transform target)
    {
        if (projectilePrefab == null) { base.Attack(target); return; }
        Vector2 dir = ((Vector2)(target.position - transform.position)).normalized;
        var go = Pool.Get(projectilePrefab, transform.position, Quaternion.identity);
        go.GetComponent<ProjectileBase>()?.Initialize(dir, Data.GetDamage(Level), 1f,
                                                      projectileSpeed, Data.GetRange(Level) * 1.2f, Pool);
    }
}
