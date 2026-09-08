// ────────────────────────────────────────────────────────────────────────────
//  PassiveEffect  —  런타임에서 StatBlock에 수치를 더하는 인스턴스
// ────────────────────────────────────────────────────────────────────────────
public class PassiveEffect
{
    public PassiveData Data  { get; }
    public int         Level { get; private set; }

    public PassiveEffect(PassiveData data, int level)
    {
        Data  = data;
        Level = level;
    }

    public void UpgradeTo(int newLevel) => Level = newLevel;

    /// <summary>StatBlock에 이 패시브 보너스를 누적 적용.</summary>
    public void Apply(StatBlock stat)
    {
        stat.MaxHp          += Data.GetMaxHp(Level);
        stat.MoveSpeed      += Data.GetMoveSpeed(Level);
        stat.Damage         += Data.GetDamage(Level);
        stat.AttackSpeed    += Data.GetAttackSpeed(Level);
        stat.ProjectileSize += Data.GetProjectileSize(Level);
        stat.PickupRadius   += Data.GetPickupRadius(Level);
        stat.CritChance     += Data.GetCritChance(Level);
        stat.Armor          += Data.GetArmor(Level);
        stat.XpGain         += Data.GetXpGain(Level);
        stat.GoldGain       += Data.GetGoldGain(Level);
        stat.BuildingCooldown += Data.GetBuildingCooldown(Level);
        stat.Luck           += Data.GetLuck(Level);
        stat.HpRegen        += Data.GetHpRegen(Level);
        // 🔴 여기를 빠뜨리면 예외도 경고도 없이 그 스탯만 죽는다 (B13 의 Luck 이 그랬다).
        stat.SummonCount      += Data.GetSummonCount(Level);
        stat.TeleportInterval += Data.GetTeleportInterval(Level);
        stat.ShieldInterval   += Data.GetShieldInterval(Level);
        stat.EnemyHpBonus     += Data.GetEnemyHp(Level);
    }
}
