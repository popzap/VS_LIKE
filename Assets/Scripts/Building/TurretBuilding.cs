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

        // 🔴 WeaponFire 를 재사용하면 안 된다 — AudioManager 의 중복 컷이 0.04초라
        // 터렛이 플레이어 발사음을 잡아먹는다. 내 무기가 나갔는지 모르게 된다.
        AudioManager.Play(SfxId.BuildingFire);
    }
}
