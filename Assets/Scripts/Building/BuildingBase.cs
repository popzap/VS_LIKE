using UnityEngine;

/// <summary>
/// 모든 건물의 공통 뼈대 — <b>쿨다운이 한 번 돌 때마다 무언가를 한다</b>.
///
/// <para>기본 동작은 "가장 가까운 적을 때린다" 지만, 마을/농장/식당처럼 공격하지 않는
/// 건물은 <see cref="OnCooldownElapsed"/> 만 갈아끼우면 된다.
/// 식당처럼 "이전 산출물을 회수하기 전까지는 다음 쿨다운을 돌리지 않는" 경우는
/// <see cref="CooldownActive"/> 를 false 로 만들어 타이머를 얼린다.</para>
/// </summary>
public class BuildingBase : MonoBehaviour
{
    protected BuildingData Data;
    protected int          Level;
    protected ObjectPool   Pool;

    /// <summary>이 인스턴스가 어떤 건물인지. 진화 제단 판정이 쓴다.</summary>
    public BuildingData DataRef => Data;

    private float _timer;

    /// <summary>
    /// 이 건물의 실제 쿨다운. 플레이어의 <see cref="StatBlock.BuildingCooldown"/> 패시브가 곱해진다.
    /// 0 이하가 되면 한 프레임에 무한히 발동하므로 하한을 둔다.
    /// </summary>
    protected float Cooldown
    {
        get
        {
            float mult = PlayerStats.Current != null ? PlayerStats.Current.Final.BuildingCooldown : 1f;
            return Mathf.Max(0.05f, Data.GetCooldown(Level) * Mathf.Max(0.1f, mult));
        }
    }

    public void Initialize(BuildingData data, int level, ObjectPool pool)
    {
        Data  = data;
        Level = level;
        Pool  = pool;
        _timer = Cooldown;   // 배치 즉시 발동하지 않도록 한 박자 쉰다
        OnInitialized();
    }

    protected virtual void OnInitialized() { }

    public void Upgrade(int newLevel)
    {
        Level = newLevel;
        OnUpgraded();
    }

    protected virtual void OnUpgraded() { }

    /// <summary>false 면 쿨다운 타이머가 멈춘다 (식당이 힐템 회수를 기다릴 때).</summary>
    protected virtual bool CooldownActive => true;

    private void Update()
    {
        if (Data == null || !CooldownActive) return;

        _timer -= Time.deltaTime;
        if (_timer > 0f) return;

        _timer = Cooldown;
        OnCooldownElapsed();
    }

    protected virtual void OnCooldownElapsed() => TryAttack();

    protected virtual void TryAttack()
    {
        var target = FindNearestEnemy();
        if (target == null) return;
        Attack(target);
    }

    protected virtual void Attack(Transform target)
    {
        // 기본 구현: 즉시 데미지
        target.GetComponent<EnemyBase>()?.TakeDamage(Data.GetDamage(Level), transform.position);
    }

    protected Transform FindNearestEnemy()
    {
        var hits = PerfCounters.OverlapCircleAll(transform.position, Data.GetRange(Level),
                   LayerMask.GetMask("Enemy"));
        if (hits.Length == 0) return null;

        Transform nearest = null;
        float min = float.MaxValue;
        foreach (var h in hits)
        {
            float d = Vector2.Distance(transform.position, h.transform.position);
            if (d < min) { min = d; nearest = h.transform; }
        }
        return nearest;
    }

    private void OnDrawGizmosSelected()
    {
        if (Data == null) return;
        Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, Data.GetRange(Level));
    }
}
