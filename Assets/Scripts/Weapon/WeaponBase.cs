using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    protected WeaponData  Data;
    protected int         Level;
    protected PlayerStats OwnerStats;
    protected ObjectPool  Pool;

    private float _timer;

    public void Initialize(WeaponData data, int level, PlayerStats owner, ObjectPool pool)
    {
        Data       = data;
        Level      = level;
        OwnerStats = owner;
        Pool       = pool;
        OnInitialized();
    }

    public void SetLevel(int newLevel)
    {
        Level = newLevel;
        OnLevelUp();
    }

    protected virtual void OnInitialized() { }
    protected virtual void OnLevelUp()     { }

    protected abstract void Fire();

    /// <summary>
    /// 사거리 안에서 가장 가까운 적.
    ///
    /// <para>ℹ️ 배열을 새로 할당하고 전수를 <c>Vector2.Distance</c>(= sqrt)로 훑는다.
    /// 구조는 나쁘지만 <b>실측에서 프레임의 1.3 % 였다</b> — 무기가 3자루뿐이고
    /// 쿨다운이 있어 초당 30회밖에 안 돈다. 고치는 순위는 낮다 (D27 · <c>Docs/PERF.md</c> §7-H 결과 ⑤).</para>
    /// </summary>
    protected virtual Transform FindNearestEnemy()
    {
        float range = Data.GetRange(Level);
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range,
                            LayerMask.GetMask("Enemy"));
        if (hits.Length == 0) return null;

        Transform nearest = null;
        float minDist = float.MaxValue;
        foreach (var h in hits)
        {
            float d = Vector2.Distance(transform.position, h.transform.position);
            if (d < minDist) { minDist = d; nearest = h.transform; }
        }
        return nearest;
    }

    private void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer <= 0)
        {
            _timer = Data.GetCooldown(Level) * OwnerStats.Final.AttackSpeed;
            Fire();
        }
    }

    // ── 크리티컬 계산 ────────────────────────────────────────────
    protected float CalculateDamage()
    {
        float dmg = Data.GetDamage(Level) * OwnerStats.Final.Damage;
        if (Random.value < OwnerStats.Final.CritChance)
            dmg *= OwnerStats.Final.CritMultiplier;
        return dmg;
    }
}
