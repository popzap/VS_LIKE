using UnityEngine;

/// <summary>
/// 적 처치 시 경험치 오브젝트를 드랍하고, 플레이어가 일정 반경 내에 있으면 흡수.
/// </summary>
public class ExperienceManager : MonoBehaviour
{
    public static ExperienceManager Instance { get; private set; }

    [Header("레벨업 곡선 (index = 레벨, value = 필요 XP)")]
    [SerializeField] private int[] xpThresholds = { 5,10,20,35,55,80,110,150,200,260 };

    [Header("경험치 오브젝트")]
    [SerializeField] private GameObject expDropPrefab;
    [SerializeField] private ObjectPool expPool;

    public int CurrentLevel { get; private set; } = 1;
    public int CurrentXp    { get; private set; } = 0;
    public int XpToNext     => CurrentLevel <= xpThresholds.Length
                                ? xpThresholds[CurrentLevel - 1]
                                : xpThresholds[^1] + CurrentLevel * 50;

    public System.Action<int, int> OnXpChanged;    // (current, toNext)
    public System.Action<int>      OnLevelUp;       // (newLevel)

    private PlayerStats _playerStats;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start() => _playerStats = FindFirstObjectByType<PlayerStats>();

    // ── 드랍 ────────────────────────────────────────────────────

    public void SpawnExpDrop(Vector2 position, int amount)
    {
        var go = expPool.Get(expDropPrefab, position, Quaternion.identity);
        go.GetComponent<ExpDrop>()?.Initialize(amount);
    }

    // ── 흡수 (ExpDrop 오브젝트가 호출) ─────────────────────────

    public void CollectXp(int amount)
    {
        // 획득 경험치 배율 (패시브 / 메타 강화)
        float mult = PlayerStats.Current != null ? PlayerStats.Current.Final.XpGain : 1f;
        CurrentXp += Mathf.Max(1, Mathf.RoundToInt(amount * mult));
        OnXpChanged?.Invoke(CurrentXp, XpToNext);

        while (CurrentXp >= XpToNext)
        {
            CurrentXp -= XpToNext;
            CurrentLevel++;
            OnLevelUp?.Invoke(CurrentLevel);
            TriggerLevelUp();
        }
    }

    private void TriggerLevelUp()
    {
        GameManager.Instance.WaveManager.PauseWave();
        GameManager.Instance.LevelUpManager.ShowLevelUpPanel();
        GameManager.Instance.ChangeState(GameState.LevelUp);
    }

    public void ResetForNewRun()
    {
        CurrentLevel = 1;
        CurrentXp    = 0;
    }
}
