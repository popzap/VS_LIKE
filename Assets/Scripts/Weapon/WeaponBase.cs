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
