using UnityEngine;

/// <summary>
/// 범위 피해 무기. 가장 가까운 적의 <b>위치</b>를 노린다.
///
/// <para><see cref="WeaponData.TravelPrefab"/> 이 있으면 그 몸체가 목표까지 날아간 뒤 터지고,
/// 없으면 예전처럼 목표 지점에서 곧바로 터진다. 파이어볼처럼 "날아오는 게 보여야 하는" 무기와
/// 폭탄처럼 즉발이 어울리는 무기를 같은 프리팹(Weapon_Aoe) 하나로 나눠 쓰기 위한 갈림길이다.</para>
/// </summary>
public class AoeWeapon : WeaponBase
{
    // 폭발 그림은 이 반경에 맞춰 커진다(AoeProjectile.spriteRadiusAtScaleOne).
    // 3 이면 지름 6유닛이라 화면(약 17×10)의 3분의 1을 덮는다. 곡사포와 같은 2 로 맞췄다.
    [SerializeField] private float explosionRadius = 2f;

    protected override void Fire()
    {
        var target = FindNearestEnemy();
        if (target == null) return;

        // 목표는 좌표로 굳혀서 넘긴다. 날아가는 동안 적이 죽으면 그 Transform 은
        // 풀에서 재사용돼 엉뚱한 자리로 옮겨 간다 (BombProjectile 주석 참고).
        Vector2 spot   = target.position;
        float   damage = CalculateDamage();
        float   radius = explosionRadius * Data.GetProjectileSize(Level);

        if (LaunchTravel(spot, damage, radius) || Explode(spot, damage, radius))
            AudioManager.Play(SfxId.WeaponCast);
    }

    /// <summary>날아가는 몸체를 쏜다. TravelPrefab 이 없거나 속도가 0 이면 아무 일도 하지 않는다.</summary>
    private bool LaunchTravel(Vector2 spot, float damage, float radius)
    {
        if (Data.TravelPrefab == null || Data.ProjectileSpeed <= 0f) return false;

        var go   = Pool.Get(Data.TravelPrefab, transform.position, Quaternion.identity);
        var bomb = go.GetComponent<BombProjectile>();
        if (bomb == null) { Pool.Return(go); return false; }

        bomb.Initialize(spot, damage, radius, Data.ProjectileSpeed, Data.ProjectilePrefab, Pool);
        return true;
    }

    private bool Explode(Vector2 spot, float damage, float radius)
    {
        if (Data.ProjectilePrefab == null) return false;

        var go   = Pool.Get(Data.ProjectilePrefab, spot, Quaternion.identity);
        var proj = go.GetComponent<AoeProjectile>();
        if (proj == null) { Pool.Return(go); return false; }

        proj.Initialize(damage, radius, Pool);
        return true;
    }
}
