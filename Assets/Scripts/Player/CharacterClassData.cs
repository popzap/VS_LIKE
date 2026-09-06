using UnityEngine;

// ────────────────────────────────────────────────────────────────────────────
//  CharacterClassData  —  직업(클래스) 정의
// ────────────────────────────────────────────────────────────────────────────
/// <summary>
/// 런 시작 시 플레이어에게 적용되는 직업. <b>시작 무기를 주는 유일한 경로</b>다.
/// (그 전에는 레벨업 카드로 무기를 뽑기 전까지 플레이어가 맨손이었다.)
///
/// <para>수치 원본은 <c>Assets/Game/Balance/Classes.csv</c> 다. 인스펙터에서 고치지 말고
/// CSV 를 고친 뒤 <c>Game/Balance/Import CSV -&gt; ScriptableObjects</c> 를 실행할 것.</para>
/// </summary>
[CreateAssetMenu(fileName = "ClassData", menuName = "Game/CharacterClassData")]
public class CharacterClassData : ScriptableObject
{
    [Header("기본")]
    public string ClassName;
    [TextArea] public string Description;

    // 승급 단계. 1 = 시작 직업, 2 이상 = 승급으로만 도달하는 직업.
    //
    // 이 값이 하는 일은 두 가지다.
    //   ① 승급 배타 — 한 티어에 하나만 가질 수 있다 (EvolutionManager.IsClassSatisfied).
    //   ② 선택 화면 차단 — Tier > 1 이면 시작 직업 목록에서 걸러진다 (ClassSelectUI).
    //
    // ⚠️ 티어를 레시피(ClassEvolutionData)가 아니라 여기 둔 이유:
    //    배타 판정은 "내 사슬에 이미 같은 티어가 있나"를 묻는데, PlayerStats.ClassChain 이
    //    들고 있는 건 레시피가 아니라 이 클래스다. 레시피에 두면 사슬의 각 직업을
    //    "그걸 만든 레시피"로 역추적해야 하는데, T1 은 레시피가 없어 그 역추적이 성립하지 않는다.
    [Header("승급 단계")]
    [Tooltip("1 = 시작 직업. 2 이상 = 승급 전용. 같은 티어는 한 런에 하나만 가질 수 있다.")]
    public int Tier = 1;

    /// <summary>
    /// 시작 직업 선택 화면에 뜨면 안 되는 직업인가. <b>티어에서 파생된다</b> —
    /// 별도 플래그를 두면 티어와 어긋날 수 있고, 어긋나면 T2 로 런을 시작하는 사고가 난다.
    /// </summary>
    public bool IsPromotionOnly => Tier > 1;

    [Header("시작 무기")]
    public WeaponData StartingWeapon;
    public int        StartingWeaponLevel = 1;

    // 보너스는 baseStats 위에 "더해지는 차이값"이다. 직업이 기본 스탯 전체를 들고 있으면
    // Economy.csv 의 baseStats 와 원본이 둘로 갈라진다. 0 이면 영향 없음.
    [Header("스탯 보너스 (PlayerStats.baseStats 에 더해진다)")]
    public float BonusMaxHp;
    public float BonusMoveSpeed;
    public float BonusDamage;
    public float BonusAttackSpeed;
    public float BonusProjectileSize;
    public float BonusPickupRadius;
    public float BonusCritChance;
    public float BonusArmor;
    public float BonusXpGain;
    public float BonusGoldGain;

    // 직업을 가르는 두 번째 축. 스탯이 "얼마나 센가"라면 이쪽은 "무엇을 할 수 있는가"다.
    // 세는 단위는 **아이템 종류 수**이지 레벨이 아니다 — Sword Lv5 도 1칸이다.
    // 건물은 "같은 건물을 몇 채 세우나"(BuildingData.MaxCount)와 다르다. 여기는 종류 수다.
    //
    // ⚠️ 위 Bonus* 와 똑같이 **더해지는 값**이다. 총량이 아니다.
    //    기본 상한이 0 이라 1차 직업에서는 "총량 == 더하는 값" 이지만, 직업이 진화하면
    //    사슬(PlayerStats.ClassChain) 전체가 합산된다 — Warrior(3) → Sentinel(+1) = 4칸.
    //    상위 직업 행에 "총 4" 를 적으면 7칸이 된다.
    [Header("소지 상한 보너스 (종류 수 · 더해진다)")]
    [Tooltip("무기 종류 칸을 이만큼 늘린다. 진화는 재료를 소모하므로 칸을 추가로 먹지 않는다.")]
    public int BonusWeaponSlots;
    [Tooltip("패시브 종류 칸을 이만큼 늘린다.")]
    public int BonusPassiveSlots;
    [Tooltip("건물 종류 칸을 이만큼 늘린다.")]
    public int BonusBuildingSlots;

    /// <summary>
    /// <b>이 직업의 그림이 무기를 오른쪽에 들고 있나</b> (D100 · 사용자 지적).
    ///
    /// <para>🔴 걷기 시트가 전부 <b>정면 그림</b>이라 "원래 향하는 쪽"이 없다.
    /// <c>flipX</c> 는 좌우를 통째로 뒤집을 뿐이라, <b>무기가 어느 손에 그려졌는지</b>가
    /// 곧 "기본 방향"이 된다. 그런데 그게 <b>직업마다 엇갈린다</b>:</para>
    ///
    /// <list type="bullet">
    ///   <item><c>false</c> — 무기가 그림 <b>왼쪽</b> (Warrior 칼 · Mage 지팡이 …). 7종</item>
    ///   <item><c>true</c>  — 무기가 그림 <b>오른쪽</b> (Ranger 활 · Sentinel/Aegis 총구). 3종</item>
    /// </list>
    ///
    /// <para>⚠️ <b>그림에서 읽어 내는 건 포기했다.</b> 불투명 픽셀 무게중심으로 자동 판정해 봤는데
    /// Ranger 가 "중앙" 으로 나왔다 — <b>몸통 질량을 잰 것이지 무기 위치가 아니다.</b>
    /// 그래서 <b>사람이 보고 표에 적는다.</b></para>
    /// </summary>
    public bool ArtFacesRight;

    [Header("연출")]
    [Tooltip("메뉴/HUD 초상화")]
    public Sprite     Portrait;
    [Tooltip("인게임 플레이어 스프라이트. 비어 있으면 프리팹 기본 스프라이트를 그대로 쓴다.")]
    public Sprite     BodySprite;
    [Tooltip("걷기 프레임. Classes.csv 의 WalkSheet 열에 적은 스프라이트시트에서 잘라 온 것이라 " +
             "인스펙터에서 직접 채우지 말 것 — 다음 Import 때 덮어써진다.")]
    public Sprite[]   WalkFrames;
    [Tooltip("스파인/3D 등 별도 모델. 지금은 사용처 없음.")]
    public GameObject ModelPrefab;

    [Header("해금")]
    public bool UnlockedByDefault = true;
    public int  UnlockCost        = 30;

    /// <summary>합산된 <see cref="StatBlock"/> 에 이 직업의 보너스를 더한다.</summary>
    public void ApplyBonus(StatBlock s)
    {
        s.MaxHp          += BonusMaxHp;
        s.MoveSpeed      += BonusMoveSpeed;
        s.Damage         += BonusDamage;
        s.AttackSpeed    += BonusAttackSpeed;
        s.ProjectileSize += BonusProjectileSize;
        s.PickupRadius   += BonusPickupRadius;
        s.CritChance     += BonusCritChance;
        s.Armor          += BonusArmor;
        s.XpGain         += BonusXpGain;
        s.GoldGain       += BonusGoldGain;
    }

    /// <summary>이 직업이 더하는 소지 칸 수. <see cref="PlayerStats.SlotLimit"/> 가 사슬 전체를 합산한다.</summary>
    public int BonusSlots(ItemCategory category) => category switch
    {
        ItemCategory.Weapon   => BonusWeaponSlots,
        ItemCategory.Building => BonusBuildingSlots,
        _                     => BonusPassiveSlots,
    };
}
