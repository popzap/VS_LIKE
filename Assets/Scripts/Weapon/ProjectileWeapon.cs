using UnityEngine;

public class ProjectileWeapon : WeaponBase
{
    protected override void Fire()
    {
        var target = FindNearestEnemy();
        if (target == null) return;

        int count = ScaledProjectileCount();
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

        // 발사체 개수만큼 울리면 소리가 뭉개진다. 한 번만 낸다.
        AudioManager.Play(SfxId.WeaponFire);
    }
}
