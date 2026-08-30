using UnityEngine;

/// <summary>
/// 적 타격감 수치를 한곳에 모은 씬 컴포넌트. <c>GameManager</c> 오브젝트에 딱 하나 붙인다.
///
/// 원래 <see cref="EnemyBase"/> 에 <c>const</c> 로 박혀 있던 값들이다. 적은 프리팹 하나를
/// 공유하는데 <c>BalanceImporter.FindSceneComponent</c> 는 **씬만** 훑기 때문에 프리팹에는
/// CSV 를 꽂을 수 없다. 그래서 씬 쪽에 값을 두고 적이 읽어 가는 구조다.
/// 수치는 <c>Assets/Game/Balance/Economy.csv</c> 가 원본이다 — 인스펙터에서 고치지 말 것.
///
/// 🔴 필드 이름은 CSV 의 <c>Field</c> 열과 **글자까지 같아야 한다.** 다르면 Import 로그에
/// <c>! CombatFeel.xxx 필드 없음</c> 이 찍히고 조용히 건너뛴다.
///
/// 🔴 <c>Default*</c> 상수는 필드 기본값이자 **컴포넌트가 없을 때의 폴백**이다. 둘을 같은
/// 상수로 묶어 둔 이유는 숫자가 두 군데로 갈라지면 "없을 때만 다른 감각으로 도는" 상황이
/// 예외 없이 조용히 생기기 때문이다.
/// </summary>
public class CombatFeel : MonoBehaviour
{
    // ── 기본값 (= EnemyBase 에 박혀 있던 값 그대로) ────────────────

    public const float DefaultEnemyKnockbackForce  = 6f;
    public const float DefaultEnemyKnockbackTime   = 0.10f;
    public const float DefaultEliteKnockbackResist = 0.4f;
    public const float DefaultBossKnockbackResist  = 0f;
    public const float DefaultDeathPopTime         = 0.14f;
    public const float DefaultDeathPopScale        = 1.25f;
    public const float DefaultEliteShakeMagnitude  = 0.20f;
    public const float DefaultEliteShakeDuration   = 0.25f;
    public const float DefaultEliteHitstop         = 0.05f;
    public const float DefaultBossShakeMagnitude   = 0.45f;
    public const float DefaultBossShakeDuration    = 0.5f;
    public const float DefaultBossHitstop          = 0.09f;

    // ── 넉백 ──────────────────────────────────────────────────────

    [Header("넉백")]
    [SerializeField] private float enemyKnockbackForce  = DefaultEnemyKnockbackForce;
    [SerializeField] private float enemyKnockbackTime   = DefaultEnemyKnockbackTime;
    [Tooltip("1 = 잡몹만큼 밀림, 0 = 안 밀림")]
    [SerializeField] private float eliteKnockbackResist = DefaultEliteKnockbackResist;
    [SerializeField] private float bossKnockbackResist  = DefaultBossKnockbackResist;

    // ── 사망 연출 ─────────────────────────────────────────────────

    [Header("사망 연출 (전 등급 공통)")]
    [SerializeField] private float deathPopTime  = DefaultDeathPopTime;
    [SerializeField] private float deathPopScale = DefaultDeathPopScale;

    // ── 처치 임팩트 (엘리트·보스 전용) ────────────────────────────

    [Header("처치 임팩트 — 엘리트")]
    [SerializeField] private float eliteShakeMagnitude = DefaultEliteShakeMagnitude;
    [SerializeField] private float eliteShakeDuration  = DefaultEliteShakeDuration;
    [SerializeField] private float eliteHitstop        = DefaultEliteHitstop;

    [Header("처치 임팩트 — 보스")]
    [SerializeField] private float bossShakeMagnitude = DefaultBossShakeMagnitude;
    [SerializeField] private float bossShakeDuration  = DefaultBossShakeDuration;
    [SerializeField] private float bossHitstop        = DefaultBossHitstop;

    // ── 조회 ──────────────────────────────────────────────────────

    private static CombatFeel _instance;

    private void Awake() => _instance = this;

    /// <summary>씬에 없으면 <c>null</c>. 그때는 아래 static 프로퍼티가 <c>Default*</c> 로 답한다.</summary>
    private static CombatFeel Current
    {
        get
        {
            // 파괴된 인스턴스는 C# 기준 null 이 아니라 "가짜 null" 이라 == null 로 걸러야 한다.
            // 씬을 리로드하면 새 인스턴스의 Awake 가 다시 채운다 (I-24 / I-38).
            if (_instance == null) _instance = FindFirstObjectByType<CombatFeel>(FindObjectsInactive.Include);
            return _instance;
        }
    }

    public static float EnemyKnockbackForce  { get { var c = Current; return c != null ? c.enemyKnockbackForce  : DefaultEnemyKnockbackForce;  } }
    public static float EnemyKnockbackTime   { get { var c = Current; return c != null ? c.enemyKnockbackTime   : DefaultEnemyKnockbackTime;   } }
    public static float EliteKnockbackResist { get { var c = Current; return c != null ? c.eliteKnockbackResist : DefaultEliteKnockbackResist; } }
    public static float BossKnockbackResist  { get { var c = Current; return c != null ? c.bossKnockbackResist  : DefaultBossKnockbackResist;  } }

    public static float DeathPopTime  { get { var c = Current; return c != null ? c.deathPopTime  : DefaultDeathPopTime;  } }
    public static float DeathPopScale { get { var c = Current; return c != null ? c.deathPopScale : DefaultDeathPopScale; } }

    public static float EliteShakeMagnitude { get { var c = Current; return c != null ? c.eliteShakeMagnitude : DefaultEliteShakeMagnitude; } }
    public static float EliteShakeDuration  { get { var c = Current; return c != null ? c.eliteShakeDuration  : DefaultEliteShakeDuration;  } }
    public static float EliteHitstop        { get { var c = Current; return c != null ? c.eliteHitstop        : DefaultEliteHitstop;        } }

    public static float BossShakeMagnitude { get { var c = Current; return c != null ? c.bossShakeMagnitude : DefaultBossShakeMagnitude; } }
    public static float BossShakeDuration  { get { var c = Current; return c != null ? c.bossShakeDuration  : DefaultBossShakeDuration;  } }
    public static float BossHitstop        { get { var c = Current; return c != null ? c.bossHitstop        : DefaultBossHitstop;        } }
}
