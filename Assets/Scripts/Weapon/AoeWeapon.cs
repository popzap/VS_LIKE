using UnityEngine;

public class AoeWeapon : WeaponBase
{
    // 폭발 그림은 이 반경에 맞춰 커진다(AoeProjectile.spriteRadiusAtScaleOne).
    // 3 이면 지름 6유닛이라 화면(약 17×10)의 3분의 1을 덮는다. 곡사포와 같은 2 로 맞췄다.
    [SerializeField] private float explosionRadius = 2f;

    protected override void Fire()
    {
        var target = FindNearestEnemy();
        if (target == null) return;

        var go = Pool.Get(Data.ProjectilePrefab, target.position, Quaternion.identity);
        var proj = go.GetComponent<AoeProjectile>();
        proj.Initialize(CalculateDamage(), explosionRadius * Data.GetProjectileSize(Level), Pool);
    }
}
