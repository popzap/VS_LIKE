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
    /// 소환수를 <b>몇 마리 더</b> 두는지 (D113 · 사용자 요구 "소환수 증가").
    ///
    /// <para>배율이 아니라 <b>덧셈 마릿수</b>다. 0 이면 소환 무기 하나당 몸통 한 마리 —
    /// 지금까지의 동작 그대로다. 1 이면 두 마리, 2 면 세 마리.</para>
    ///
    /// <para>🔴 <b>무기 개수가 아니라 무기 <i>하나당</i> 몸통 수</b>다.
    /// 드래곤과 문어를 둘 다 들고 이 값이 1 이면 <b>넷</b>이 따라온다.</para>
    /// </summary>
    public float SummonCount = 0f;

    /// <summary>
    /// 자동 순간이동 주기(초) — 0 이면 <b>기능 자체가 없다</b> (D113 · 사용자 요구 "텔레포트").
    ///
    /// <para>🔴 <b>다른 필드와 방향이 반대다</b> — 작을수록 좋다.
    /// 그런데 <see cref="Add"/> 는 더하므로, 이 값을 쓰는 곳이 <b>둘</b>이 되면
    /// 합쳐서 오히려 <b>느려진다.</b> 지금은 <c>Teleport</c> 패시브 하나뿐이고,
    /// 레벨별 배열이 "그 레벨일 때의 총 보너스"라 덮어쓰기처럼 동작한다.
    /// 두 번째 출처를 만들 때는 <b>여기 규칙부터 다시 정해야 한다.</b></para>
    /// </summary>
    public float TeleportInterval = 0f;

    /// <summary>
    /// 보호막 재생 주기(초) — 0 이면 <b>기능 자체가 없다</b> (D113 · 사용자 요구 "보호막").
    ///
    /// <para>막는 것은 <b>원거리 피격 한 번</b>이다. 접촉 피해는 안 막는다 —
    /// 사용자 요구가 *"원거리 공격 막아주는 쉴드"* 였고, 접촉까지 막으면
    /// 무적 픽업(<see cref="GrantInvincibility"/>)과 구분이 사라진다.</para>
    ///
    /// <para>🔴 <see cref="TeleportInterval"/> 과 같은 이유로 <b>작을수록 좋다.</b></para>
    /// </summary>
    public float ShieldInterval = 0f;

    /// <summary>
    /// <b>적 최대 체력 가산 배율</b> (D121 · 사용자 요구 "적이 강해지지만 정산 골드 증가").
    ///
    /// <para>🔴 <b>이 게임에서 유일하게 "나를 나쁘게 만드는" 스탯이다.</b>
    /// 나머지 15종은 전부 순수 상향이라 레벨업 3택이 <i>"어느 숫자를 키울까"</i> 였다 —
    /// 무엇을 골라도 손해가 없으면 그건 선택이 아니다.</para>
    ///
    /// <para>0.5 면 적 체력이 <b>1.5배</b>가 된다. <see cref="EnemyBase.Initialize"/> 가
    /// 층 배율(<c>LayerScaling.HpMult</c>) 옆에서 같이 곱한다.</para>
    ///
    /// <para>🔵 <b>보상 쪽은 새 필드가 필요 없었다</b> — <see cref="GoldGain"/> 이
    /// 런 골드와 <b>정산(메타) 골드에 똑같이</b> 곱해진다(<c>GameManager.GrantMetaGold</c>).</para>
    /// </summary>
    public float EnemyHpBonus = 0f;

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
        SummonCount      = 0f,
        TeleportInterval = 0f,
        ShieldInterval   = 0f,
        EnemyHpBonus     = 0f,
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
        SummonCount      = a.SummonCount      + b.SummonCount,
        TeleportInterval = a.TeleportInterval + b.TeleportInterval,
        ShieldInterval   = a.ShieldInterval   + b.ShieldInterval,
        EnemyHpBonus     = a.EnemyHpBonus     + b.EnemyHpBonus,
    };
}
