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
    /// 행운. <b>드랍 확률에 곱하는 배율의 "덧셈분"</b>이다 —
    /// 최종확률 = 기본확률 × (1 + <c>Luck</c>). 0 이면 배율 1배라 아무 일도 안 일어난다.
    ///
    /// <para>다른 필드와 달리 기본값이 <b>1 이 아니라 0</b> 인 이유가 여기 있다.
    /// <c>XpGain</c>/<c>GoldGain</c> 은 그 자체가 배율이라 1 에서 시작하지만,
    /// 이건 배율에 <b>더해지는 값</b>이라 1 로 두면 시작부터 드랍이 2배가 된다.</para>
    /// </summary>
    public float Luck = 0f;

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
        Luck             = 0f,
    };
}
