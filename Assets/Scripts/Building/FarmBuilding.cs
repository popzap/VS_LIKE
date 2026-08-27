using UnityEngine;

/// <summary>농장 — 쿨다운마다 골드를 자동으로 벌어 준다.</summary>
public class FarmBuilding : BuildingBase
{
    protected override void OnCooldownElapsed()
    {
        float mult = PlayerStats.Current != null ? PlayerStats.Current.Final.GoldGain : 1f;
        int gold = Mathf.RoundToInt(Data.GetOutput(Level) * mult);
        if (gold <= 0) return;

        var meta = GameManager.Instance != null ? GameManager.Instance.MetaProgression : null;
        if (meta != null) meta.AddCurrency(gold);
    }
}
