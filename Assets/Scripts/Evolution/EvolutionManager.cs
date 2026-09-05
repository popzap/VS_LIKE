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

    [Header("연출")]
    [Tooltip("승급 순간 플레이어 자리에 뜨는 파동(Fx_Promote). "
           + "SceneWiring.csv 의 EvolutionManager,promoteEffect 로 배선한다.")]
    [SerializeField] private GameObject promoteEffect;

    // ── 승급 연출 (B-5 · D75) ───────────────────────────────────
    // 🔴 수치는 Economy.csv 의 EvolutionManager 행이 들고 있다. 여기 기본값은 자리표시다.
    [Header("승급 연출 — 값은 Economy.csv 가 덮는다")]
    [Tooltip("파동을 몇 번 겹쳐 낼지. 1 이면 D41 과 같다. "
           + "사용자 판정이 '약하다' 였고, 한 번짜리로는 레벨업 파동과 구분이 안 됐다.")]
    [SerializeField] private int promotePulseCount = 3;

    [Tooltip("파동 사이 간격(초). 🔴 실시간이다 — 히트스톱으로 게임이 멈춰 있어도 이어져야 한다")]
    [SerializeField] private float promotePulseInterval = 0.13f;

    [Tooltip("파동이 겹칠 때마다 커지는 비율. 1.25 = 두 번째가 1.25배, 세 번째가 1.56배")]
    [SerializeField] private float promotePulseGrowth = 1.25f;

    [Tooltip("승급 순간 게임을 멈추는 시간(초). 🔴 0.2 를 넘기면 '멈췄다' 가 아니라 '끊겼다' 로 읽힌다")]
    [SerializeField] private float promoteHitstop = 0.12f;

    [Tooltip("추가 화면 흔들림 세기 / 시간. 파동 프리팹의 흔들림 위에 얹힌다")]
    [SerializeField] private float promoteShakeMagnitude = 0.5f;
    [SerializeField] private float promoteShakeDuration  = 0.45f;

    [Tooltip("주변 적을 밀어내는 반경(월드 유닛). 0 이면 안 민다")]
    [SerializeField] private float promotePushRadius = 5.5f;

    [Tooltip("밀어내는 세기. 🔴 피해는 0 이다 — 연출이 잡몹을 죽이면 그건 연출이 아니라 기술이다")]
    [SerializeField] private float promotePushForce = 16f;

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

    // ── 도감이 읽는 목록 (D54) ──────────────────────────────────
    //
    // 🔑 도감이 자기 목록을 따로 배선하지 않는 이유다. 여기서 읽으면
    //    **게임에 실제로 들어간 레시피만** 도감에 뜬다 — 애셋 폴더를 훑으면
    //    배선 안 된 시험용 레시피까지 목록에 섞인다.
    public EvolutionData[]      AllEvolutions   => allEvolutions   != null ? allEvolutions   : System.Array.Empty<EvolutionData>();
    public ClassEvolutionData[] ClassEvolutions => classEvolutions != null ? classEvolutions : System.Array.Empty<ClassEvolutionData>();

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

        // 진화 특전 (D68). 🔑 결과 무기를 준 **뒤에** 켠다 — 특전이 스탯 재계산을 부르는데
        //    무기가 아직 없으면 그 프레임의 재계산이 빠진 채로 굳는다.
        if (evo.Perk != EvolutionPerk.None && PlayerStats.Current != null)
            PlayerStats.Current.ApplyEvolutionPerk(evo.Perk);

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
        // E9 — 무기 최종 진화도 같이 풀어 준다. 둘 다 "건물 앞에서 E" 라는 같은 조작이다.
        if (FieldPromotion)
        {
            foreach (var evo in GetReadyEvolutions())
                if (evo.IsFinalEvolution) return evo;
        }

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

        // 배타 — 한 티어에 하나만. T2 를 하나 타면 다른 T2 는 여기서 전부 막힌다.
        // 이래야 직업이 "빌드가 도달하는 지점"이 된다. 다 모을 수 있으면 갈래가
        // 사라지고, 사슬 보너스가 전부 누적되므로 후반이 일방적으로 세진다.
        if (ps.HasTier(evo.ResultClass.Tier)) return false;

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
    /// <summary>
    /// E9 야전 승급이 켜져 있으면 <b>제단 조건을 통째로 건너뛴다</b> (D37).
    ///
    /// <para>승급은 이 게임의 유일한 설계 차별점인데 <b>"건물 앞에서 E"</b> 를 모르면 못 쓴다.
    /// 이 이벤트는 그 조작을 한 번 강제로 알려 주는 장치다 — <b>다음 전투가 끝나면</b> 꺼진다.</para>
    /// </summary>
    private static bool FieldPromotion =>
        EventManager.Instance != null && EventManager.Instance.FieldPromotionActive;

    public ClassEvolutionData FindAltarClassEvolution(Vector2 origin)
    {
        // E9 — 제단이 없어도 준비된 승급 하나를 그대로 내준다.
        if (FieldPromotion)
        {
            var ready = GetReadyClassEvolutions();
            if (ready.Count > 0) return ready[0];
        }

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
        PlayPromoteFx(ps.transform.position);
        Debug.Log($"[EvolutionManager] 직업 승급 — {evo.EvolutionName} → {evo.ResultClass.ClassName}");
        return true;
    }

    /// <summary>
    /// 승급 파동. <b>이 게임에서 가장 큰 성취인데 D41 전까지 연출이 0 이었다</b> —
    /// 보스 등장에는 화면 흔들림이, 엘리트 처치에는 히트스톱이 있는데 승급만 조용했다(C34).
    ///
    /// <para>🔴 <c>?.</c> 를 쓰지 않는다 — 미할당 직렬화 필드는 C# 기준 null 이 아니라
    /// "가짜 null" 이라 <c>?.</c> 가 그냥 통과시키고 <c>UnassignedReferenceException</c> 이
    /// 호출 사슬 밖으로 새어 나간다 (I-24). 여기서 새면 <b>승급 자체가 실패한 것처럼 보인다.</b></para>
    ///
    /// <para>흔들림 세기·파동 크기는 <see cref="PulseFx"/> 프리팹이 들고 있다 → TUNING.md</para>
    /// </summary>
    private void PlayPromoteFx(Vector3 at)
    {
        if (promoteEffect == null) return;

        // 🔑 이 게임에서 **가장 큰 보상**이다. 사용자 판정이 "약하다" 였고,
        //    파동 한 번짜리로는 레벨업 파동과 구분이 안 됐다 (B-5).
        //    새 애셋은 0개다 — 이미 있는 부품(PulseFx · Shake · Hitstop · 넉백)을 겹친다.
        StartCoroutine(PromoteFxRoutine(at));
    }

    /// <summary>
    /// 승급 연출 (D75 · 사용자 요구 B-5).
    ///
    /// <para>네 가지가 <b>같은 순간에</b> 일어난다 — 하나씩은 이미 게임 어딘가에 있던 것들이고,
    /// 겹치는 것 자체가 *"이건 다른 사건이다"* 라는 신호다.</para>
    ///
    /// <list type="number">
    ///   <item><b>히트스톱</b> — 시간이 멎는다. 제일 먼저 온다.</item>
    ///   <item><b>충격파</b> — 주변 적이 밀려난다. <b>피해는 0</b>이다.</item>
    ///   <item><b>화면 흔들림</b> — 파동 프리팹의 흔들림 위에 한 번 더.</item>
    ///   <item><b>파동 3연</b> — 점점 커지며 겹친다.</item>
    /// </list>
    ///
    /// <para>🔴 <c>WaitForSecondsRealtime</c> 을 쓴다. 히트스톱이 <c>timeScale</c> 을 0 으로 만드므로
    /// <c>WaitForSeconds</c> 로 하면 <b>두 번째 파동이 영원히 안 온다.</b></para>
    ///
    /// <para>🔴 승급 직후 레벨업 패널이 뜰 수 있다(무기 만렙 = 레벨업 직후인 경우가 많다).
    /// 그때도 <c>timeScale</c> 이 0 이라 같은 이유로 실시간이어야 한다.</para>
    /// </summary>
    private System.Collections.IEnumerator PromoteFxRoutine(Vector3 at)
    {
        // ① 시간이 멎는다
        if (promoteHitstop > 0f && GameManager.Instance != null)
            GameManager.Instance.DoHitstop(promoteHitstop);

        // ② 충격파 — 주변 적을 민다. 피해 0.
        if (promotePushRadius > 0f && promotePushForce > 0f)
        {
            var hits = Physics2D.OverlapCircleAll(at, promotePushRadius, LayerMask.GetMask("Enemy"));
            foreach (var h in hits)
            {
                var e = h.GetComponent<EnemyBase>();
                if (e != null) e.PushAway(at, promotePushForce);
            }
        }

        // ③ 화면 흔들림 — 프리팹이 내는 것 위에 한 번 더
        var cam = Camera.main != null ? Camera.main.GetComponent<CameraController>() : null;
        if (cam != null && promoteShakeMagnitude > 0f)
            cam.Shake(promoteShakeMagnitude, promoteShakeDuration);

        // ④ 파동 — 🔴 <b>기본이 1개다</b> (D83 · 사용자 판정).
        //
        //    `D75` 는 *"약하다"* 를 **파동 수**로 풀었는데(1 -> 3), 판정이
        //    *"3개의 파동이 동시에 나와 난잡해 큰 파동 하나면 될거 같아"* 였다.
        //    ⇒ **수가 아니라 크기로 푼다.**
        //
        //    🔑 그래서 <see cref="promotePulseGrowth"/> 의 뜻이 바뀌었다 —
        //    예전에는 *두 번째부터* 곱해지는 값이라 1개면 아무 효과가 없었다.
        //    이제 <b>첫 파동부터</b> 곱한다. 값도 1.25 -> 1.6 으로 올렸다.
        //    (2 이상으로 두면 예전 3연 동작이 그대로 돌아온다 — 되돌릴 손잡이를 남겼다)
        int count = Mathf.Max(1, promotePulseCount);
        float scale = Mathf.Max(1f, promotePulseGrowth);
        for (int i = 0; i < count; i++)
        {
            var go = Instantiate(promoteEffect, at, Quaternion.identity);
            if (!Mathf.Approximately(scale, 1f)) go.transform.localScale *= scale;
            scale *= Mathf.Max(1f, promotePulseGrowth);

            if (i < count - 1)
                yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, promotePulseInterval));
        }
    }
}
