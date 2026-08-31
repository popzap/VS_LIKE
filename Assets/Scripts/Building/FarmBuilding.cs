using UnityEngine;

/// <summary>농장 — 쿨다운마다 골드를 자동으로 벌어 준다.</summary>
public class FarmBuilding : BuildingBase
{
    protected override void OnCooldownElapsed()
    {
        // GrantGold 가 GoldGain 배율을 적용하고 런 골드로 넣는다 (TODO §2-B).
        int output = Mathf.RoundToInt(Data.GetOutput(Level));
        if (output <= 0) return;

        if (GameManager.Instance != null) GameManager.Instance.GrantGold(output);
    }
}
