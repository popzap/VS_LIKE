using UnityEngine;

/// <summary>
/// 적의 행동 방식.
///
/// <para>ROADMAP 은 <c>RangedEnemy : EnemyBase</c> 처럼 상속으로 나누자고 했지만
/// 그러려면 <b>행동마다 프리팹이 따로 있어야 한다</b> — 컴포넌트 타입은 런타임에 못 바꾼다.
/// 이 프로젝트는 적 6종이 <c>Enemy_Goblin.prefab</c> 하나를 공유하고 CSV 로만 구분하는
/// 구조라, 프리팹을 5개로 늘리는 대신 <b>데이터로 행동을 고르는</b> 쪽을 택했다.
/// 나중에 행동이 훨씬 복잡해지면 그때 상속으로 갈라도 <c>MoveTowardsPlayer</c> 는
/// 여전히 virtual 이라 길이 막히지 않는다.</para>
/// </summary>
public enum EnemyAI
{
    /// <summary>플레이어에게 직진. 기본값.</summary>
    Chaser,
    /// <summary>일정 거리를 유지하며 투사체를 쏜다.</summary>
    Ranged,
    /// <summary>멈춰서 조준(예고) → 고속 돌진 → 경직.</summary>
    Charger
}

[CreateAssetMenu(fileName = "EnemyData", menuName = "Game/EnemyData")]
public class EnemyData : ScriptableObject
{
    [Header("기본")]
    public string     EnemyName;
    public GameObject Prefab;
    public Sprite     Sprite;

    [Header("외형 (프리팹을 공유하고 색/크기로만 구분)")]
    public Color Tint      = Color.white;
    public float SizeScale = 1f;

    [Tooltip("걷기 프레임. Enemies.csv 의 WalkSheet 열에 적은 스프라이트시트에서 잘라 온 것이라 " +
             "인스펙터에서 직접 채워도 다음 CSV Import 때 덮어써진다. " +
             "비면 Sprite 한 장으로 버틴다(EnemyVisual 의 셰이더 바운스는 그대로 동작).")]
    public Sprite[] WalkFrames;

    [Header("스탯")]
    public float MaxHp        = 30f;
    public float MoveSpeed    = 2f;
    public float ContactDamage= 10f;
    public float Armor        = 0f;

    [Header("행동")]
    public EnemyAI AI = EnemyAI.Chaser;

    [Header("원거리 (AI = Ranged)")]
    public GameObject ProjectilePrefab;
    [Tooltip("이 거리를 유지하려 한다. 더 멀면 접근, 70% 보다 가까우면 후퇴, 사이에서는 옆걸음")]
    public float PreferredRange   = 7f;
    public float AttackCooldown   = 2.5f;
    public float ProjectileSpeed  = 6f;
    [Tooltip("0 이면 ContactDamage 를 그대로 쓴다")]
    public float ProjectileDamage = 0f;

    [Header("돌진 (AI = Charger)")]
    [Tooltip("이 거리 안에 들어오면 돌진을 시작한다")]
    public float ChargeRange     = 6f;
    [Tooltip("멈춰서 조준하는 시간(예고). 이게 없으면 그냥 빠른 적일 뿐 피할 수가 없다")]
    public float ChargeWindup    = 0.45f;
    public float ChargeSpeedMult = 3.5f;
    public float ChargeDuration  = 0.4f;
    [Tooltip("돌진 후 경직. 플레이어의 반격 기회다")]
    public float ChargeRecover   = 0.6f;
    public float ChargeCooldown  = 3f;

    [Header("경험치 / 보상")]
    public int   XpDrop       = 3;
    public int   CurrencyDrop = 0; // 상점용 재화

    [Header("엘리트 배율")]
    public float EliteHpMult      = 2.5f;
    public float EliteDamageMult  = 1.5f;
    public float EliteSpeedMult   = 1.2f;
    public int   EliteXpMult      = 3;

    [Header("보스 배율")]
    public float BossHpMult       = 10f;
    public float BossDamageMult   = 2.5f;
    public float BossSpeedMult    = 0.8f;
    public int   BossXpMult       = 10;
}
