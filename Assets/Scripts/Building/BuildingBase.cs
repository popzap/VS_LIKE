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
    // 🔴 컴파일 반영 확인용 (D27).
    public const int Version = 2;   // 1 = 전투 노드에서만 쿨다운이 돈다 (B17) · 2 = FindEnemiesInRange (D108)

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

    /// <summary>
    /// 🔴 <b>전투 노드에서만 건물이 돈다</b> (B17 · 사용자 판단).
    ///
    /// <para>예전에는 <see cref="Update"/> 가 <b>무조건</b> 돌았다. 그래서 상점에 오래 앉아 있으면
    /// <see cref="VillageBuilding"/> 이 쿨다운마다 XP 를 얹어 <b>상점 화면에서 레벨업이 터졌고</b>,
    /// 그 레벨업 패널이 상점 UI 뒤에 숨으면서 상태 기계가 통째로 꼬였다 (`B17`).</para>
    ///
    /// <para>🔑 <b>사용자 판단이 옳았다.</b> 나는 *"막으면 상점에 있는 동안 마을이 논다"* 며
    /// 그대로 두자고 했는데, 애초에 <b>마을·농장이 전투 밖에서 도는 것 자체가 이상하다.</b>
    /// 터렛·곡사포도 마찬가지다 — 맵 화면에서 쏠 적이 없다.</para>
    ///
    /// <para>⚠️ <c>Paused</c>·<c>LevelUp</c> 은 어차피 <c>timeScale = 0</c> 이라 타이머가 안 흐른다.
    /// 여기서 막는 것은 <b>상점·맵·이벤트·결과창</b> 처럼 <b>시간은 흐르는데 전투가 아닌</b> 화면이다.</para>
    ///
    /// <para>🔑 타이머를 <b>되돌리지 않고 멈춘다</b> — 전투로 돌아오면 멈춘 자리에서 이어진다.
    /// 리셋하면 상점을 들를 때마다 건물이 한 박자씩 손해를 본다.</para>
    /// </summary>
    private static bool InCombat
    {
        get
        {
            // 🔴 GameManager.Instance 를 Awake 에서 캐시하지 않는다 (I-8 · I-38).
            var gm = GameManager.Instance;
            return gm != null && gm.CurrentState == GameState.Wave;
        }
    }

    private void Update()
    {
        if (Data == null || !CooldownActive) return;
        if (!InCombat) return;

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

    /// <summary>
    /// 사거리 안의 <b>모든</b> 적 (D108).
    ///
    /// <para><see cref="FindNearestEnemy"/> 는 "하나를 쏜다" 는 건물용이고,
    /// 이건 <b>범위 전체에 같은 일을 하는</b> 건물(냉각탑·사이렌)용이다.
    /// 둘 다 같은 <c>OverlapCircleAll</c> 을 부르지만, 두 벌로 두면
    /// 나중에 레이어 이름이나 사거리 규칙이 바뀔 때 <b>한쪽만 고치게 된다.</b></para>
    ///
    /// <para>🔵 반환 배열은 <b>매번 새로 할당된다.</b> 지금은 건물 수가 한 자리라 문제없지만,
    /// 건물이 수십 개가 되면 <c>OverlapCircleNonAlloc</c> 로 바꿀 것 (<c>PERF.md</c> 의 방침).</para>
    /// </summary>
    protected Collider2D[] FindEnemiesInRange()
        => Physics2D.OverlapCircleAll(transform.position, Data.GetRange(Level),
                                      LayerMask.GetMask("Enemy"));

    protected Transform FindNearestEnemy()
    {
        var hits = Physics2D.OverlapCircleAll(transform.position, Data.GetRange(Level),
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
