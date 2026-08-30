using System.Collections;
using UnityEngine;

/// <summary>
/// 근접 무기. 발사체를 쏘지 않고 <b>플레이어 둘레의 부채꼴</b>을 직접 벤다.
///
/// <para><see cref="WeaponData"/> 의 열을 재해석해서 쓴다 — 새 CSV 열을 만들지 않기 위해서다.</para>
/// <list type="table">
///   <item><c>Range</c> → 호의 바깥 반지름(≈2 유닛). 원거리 무기의 10~15 와 뜻이 다르다</item>
///   <item><c>ProjectileCount</c> → 한 번 쿨다운에 몇 번 베는지(연타)</item>
///   <item><c>ProjectilePrefab</c> → 날아가는 발사체가 아니라 <b>휘두름 이펙트</b>(<see cref="SwingArcFx"/>)</item>
///   <item><c>ProjectileSpeed</c> → 쓰지 않는다(0)</item>
/// </list>
/// </summary>
public class MeleeWeapon : WeaponBase
{
    [Header("호")]
    [Tooltip("겨눈 방향에서 좌우로 몇 도까지 베는지. 70 이면 앞쪽 140도가 잘린다 — " +
             "SwingArc 그림이 실제로 그리는 각(+70°~-72°)에 맞춘 값이다.")]
    [SerializeField] private float halfAngle = 70f;

    [Header("타이밍")]
    [Tooltip("휘두르기 시작해서 피해가 들어가기까지. SwingArcFx 가 30fps 이므로 " +
             "0.067 = 프레임 2, 즉 칼이 몸을 지나는 순간이다.")]
    [SerializeField] private float hitDelay = 0.067f;
    [Tooltip("연타 사이의 간격(초). ProjectileCount 가 2 이상일 때만 쓰인다.")]
    [SerializeField] private float comboInterval = 0.18f;

    protected override void Fire()
    {
        // 사거리 안에 아무도 없으면 허공을 베지 않는다.
        if (FindNearestEnemy() == null) return;
        StartCoroutine(SwingCombo());
    }

    private IEnumerator SwingCombo()
    {
        int   swings = Mathf.Max(1, Data.GetProjectileCount(Level));
        float range  = Data.GetRange(Level);

        for (int i = 0; i < swings; i++)
        {
            // 연타 도중에 적이 죽거나 뒤로 돌아갈 수 있으니 매 번 다시 겨눈다.
            var target = FindNearestEnemy();
            if (target == null) yield break;

            Vector2 dir = (Vector2)target.position - (Vector2)transform.position;
            if (dir.sqrMagnitude < 1e-6f) dir = Vector2.right;
            dir.Normalize();

            SpawnArc(dir, range);
            AudioManager.Play(SfxId.WeaponFire);

            // 🔴 피해는 휘두르는 동안 딱 한 번이다. 프레임마다 굴리면 연타와 겹쳐 몇 배가 된다.
            yield return new WaitForSeconds(hitDelay);
            Strike(dir, range);

            if (i < swings - 1) yield return new WaitForSeconds(comboInterval);
        }
    }

    /// <summary>부채꼴 안의 적을 한 번씩 벤다.</summary>
    private void Strike(Vector2 dir, float range)
    {
        Vector2 origin = transform.position;
        float   dmg    = CalculateDamage();

        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, range, LayerMask.GetMask("Enemy"));
        foreach (var h in hits)
        {
            Vector2 to = (Vector2)h.transform.position - origin;
            // 원이 아니라 부채꼴이다. 등 뒤의 적은 안 맞는다.
            if (to.sqrMagnitude > 1e-6f && Vector2.Angle(dir, to) > halfAngle) continue;

            var enemy = h.GetComponent<EnemyBase>();
            // 넉백 기준점은 플레이어 — 벤 방향으로 밀려난다.
            if (enemy != null) enemy.TakeDamage(dmg, origin);
        }
    }

    private void SpawnArc(Vector2 dir, float range)
    {
        if (Data.ProjectilePrefab == null) return;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        var go = Pool.Get(Data.ProjectilePrefab, transform.position, Quaternion.Euler(0f, 0f, angle));

        var fx = go.GetComponent<SwingArcFx>();
        // Initialize 를 안 부르면 호가 풀로 돌아가지 않고 화면에 그대로 남는다 (I-39).
        if (fx == null) { Pool.Return(go); return; }

        // 휘두르는 0.2초 동안에도 플레이어는 움직인다. 붙여 두지 않으면 호만 뒤에 남는다.
        go.transform.SetParent(transform, true);
        fx.Initialize(range, Pool);
    }
}
