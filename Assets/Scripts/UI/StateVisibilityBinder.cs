using UnityEngine;

/// <summary>
/// 별도 로직 없이 "특정 상태에서만 보이는" 오브젝트용 범용 바인더.
/// 예: HUD 는 Wave / LevelUp / Paused 에서만 표시.
/// </summary>
public class StateVisibilityBinder : GameStatePanel
{
    [SerializeField] private GameState[] visibleStates;

    protected override bool IsVisibleIn(GameState state)
        => System.Array.IndexOf(visibleStates, state) >= 0;
}
