using UnityEngine;

/// <summary>
/// 지정한 <see cref="GameState"/> 에서만 <c>panel</c> 을 활성화하는 UI 패널 베이스.
///
/// 주의: 이 스크립트가 붙은 오브젝트는 항상 활성 상태여야 한다 (보통 UI Canvas).
/// 실제로 켜고 끄는 대상은 자식으로 둔 <c>panel</c> 이다.
/// 비활성 오브젝트는 Awake 가 호출되지 않아 상태 이벤트를 구독할 수 없기 때문.
/// </summary>
public abstract class GameStatePanel : MonoBehaviour
{
    [Header("상태 연동 패널")]
    [SerializeField] protected GameObject panel;

    private GameManager _game;
    private bool        _isShown;

    protected bool IsShown => _isShown;

    protected virtual void Awake()
    {
        _game = GameManager.Instance != null
            ? GameManager.Instance
            : FindFirstObjectByType<GameManager>();

        if (_game != null) _game.OnStateChanged.AddListener(HandleStateChanged);
        if (panel != null) panel.SetActive(false);
    }

    protected virtual void OnDestroy()
    {
        if (_game != null) _game.OnStateChanged.RemoveListener(HandleStateChanged);
    }

    private void HandleStateChanged(GameState state)
    {
        bool visible = IsVisibleIn(state);
        if (visible == _isShown) return;

        _isShown = visible;
        if (panel != null) panel.SetActive(visible);

        if (visible) OnShown(state);
        else         OnHidden();
    }

    /// <summary>해당 상태에서 패널을 보여줄지 여부.</summary>
    protected abstract bool IsVisibleIn(GameState state);

    protected virtual void OnShown(GameState state) { }
    protected virtual void OnHidden() { }
}
