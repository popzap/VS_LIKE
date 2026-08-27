using UnityEngine;

// ────────────────────────────────────────────────────────────────────────────
//  StatBlock  —  플레이어 스탯 데이터 컨테이너 (base + meta + passive 합산용)
// ────────────────────────────────────────────────────────────────────────────
[System.Serializable]
public class StatBlock
{
    public float MaxHp          = 100f;
    public float MoveSpeed      = 4f;
    public float Damage         = 1f;   // 무기 데미지 배율
    public float AttackSpeed    = 1f;   // 무기 쿨다운 배율 (낮을수록 빠름)
    public float ProjectileSize = 1f;
    public float PickupRadius   = 2f;
    public float CritChance     = 0.05f;
    public float CritMultiplier = 1.5f;
    public float Armor          = 0f;
    public float XpGain         = 1f;   // 획득 경험치 배율
    public float GoldGain       = 1f;   // 획득 골드 배율
    public float BuildingCooldown = 1f; // 건물 쿨다운 배율 (낮을수록 빠름)

    /// <summary>
    /// 전부 0 인 블록. <b>"보너스"로 쓸 때는 반드시 이걸 써야 한다.</b>
    ///
    /// <para>기본 생성자는 위 초기값(MaxHp 100, MoveSpeed 4 …)을 가진다. 그건 인스펙터에서
    /// <c>baseStats</c> 를 처음 만들 때 쓸 값이지 보너스가 아니다. 예전에
    /// <c>MetaProgressionManager.GetStatBonus()</c> 가 <c>new StatBlock()</c> 으로 시작해서
    /// <c>base + meta</c> 합산 결과가 전 스탯 2배가 되는 버그가 있었다.</para>
    /// </summary>
    public static StatBlock Zero() => new()
    {
        MaxHp          = 0f,
        MoveSpeed      = 0f,
        Damage         = 0f,
        AttackSpeed    = 0f,
        ProjectileSize = 0f,
        PickupRadius   = 0f,
        CritChance     = 0f,
        CritMultiplier = 0f,
        Armor          = 0f,
        XpGain         = 0f,
        GoldGain       = 0f,
        BuildingCooldown = 0f,
    };
}
