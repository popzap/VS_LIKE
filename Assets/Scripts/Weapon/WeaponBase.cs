using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    protected WeaponData  Data;
    protected int         Level;
    protected PlayerStats OwnerStats;
    protected ObjectPool  Pool;

    private float _timer;

    /// <summary>
    /// 이번 발사에 나갈 투사체 수(근접은 연타 수). 진화 특전 배율을 곱한다 (D68).
    ///
    /// <para>🔑 <b>두 무기가 같은 식을 쓰게 여기 하나로 모았다.</b> 각자 곱하면
    /// 한쪽만 고쳐지는 날이 온다 — 근접의 "연타"와 원거리의 "투사체"는 이름이 달라서
    /// 특히 그렇다. <c>WeaponData</c> 에서 보면 둘 다 <c>ProjectileCount</c> 다.</para>
    ///
    /// <para>🔴 <c>OwnerStats</c> 가 null 일 수 있다 — 배율 없이 <b>원래 수를 그대로</b> 낸다.
    /// 여기서 0 을 내면 무기가 조용히 안 나간다.</para>
    /// </summary>
    protected int ScaledProjectileCount()
    {
        int baseCount = Mathf.Max(1, Data.GetProjectileCount(Level));
        int mult      = OwnerStats != null ? Mathf.Max(1, OwnerStats.ProjectileCountMult) : 1;
        return baseCount * mult;
    }

    /// <summary>
    /// 이 무기의 <b>공격 범위</b>. <c>Weapons.csv</c> 의 <c>Range</c> 에 패시브 배율을 곱한다 (D125).
    ///
    /// <para>🔑 <b><see cref="ScaledProjectileCount"/> 와 같은 이유로 여기 하나로 모았다.</b>
    /// <c>Data.GetRange(Level)</c> 를 부르는 자리가 무기 쪽에만 <b>아홉 곳</b>이라
    /// 각자 곱하면 다음에 무기가 하나 늘 때 <b>거기만 빠진다.</b></para>
    ///
    /// <para>🔴 <b>무기마다 이 값의 뜻이 다르다</b> — 근접은 호의 반지름, 발사체는 날아가는 거리,
    /// 장판은 뿌리는 반경, 공전체는 <b>공전 반지름</b>(D118), 지뢰는 <b>기폭 반경</b>(D119)이다.
    /// 그래도 전부 *"내 공격이 닿는 거리"* 라서 한 배율로 묶는 것이 맞다.</para>
    ///
    /// <para>🔴 <b>건물은 여기 안 걸린다.</b> 건물은 <c>Data.GetRange</c> 를 직접 부르고
    /// 자기 손잡이(<c>BuildingCooldown</c>)를 따로 갖고 있다 — 하나의 패시브가
    /// 무기와 건물을 같이 키우면 <b>건물 쪽 균형을 아무도 못 읽는다.</b></para>
    ///
    /// <para>🔴 <c>OwnerStats</c> 가 null 일 수 있다 — 그때는 배율 없이 원래 값이다.</para>
    /// </summary>
    protected float ScaledRange()
    {
        float raw = Data.GetRange(Level);
        if (OwnerStats == null) return raw;
        return raw * Mathf.Max(0.1f, OwnerStats.Final.AttackRange);
    }

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
        float range = ScaledRange();
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
