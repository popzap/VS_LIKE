using UnityEngine;

/// <summary>
/// ? 스테이지 이벤트 (확장 가능한 스텁)
/// </summary>
public class EventManager : MonoBehaviour
{
    public static EventManager Instance { get; private set; }

    [System.Serializable]
    public class GameEvent
    {
        public string Title;
        [TextArea] public string Description;
        public int   XpBonus;
        public int   CurrencyBonus;
        public bool  TriggerRandomWave;
    }

    [SerializeField] private GameEvent[] events;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void TriggerRandomEvent(StageNode node)
    {
        if (events == null || events.Length == 0) { FinishEvent(node); return; }

        var e = events[Random.Range(0, events.Length)];
        Debug.Log($"[Event] {e.Title}: {e.Description}");

        if (e.XpBonus > 0)
            ExperienceManager.Instance?.CollectXp(e.XpBonus);

        if (e.CurrencyBonus > 0)
            GameManager.Instance.MetaProgression.AddCurrency(e.CurrencyBonus);

        if (e.TriggerRandomWave)
        {
            // 상태 전환을 빠뜨리면 Event 상태로 남아 HUD 도 맵도 표시되지 않는다.
            GameManager.Instance.WaveManager.StartWave(node);
            GameManager.Instance.ChangeState(GameState.Wave);
        }
        else
        {
            FinishEvent(node);
        }
    }

    private void FinishEvent(StageNode node)
    {
        GameManager.Instance.StageMap.AdvanceToNext(node);
        GameManager.Instance.ChangeState(GameState.StageMap);
    }
}
