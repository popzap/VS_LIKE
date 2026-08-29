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
