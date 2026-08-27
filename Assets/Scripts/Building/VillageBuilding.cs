using UnityEngine;

/// <summary>마을 — 쿨다운마다 경험치를 자동으로 얹어 준다.</summary>
public class VillageBuilding : BuildingBase
{
    protected override void OnCooldownElapsed()
    {
        int xp = Mathf.RoundToInt(Data.GetOutput(Level));
        if (xp <= 0) return;
        // CollectXp 안에서 XpGain 배율과 레벨업 처리가 함께 일어난다.
        ExperienceManager.Instance?.CollectXp(xp);
    }
}
