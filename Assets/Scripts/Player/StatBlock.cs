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
    /// 초당 체력 재생 (D74 · 사용자 요구 11).
    ///
    /// <para>🔴 <b>기본값이 0 이다.</b> <c>XpGain</c>/<c>GoldGain</c> 처럼 배율이 아니라
    /// <b>더해지는 양</b>이라, 1 로 두면 아무 것도 안 먹은 플레이어가 초당 1 씩 회복한다.
    /// 실제 기본값은 <c>Economy.csv</c> 의 <c>PlayerStats,baseStats.HpRegen</c> 이 넣는다.</para>
    /// </summary>
    public float HpRegen = 0f;

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
        HpRegen          = 0f,   // 🔴 새 필드는 여기에도 넣어야 한다 (I-21)
    };

    /// <summary>
    /// 두 블록을 <b>필드별로 더한 새 블록</b>을 만든다 (D83 · <c>B13</c> 사용자 결정 B안).
    ///
    /// <para>🔴 <b>왜 이게 필요했나</b> — <see cref="PlayerStats"/> 의 <c>RecalculateStats</c> 가
    /// 합산을 <b>손으로 나열</b>하고 있었고, 그 나열이 <b>두 번</b> 사람을 속였다:
    /// <c>Luck</c>(줄곧 빠져 있었다 · <c>B13</c>)과 <c>HpRegen</c>(<c>D74</c> 에서 실측으로 잡았다).
    /// 필드를 더할 때마다 <b>두 곳</b>(여기 <see cref="Zero"/> 와 그 나열)을 같이 고쳐야 했는데,
    /// 한 곳을 잊으면 <b>예외도 경고도 없이 그 스탯만 죽는다.</b></para>
    ///
    /// <para>🔑 이제 합산 지점이 <b>한 곳</b>이다. 필드를 더할 때 고칠 곳도 여기 하나로 준다
    /// — <see cref="Zero"/> 와 이 함수가 <b>나란히 있어서</b> 하나만 고치면 눈에 띈다.</para>
    ///
    /// <para>⚠️ <b>더하기지 곱하기가 아니다.</b> <c>Damage</c>·<c>AttackSpeed</c> 처럼
    /// "배율"인 필드도 여기서는 더한다 — 기존 나열이 그렇게 하고 있었고,
    /// <c>base</c> 가 1.0 이고 <c>meta</c> 가 증분(0.05 …)이라 그게 맞다.</para>
    /// </summary>
    public static StatBlock Add(StatBlock a, StatBlock b) => new()
    {
        MaxHp            = a.MaxHp            + b.MaxHp,
        MoveSpeed        = a.MoveSpeed        + b.MoveSpeed,
        Damage           = a.Damage           + b.Damage,
        AttackSpeed      = a.AttackSpeed      + b.AttackSpeed,
        ProjectileSize   = a.ProjectileSize   + b.ProjectileSize,
        PickupRadius     = a.PickupRadius     + b.PickupRadius,
        CritChance       = a.CritChance       + b.CritChance,
        CritMultiplier   = a.CritMultiplier   + b.CritMultiplier,
        Armor            = a.Armor            + b.Armor,
        XpGain           = a.XpGain           + b.XpGain,
        GoldGain         = a.GoldGain         + b.GoldGain,
        BuildingCooldown = a.BuildingCooldown + b.BuildingCooldown,
        Luck             = a.Luck             + b.Luck,      // 🔴 B13 — 여기가 빠져 있었다
        HpRegen          = a.HpRegen          + b.HpRegen,
    };
}
