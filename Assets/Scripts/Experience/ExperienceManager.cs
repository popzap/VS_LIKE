using UnityEngine;

/// <summary>
/// 적 처치 시 경험치 오브젝트를 드랍하고, 플레이어가 일정 반경 내에 있으면 흡수.
/// </summary>
public class ExperienceManager : MonoBehaviour
{
    public static ExperienceManager Instance { get; private set; }

    [Header("레벨업 곡선 (index = 레벨, value = 필요 XP)")]
    // 🔴 값은 Economy.csv 의 ExperienceManager 행이 덮는다. 여기 기본값은 자리표시다.
    [SerializeField] private int[] xpThresholds = { 5,9,14,20,27,35,44,54,65,77 };

    [Tooltip("표를 넘어선 뒤 레벨당 늘어나는 필요 XP (D65). "
           + "예전에는 코드에 50 이 박혀 있어서 Lv11 이 갑자기 810 이 됐다 — "
           + "표의 마지막 값(77)보다 10배 큰 벽이라 그 앞에서 레벨업이 사실상 멈췄다.")]
    [SerializeField] private int xpTailStep = 12;

    [Header("경험치 오브젝트")]
    [SerializeField] private GameObject expDropPrefab;
    [SerializeField] private ObjectPool expPool;

    public int CurrentLevel { get; private set; } = 1;
    public int CurrentXp    { get; private set; } = 0;
    /// <summary>
    /// 다음 레벨까지 필요한 XP.
    ///
    /// <para>🔴 표를 넘어선 뒤의 식이 <c>+ CurrentLevel * 50</c> 이었다 (D65 이전).
    /// 표 마지막이 <c>260</c> 인데 Lv11 이 <c>260 + 550 = 810</c> 으로 <b>세 배가 넘게 튀었다</b> —
    /// 이어지는 곡선이 아니라 <b>벽</b>이라 9노드를 완주해도 13레벨에서 멈췄다.
    /// 이제는 <b>표 밖으로 나간 만큼만</b> 더한다: <c>표마지막 + (레벨 - 표길이) × xpTailStep</c>.
    /// Lv11 = 77 + 12 = 89 로 표의 마지막(77) 바로 다음 값이 된다.</para>
    /// </summary>
    public int XpToNext     => CurrentLevel <= xpThresholds.Length
                                ? xpThresholds[CurrentLevel - 1]
                                : xpThresholds[^1]
                                  + (CurrentLevel - xpThresholds.Length) * Mathf.Max(1, xpTailStep);

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

    // ── 픽업 드랍표 ──────────────────────────────────────────────
    //
    // 두 배열을 나란히 쓰는 이유는 SceneWiring.csv 가 구조체 배열을 못 쓰기 때문이다
    // (BalanceImporter.WriteProperty 는 배열 원소를 스칼라로만 쓴다).
    // 나란한 배열은 길이가 어긋나면 조용히 틀리므로 아래에서 길이를 맞춰 잘라 쓴다.

    [Tooltip("잡몹이 떨굴 수 있는 픽업 프리팹. pickupChances 와 같은 순서·같은 길이여야 한다")]
    [SerializeField] private GameObject[] pickupPrefabs;

    [Tooltip("위 프리팹 각각의 드랍 확률(0~1). 행운 배율이 곱해지기 전의 기본값이다")]
    [SerializeField] private float[] pickupChances;

    [Tooltip("드랍으로 나온 힐 픽업의 회복량. 식당이 뱉는 것과 달리 건물 레벨이 없어 여기서 정한다")]
    [SerializeField] private float healPickupAmount = 30f;

    /// <summary>엘리트/보스 처치 시 호출. 상자는 확률이 아니라 <b>확정</b>이다.</summary>
    public void SpawnChest(Vector2 position)
    {
        if (chestPrefab == null) return;
        expPool.Get(chestPrefab, position, Quaternion.identity);
    }

    /// <summary>
    /// 잡몹 처치 시 호출. 드랍표를 <b>한 번만</b> 굴려 최대 하나를 떨군다.
    ///
    /// <para>종류마다 따로 굴리지 않는 이유는, 그러면 한 마리가 폭탄과 무적을 같이 떨구는
    /// 일이 생기고 표의 "합계 6.7%" 가 실제 값과 어긋나기 때문이다. 누적 확률을 한 번
    /// 지나가면 합계가 곧 드랍률이고, 표를 읽는 사람이 계산할 게 없다.</para>
    ///
    /// <para>행운은 <b>각 항목에</b> 곱해진다 — 최종확률 = 기본확률 × (1 + Luck).
    /// 합계에 곱하는 것과 값은 같지만, 이렇게 두면 종류별 비율이 행운과 무관하게 유지된다.</para>
    /// </summary>
    public void RollPickupDrop(Vector2 position)
    {
        if (pickupPrefabs == null || pickupChances == null) return;

        int count = Mathf.Min(pickupPrefabs.Length, pickupChances.Length);
        if (count == 0) return;

        float luck = _playerStats != null ? _playerStats.Final.Luck : 0f;
        float mult = 1f + Mathf.Max(0f, luck);

        float roll = Random.value;
        float acc  = 0f;
        for (int i = 0; i < count; i++)
        {
            acc += pickupChances[i] * mult;
            if (roll >= acc) continue;

            var prefab = pickupPrefabs[i];
            if (prefab == null) return;

            var go = expPool.Get(prefab, position, Quaternion.identity);

            // 힐 픽업만 초기화가 필요하다 — 원래 식당이 회복량을 주입하던 물건이라,
            // 안 주면 0 을 회복하고 조용히 사라진다.
            var heal = go.GetComponent<HealPickup>();
            if (heal != null) heal.Initialize(healPickupAmount, expPool, null);
            return;
        }
    }

    /// <summary>
    /// 보물상자 보상. 레벨업 패널을 그대로 재사용한다.
    ///
    /// <para>상자 전용 UI 를 새로 만들지 않은 이유는, 플레이어가 얻는 것이
    /// 결국 같은 아이템 카드이기 때문이다. 화면이 하나 더 생기면 조작만 헷갈린다.
    /// (레벨은 오르지 않는다 — 경험치가 아니라 보상이다)</para>
    /// </summary>
    /// <remarks>
    /// 진화가 먼저다. 무기를 최대까지 키워 놓고도 상자에서 계속 평범한 카드만 나오면
    /// "모아 봐야 도착점이 없다"는 인상이 남는다. 완성 가능한 레시피가 있으면 그것부터 준다.
    /// </remarks>
    public void GrantChestReward()
    {
        if (EvolutionManager.Instance != null &&
            EvolutionManager.Instance.TryOfferChestEvolution()) return;

        TriggerLevelUp();
    }

    // ── 흡수 (ExpDrop 오브젝트가 호출) ─────────────────────────

    public void CollectXp(int amount)
    {
        // 획득 경험치 배율 (패시브 / 메타 강화)
        float mult = PlayerStats.Current != null ? PlayerStats.Current.Final.XpGain : 1f;
        CurrentXp += Mathf.Max(1, Mathf.RoundToInt(amount * mult));
        OnXpChanged?.Invoke(CurrentXp, XpToNext);

        // 구슬이 초당 수십 개씩 들어오지만 AudioManager 의 0.04초 중복 컷이 걸러 준다.
        AudioManager.Play(SfxId.XpPickup);

        while (CurrentXp >= XpToNext)
        {
            CurrentXp -= XpToNext;
            CurrentLevel++;

            // 팡파레는 여기에만 둔다. TriggerLevelUp 은 보물상자 보상도 같이 쓰는데,
            // 상자는 레벨이 오르는 게 아니라 카드만 한 번 더 고르는 것이라 소리가 달라야 한다.
            AudioManager.Play(SfxId.LevelUp);

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
