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
    Charger,

    // ── D51 ─────────────────────────────────────────────────
    // 🔴 <b>새 값은 반드시 뒤에 붙인다.</b> 이 enum 은 EnemyData.asset 에 **정수**로
    //    직렬화돼 있다 — 중간에 끼워 넣으면 기존 적의 행동이 조용히 바뀐다.

    /// <summary>정면으로 안 온다. 멀 때는 옆으로 돌아 들어오고 가까워지면 직진으로 수렴한다.</summary>
    Flanker,

    /// <summary>이웃이 많을수록 빨라진다. 혼자 남으면 주춤한다.</summary>
    Swarmer,

    /// <summary>쫓지 않는다. 플레이어가 <b>가려는 곳</b> 앞으로 질러가 막아선다.</summary>
    Blocker
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

    [Header("측면 접근 (AI = Flanker)")]
    [Tooltip("접선 성분의 세기. 0 이면 Chaser 와 같고, 1 이면 거의 원을 그리며 돈다. " +
             "🔴 1 을 넘기면 영영 안 다가온다 — 코드가 1 로 묶는다")]
    public float FlankArcWeight  = 0.85f;

    [Tooltip("이 거리 안에 들어오면 돌기를 그만두고 직진한다. " +
             "0 이면 끝까지 돌기만 해서 절대 안 닿는다")]
    public float FlankCloseRange = 3.5f;

    [Header("무리 가속 (AI = Swarmer)")]
    [Tooltip("주변에 아무도 없을 때의 속도 배율. 1 보다 작아야 '혼자면 주춤한다'가 된다")]
    public float SwarmSoloMult  = 0.6f;

    [Tooltip("무리가 꽉 찼을 때의 속도 배율")]
    public float SwarmPackMult  = 1.35f;

    [Tooltip("이 이웃 수에서 SwarmPackMult 에 도달한다. " +
             "🔴 이웃은 분리 조향이 이미 세고 있는 값을 그대로 쓴다(추가 질의 0)")]
    public int   SwarmFullCount = 6;

    [Header("길목 차단 (AI = Blocker)")]
    [Tooltip("플레이어의 몇 초 뒤 위치를 노리나. 0 이면 그냥 Chaser 다. " +
             "🔑 이 행동은 '느려서 절대 못 쫓아오는 적'을 위한 것이다 — " +
             "Ogre 1.2 vs 플레이어 3.5~4.6 이라 추격은 성립하지 않는다")]
    public float BlockLeadTime  = 1.8f;

    [Tooltip("이 거리 안에 들어오면 예측을 그만두고 직진한다. " +
             "코앞에서까지 앞을 재면 플레이어를 두고 헛돈다")]
    public float BlockHoldRange = 2.5f;

    [Header("경험치 / 보상")]
    public int   XpDrop       = 3;
    public int   CurrencyDrop = 0; // 상점용 재화

    [Header("엘리트 배율")]
    public float EliteHpMult      = 2.5f;
    public float EliteDamageMult  = 1.5f;
    public float EliteSpeedMult   = 1.2f;
    public int   EliteXpMult      = 3;

    [Header("보스 배율")]
    /// <summary>
    /// 보스로 나올 때 쓸 페이즈·기술 파라미터 (D31). <c>null</c> 이면 <b>패턴 없는 큰 잡몹</b>이다.
    ///
    /// <para>🔴 <c>Enemies.csv</c> 에는 이 열이 <b>없다.</b> <c>Bosses.csv</c> 의 <c>EnemyId</c> 가
    /// 여기를 되꽂는다 — 보스가 6종 중 하나뿐이라 열을 늘리면 대부분이 빈칸이 되기 때문이다.
    /// ⚠️ 그래서 <c>Export ScriptableObjects -> CSV</c> 로 내보내도 이 값은 안 나온다.
    /// 복원은 <c>Bosses.csv</c> 가 한다.</para>
    /// </summary>
    public BossPatternData BossPattern;

    public float BossHpMult       = 10f;
    public float BossDamageMult   = 2.5f;
    public float BossSpeedMult    = 0.8f;
    public int   BossXpMult       = 10;
}
