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

    // ── 보물상자 / 자석 ──────────────────────────────────────────
    //
    // 경험치와 같은 "적이 죽으면 바닥에 떨어지는 것"이라 이 클래스가 같이 맡는다.
    // 프리팹 참조는 SceneWiring.csv 의 ExperienceManager 행에서 배선한다.

    [Header("보상 드랍")]
    [SerializeField] private GameObject chestPrefab;
    [SerializeField] private GameObject magnetPrefab;

    [Tooltip("잡몹 하나가 자석을 떨굴 확률. 웨이브당 수백 마리가 죽으므로 아주 낮게 잡는다")]
    [Range(0f, 1f)]
    [SerializeField] private float magnetDropChance = 0.006f;

    /// <summary>엘리트/보스 처치 시 호출. 상자는 확률이 아니라 <b>확정</b>이다.</summary>
    public void SpawnChest(Vector2 position)
    {
        if (chestPrefab == null) return;
        expPool.Get(chestPrefab, position, Quaternion.identity);
    }

    /// <summary>잡몹 처치 시 호출. 확률로 자석을 떨군다.</summary>
    public void RollMagnetDrop(Vector2 position)
    {
        if (magnetPrefab == null) return;
        if (Random.value >= magnetDropChance) return;
        expPool.Get(magnetPrefab, position, Quaternion.identity);
    }

    /// <summary>
    /// 보물상자 보상. 레벨업 패널을 그대로 재사용한다.
    ///
    /// <para>상자 전용 UI 를 새로 만들지 않은 이유는, 플레이어가 얻는 것이
    /// 결국 같은 아이템 카드이기 때문이다. 화면이 하나 더 생기면 조작만 헷갈린다.
    /// (레벨은 오르지 않는다 — 경험치가 아니라 보상이다)</para>
    /// </summary>
    public void GrantChestReward() => TriggerLevelUp();

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
