using UnityEngine;

public class ProjectileWeapon : WeaponBase
{
    protected override void Fire()
    {
        var target = FindNearestEnemy();
        if (target == null) return;

        int count = Data.GetProjectileCount(Level);
        float spread = count > 1 ? 15f : 0f;
        Vector2 baseDir = ((Vector2)(target.position - transform.position)).normalized;

        for (int i = 0; i < count; i++)
        {
            float angle = -spread * (count - 1) / 2f + spread * i;
            Vector2 dir = Quaternion.Euler(0, 0, angle) * baseDir;

            var go = Pool.Get(Data.ProjectilePrefab, transform.position, Quaternion.identity);
            var proj = go.GetComponent<ProjectileBase>();
            proj.Initialize(dir, CalculateDamage(), Data.GetProjectileSize(Level),
                            Data.ProjectileSpeed, Data.GetRange(Level), Pool);
        }
    }
}
