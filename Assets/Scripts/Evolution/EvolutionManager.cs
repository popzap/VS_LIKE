using System.Collections.Generic;
using UnityEngine;

// ────────────────────────────────────────────────────────────────────────────
//  EvolutionManager  —  진화 조건 판정 / 실행
// ────────────────────────────────────────────────────────────────────────────
/// <summary>
/// 레시피(<see cref="EvolutionData"/>)가 완성됐는지 보고, 두 경로 중 하나로 내보낸다.
///
/// <list type="bullet">
/// <item><b>보물상자</b> — 건물이 재료가 아닌 레시피. 상자를 열면 아이템 3택 대신
///       진화 카드 한 장이 뜬다 (<see cref="TryOfferChestEvolution"/>).</item>
/// <item><b>제단</b> — 건물이 재료인 <i>최종 진화</i>. 그 건물 근처에서 E 를 누른다
///       (<see cref="TryEvolveAtAltar"/>).</item>
/// </list>
///
/// <para><b>직업 승급</b>(<see cref="ClassEvolutionData"/>)도 같은 제단·같은 E 키를 쓴다.
/// 무기 진화와 달리 재료를 <b>소모하지 않고</b>, 결과가 아이템이 아니라
/// <see cref="PlayerStats.EvolveClass"/> 로 사슬에 덧붙는 직업이다.</para>
///
/// <para>판정에 쓰는 인벤토리는 <see cref="LevelUpManager"/> 한 곳뿐이다 —
/// 무기·건물·패시브가 전부 거기 <see cref="ItemData"/> 로 들어가 있어서
/// 카테고리별로 다른 시스템을 뒤질 필요가 없다.</para>
/// </summary>
public class EvolutionManager : MonoBehaviour
{
    public static EvolutionManager Instance { get; private set; }

    [Header("레시피")]
    [Tooltip("모든 무기 진화 레시피. SceneWiring.csv 의 EvolutionManager,allEvolutions 로 배선한다.")]
    [SerializeField] private EvolutionData[] allEvolutions;

    [Tooltip("모든 직업 승급 레시피. SceneWiring.csv 의 EvolutionManager,classEvolutions 로 배선한다.")]
    [SerializeField] private ClassEvolutionData[] classEvolutions;

    [Header("제단")]
    [Tooltip("건물에서 이 거리 안에 있어야 E 로 최종 진화를 할 수 있다. 건물 설치 간격(1.2)보다 넉넉해야 한다.")]
    [SerializeField] private float altarRadius = 2.2f;

    // 이번 런에서 이미 완성한 레시피. 결과 아이템을 상점에서 팔아 버려도 다시 만들 수는 없다.
    private readonly HashSet<EvolutionData> _completed = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    /// <summary>
    /// <see cref="GameManager.LevelUpManager"/> 를 쓸 때마다 새로 묻는다.
    /// <b>Awake 에서 캐시하면 안 된다</b> — 그 참조는 <c>GameManager.Start()</c> 에서 채워지는데
    /// 모든 <c>Awake</c> 가 모든 <c>Start</c> 보다 먼저 돌아 늘 null 이 잡힌다 (I-8, I-38).
    /// </summary>
    private LevelUpManager LevelUp
        => GameManager.Instance != null ? GameManager.Instance.LevelUpManager : null;

    public float AltarRadius => altarRadius;

    // ── 런 초기화 ────────────────────────────────────────────────

    /// <summary>
    /// 새 런 시작 시 <see cref="GameManager.StartRun"/> 가 호출한다.
    ///
    /// <para>진화 결과 아이템은 <c>allItems</c> 에 없어서
    /// <see cref="LevelUpManager.ResetRunState"/> 가 <c>CurrentLevel</c> 을 못 지운다.
    /// 그 값은 SO 애셋에 남는 런타임 값이라 씬을 다시 로드해도 살아남는다 — 여기서 직접 지운다.</para>
    /// </summary>
    public void ResetRunState()
    {
        _completed.Clear();

        if (allEvolutions == null) return;
        foreach (var evo in allEvolutions)
        {
            if (evo == null) continue;
            if (evo.ResultItem != null) evo.ResultItem.CurrentLevel = 0;
        }
    }

    // ── 판정 ─────────────────────────────────────────────────────

    /// <summary>재료를 전부 요구 레벨 이상으로 들고 있는가.</summary>
    public bool IsSatisfied(EvolutionData evo)
    {
        if (evo == null || evo.ResultItem == null) return false;
        if (_completed.Contains(evo)) return false;

        var lm = LevelUp;
        if (lm == null) return false;
        if (lm.HasItem(evo.ResultItem)) return false;          // 이미 들고 있다

        if (evo.Ingredients == null || evo.Ingredients.Length == 0) return false;

        for (int i = 0; i < evo.Ingredients.Length; i++)
        {
            var item = evo.Ingredients[i];
            if (item == null) return false;                     // 데이터가 깨졌다
            if (lm.GetItemLevel(item) < evo.GetRequiredLevel(i)) return false;
        }
        return true;
    }

    /// <summary>완성 가능한 레시피 전부. HUD 안내에 쓴다.</summary>
    public List<EvolutionData> GetReadyEvolutions()
    {
        var list = new List<EvolutionData>();
        if (allEvolutions == null) return list;

        foreach (var evo in allEvolutions)
            if (IsSatisfied(evo)) list.Add(evo);

        return list;
    }

    // ── 실행 ─────────────────────────────────────────────────────

    /// <summary>
    /// 재료를 소모하고 결과를 지급한다.
    ///
    /// <para><b>무기 재료만 사라진다.</b> 패시브가 사라지면 스탯이 되레 내려가 진화가 손해가 되고,
    /// 건물이 사라지면 방금 상호작용한 제단이 눈앞에서 증발한다.
    /// 무기를 먼저 비우는 것도 이유가 있다 — 무기 슬롯이 6칸이라 꽉 찬 상태에서
    /// 결과를 먼저 넣으면 <see cref="WeaponManager"/> 가 조용히 거절한다.</para>
    /// </summary>
    public bool Evolve(EvolutionData evo)
    {
        if (!IsSatisfied(evo)) return false;

        var lm = LevelUp;
        if (lm == null) return false;

        foreach (var item in evo.Ingredients)
        {
            if (item == null) continue;
            if (item.Category != ItemCategory.Weapon) continue;
            lm.RemoveItemFull(item);
        }

        lm.GrantEvolvedItem(evo.ResultItem);
        _completed.Add(evo);

        AudioManager.Play(SfxId.LevelUp);
        Debug.Log($"[EvolutionManager] 진화 완료 — {evo.EvolutionName} → {evo.ResultItem.ItemName}");
        return true;
    }

    // ── 경로 ① 보물상자 ──────────────────────────────────────────

    /// <summary>
    /// 상자를 열 때 <see cref="ExperienceManager.GrantChestReward"/> 가 먼저 묻는다.
    /// 완성 가능한(건물이 필요 없는) 레시피가 있으면 진화 카드를 띄우고 <c>true</c>.
    /// </summary>
    public bool TryOfferChestEvolution()
    {
        EvolutionData picked = null;
        foreach (var evo in GetReadyEvolutions())
        {
            if (evo.IsFinalEvolution) continue;   // 최종 진화는 제단에서만
            picked = evo;
            break;
        }
        if (picked == null) return false;

        var lm = LevelUp;
        if (lm == null) return false;

        GameManager.Instance.WaveManager.PauseWave();
        lm.ShowForcedChoices(new List<ItemData> { picked.ResultItem }, _ => Evolve(picked));
        GameManager.Instance.ChangeState(GameState.LevelUp);
        return true;
    }

    // ── 경로 ② 제단 (건물 앞 E) ─────────────────────────────────

    /// <summary>
    /// <paramref name="origin"/> 근처 건물에서 완성 가능한 최종 진화. 없으면 null.
    /// 프롬프트 표시와 실제 실행이 같은 판정을 쓰도록 따로 뺐다.
    /// </summary>
    public EvolutionData FindAltarEvolution(Vector2 origin)
    {
        var bm = GameManager.Instance != null ? GameManager.Instance.BuildingMgr : null;
        if (bm == null) return null;

        var building = bm.FindNearestPlaced(origin, altarRadius);
        if (building == null || building.DataRef == null) return null;

        foreach (var evo in GetReadyEvolutions())
            if (evo.AltarBuilding == building.DataRef) return evo;

        return null;
    }

    /// <summary>
    /// E 키가 호출한다. 조건이 맞으면 그 자리에서 완성한다.
    ///
    /// <para><b>직업 승급을 먼저 본다.</b> 한 건물에 두 레시피가 걸릴 수 있는데,
    /// 직업 승급은 재료를 안 먹고 무기 진화는 먹는다 — 무기가 먼저 사라지면
    /// 그 무기를 재료로 쓰던 승급이 조용히 불가능해진다.</para>
    /// </summary>
    public bool TryEvolveAtAltar(Vector2 origin)
    {
        var cls = FindAltarClassEvolution(origin);
        if (cls != null && EvolveClass(cls)) return true;

        var evo = FindAltarEvolution(origin);
        return evo != null && Evolve(evo);
    }

    // ── 직업 승급 ────────────────────────────────────────────────

    /// <summary>
    /// 이 승급이 지금 가능한가.
    ///
    /// <para>"이미 했다"는 <see cref="PlayerStats.ClassChain"/> 로 판정한다.
    /// 무기 진화처럼 별도 완료 집합을 두지 않는 이유는, 직업은 팔거나 잃을 수 없어서
    /// 사슬 자체가 곧 이력이기 때문이다 — 런 초기화도 따로 필요 없다.</para>
    /// </summary>
    public bool IsClassSatisfied(ClassEvolutionData evo)
    {
        if (evo == null || evo.ResultClass == null) return false;

        var ps = PlayerStats.Current;
        if (ps == null) return false;
        if (ps.HasClass(evo.ResultClass)) return false;

        // 선행 직업 지정이 있으면 사슬 어딘가에 있어야 한다. 끝(현재 직업)만 보면
        // 같은 T2 에서 갈라지는 T3 두 갈래 중 하나를 타는 순간 다른 쪽이 막힌다.
        if (evo.FromClass != null && !ps.HasClass(evo.FromClass)) return false;

        var lm = LevelUp;
        if (lm == null) return false;

        if (evo.Ingredients == null || evo.Ingredients.Length == 0) return false;

        for (int i = 0; i < evo.Ingredients.Length; i++)
        {
            var item = evo.Ingredients[i];
            if (item == null) return false;
            if (lm.GetItemLevel(item) < evo.GetRequiredLevel(i)) return false;
        }
        return true;
    }

    /// <summary>완성 가능한 승급 전부. 프롬프트 안내에 쓴다.</summary>
    public List<ClassEvolutionData> GetReadyClassEvolutions()
    {
        var list = new List<ClassEvolutionData>();
        if (classEvolutions == null) return list;

        foreach (var evo in classEvolutions)
            if (IsClassSatisfied(evo)) list.Add(evo);

        return list;
    }

    /// <summary><paramref name="origin"/> 근처 건물에서 가능한 승급. 없으면 null.</summary>
    public ClassEvolutionData FindAltarClassEvolution(Vector2 origin)
    {
        var bm = GameManager.Instance != null ? GameManager.Instance.BuildingMgr : null;
        if (bm == null) return null;

        var building = bm.FindNearestPlaced(origin, altarRadius);
        if (building == null || building.DataRef == null) return null;

        foreach (var evo in GetReadyClassEvolutions())
            if (evo.AltarBuilding == building.DataRef) return evo;

        return null;
    }

    /// <summary>
    /// 승급을 실행한다. <b>재료는 그대로 둔다</b> — 돌려받는 게 무기가 아니라 직업이라
    /// 재료까지 먹으면 순수한 손해가 된다 (<see cref="ClassEvolutionData"/> 참고).
    /// </summary>
    public bool EvolveClass(ClassEvolutionData evo)
    {
        if (!IsClassSatisfied(evo)) return false;

        var ps = PlayerStats.Current;
        if (ps == null) return false;

        ps.EvolveClass(evo.ResultClass);

        AudioManager.Play(SfxId.LevelUp);
        Debug.Log($"[EvolutionManager] 직업 승급 — {evo.EvolutionName} → {evo.ResultClass.ClassName}");
        return true;
    }
}
