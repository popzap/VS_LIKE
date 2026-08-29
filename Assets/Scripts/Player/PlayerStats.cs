using System.Collections.Generic;
using UnityEngine;

// ────────────────────────────────────────────────────────────────────────────
//  PlayerStats  —  모든 수치 원본 보관 + 패시브 modifier 적용
// ────────────────────────────────────────────────────────────────────────────
public class PlayerStats : MonoBehaviour
{
    /// <summary>현재 씬의 플레이어. 골드/경험치 배율 조회처럼 잦은 참조를 위해 캐싱한다.</summary>
    public static PlayerStats Current { get; private set; }

    [Header("기본 스탯 (SO나 캐릭터 데이터로 주입)")]
    [SerializeField] private StatBlock baseStats = new();

    [Header("피격")]
    [Tooltip("피격 후 무적 시간(초). 접촉 피해가 연타되지 않도록 막는다.")]
    [SerializeField] private float invincibleTime = 0.6f;

    // 런타임 수치 (base + meta + class + passive 합산)
    public StatBlock Final { get; private set; } = new();

    /// <summary>
    /// 이번 런에 거쳐 온 직업 사슬. 0번이 런 시작 직업(T1)이고 뒤로 갈수록 상위 직업이다.
    ///
    /// <para><b>왜 하나가 아니라 사슬인가</b> — 직업 진화는 갈아타는 게 아니라 <b>쌓는</b> 것이다.
    /// 하나만 들고 교체하면 T2 로 올라가는 순간 T1 의 보너스와 소지 칸이 사라져,
    /// 진화가 <b>손해가 되는 경우</b>가 생긴다 (Warrior 의 건물 5칸을 잃는 식).</para>
    /// </summary>
    private readonly List<CharacterClassData> _classChain = new();

    /// <summary>현재(=가장 상위) 직업. 사슬이 비면 null.</summary>
    public CharacterClassData Class => _classChain.Count > 0 ? _classChain[^1] : null;

    /// <summary>런 시작 직업. 초상화·이름 표시가 T1 을 필요로 할 때 쓴다.</summary>
    public CharacterClassData BaseClass => _classChain.Count > 0 ? _classChain[0] : null;

    /// <summary>거쳐 온 직업 전부. 1 이면 아직 진화 전이다.</summary>
    public IReadOnlyList<CharacterClassData> ClassChain => _classChain;

    /// <summary>이 직업을 거쳐 왔는가. 승급 조건 판정이 매 프레임 물어서 LINQ 없이 둔다.</summary>
    public bool HasClass(CharacterClassData cls) => cls != null && _classChain.Contains(cls);

    public float CurrentHp { get; private set; }
    public bool  IsDead    { get; private set; }

    /// <summary>피격 무적 중인가.</summary>
    public bool IsInvincible => _invincibleTimer > 0f;

    private float _invincibleTimer;

    private readonly List<PassiveEffect> _activePassives = new();

    private PlayerController _controller;

    private void Awake()
    {
        Current     = this;
        _controller = GetComponent<PlayerController>();
    }

    private void OnDestroy() { if (Current == this) Current = null; }

    private void Update()
    {
        if (_invincibleTimer > 0f) _invincibleTimer -= Time.deltaTime;
    }

    private void Start()
    {
        RecalculateStats();
        CurrentHp = Final.MaxHp;
    }

    // ── 직업 ─────────────────────────────────────────────────────

    /// <summary>
    /// 런 시작 시 <see cref="GameManager.StartRun"/> 이 호출한다.
    /// 스탯 보너스를 반영하고 체력을 가득 채운다. 시작 무기 지급은 GameManager 가 맡는다.
    /// </summary>
    public void ApplyClass(CharacterClassData cls)
    {
        _classChain.Clear();
        if (cls != null) _classChain.Add(cls);

        RecalculateStats();
        CurrentHp = Final.MaxHp;

        ApplyClassVisual(cls);
    }

    /// <summary>
    /// 직업 진화 — 상위 직업을 사슬 <b>끝에 덧붙인다</b>. 교체가 아니다.
    /// <see cref="EvolutionManager.EvolveClass"/> 가 호출한다.
    ///
    /// <para>체력은 <b>가득 채우지 않는다.</b> 진화는 필드에서 전투 중에 일어나므로
    /// 완전 회복이 붙으면 "위험할 때 진화를 아껴 두는" 이상한 운용이 생긴다.
    /// 대신 늘어난 최대치만큼은 그대로 얹어 준다 — 안 그러면 최대 체력이 올라도
    /// 현재 체력은 그대로라 진화가 눈에 띄지 않는다.</para>
    /// </summary>
    public void EvolveClass(CharacterClassData cls)
    {
        if (cls == null || _classChain.Contains(cls)) return;

        float before = Final.MaxHp;
        _classChain.Add(cls);
        RecalculateStats();

        CurrentHp = Mathf.Min(Final.MaxHp, CurrentHp + Mathf.Max(0f, Final.MaxHp - before));

        ApplyClassVisual(cls);
    }

    /// <summary>
    /// 직업 외형 반영. 진화 직업이 그림을 안 들고 있으면 <b>이전 모습을 그대로 둔다</b> —
    /// 빈 값으로 덮으면 플레이어가 프리팹 기본 스프라이트로 되돌아간다.
    /// </summary>
    private void ApplyClassVisual(CharacterClassData cls)
    {
        if (cls == null) return;

        if (cls.BodySprite != null) _controller?.ApplyBodySprite(cls.BodySprite);

        // 걷기 프레임은 정지 그림 다음에 넘겨야 한다. PlayerVisual 이 첫 프레임으로
        // sr.sprite 를 다시 덮어쓰므로 순서가 뒤바뀌면 정지 그림이 이겨 버린다.
        if (cls.WalkFrames == null || cls.WalkFrames.Length == 0) return;

        var visual = GetComponent<PlayerVisual>();
        if (visual != null) visual.SetWalkFrames(cls.WalkFrames);
    }

    /// <summary>
    /// 동시에 지닐 수 있는 <b>아이템 종류 수</b>. 레벨이 아니라 종류를 센다.
    /// 직업 사슬 전체의 <see cref="CharacterClassData.BonusSlots"/> 합이다.
    ///
    /// <para>직업이 없으면 제한하지 않는다. 실제 런은 <see cref="GameManager.StartRun"/> 에서
    /// 반드시 직업을 받으므로 이 경로는 직업 없이 씬을 직접 재생할 때만 탄다 —
    /// 거기서 상한을 걸면 원인 모를 "카드가 안 뜬다"가 된다.</para>
    /// </summary>
    public int SlotLimit(ItemCategory category)
    {
        if (_classChain.Count == 0) return int.MaxValue;

        int n = 0;
        foreach (var cls in _classChain)
            if (cls != null) n += cls.BonusSlots(category);

        return n;
    }

    // ── Passive 등록 / 해제 ───────────────────────────────────────

    /// <summary>
    /// 패시브를 등록하거나, 이미 있으면 레벨만 올린다.
    ///
    /// <para><b>패시브를 추가하는 유일한 경로다.</b> 레벨업마다 새 <see cref="PassiveEffect"/> 를
    /// 리스트에 밀어 넣으면 Lv1~Lv5 값이 전부 합산된다. <c>Passives.csv</c> 의 값은 누적이 아니라
    /// "그 레벨일 때의 총 보너스"라서, 누적되면 배수형 스탯
    /// (<c>BuildingCooldown</c>) 이 음수로 뒤집힌다 (I-32).</para>
    /// </summary>
    public void AddOrUpgradePassive(PassiveData data, int level)
    {
        if (data == null) return;

        var existing = _activePassives.Find(e => e.Data == data);
        if (existing != null) existing.UpgradeTo(level);
        else                  _activePassives.Add(new PassiveEffect(data, level));

        RecalculateStats();
    }

    public void RemovePassive(PassiveEffect passive)
    {
        _activePassives.Remove(passive);
        RecalculateStats();
    }

    /// <summary>
    /// 상점에서 패시브 아이템 제거 시 호출.
    /// PassiveData로 일치하는 PassiveEffect를 찾아 제거한다.
    /// </summary>
    public void RemovePassiveByData(PassiveData data)
    {
        var effect = _activePassives.Find(e => e.Data == data);
        if (effect != null) RemovePassive(effect);
    }

    // ── 수치 재계산 ──────────────────────────────────────────────

    public void RecalculateStats()
    {
        // meta 는 "보너스"라 0 에서 시작한다 (StatBlock.Zero). 기본 생성자를 쓰면 기본 스탯이
        // 한 번 더 더해져 전 스탯이 2배가 된다.
        var meta = GameManager.Instance?.MetaProgression?.GetStatBonus() ?? StatBlock.Zero();

        Final = new StatBlock
        {
            MaxHp          = baseStats.MaxHp          + meta.MaxHp,
            MoveSpeed      = baseStats.MoveSpeed       + meta.MoveSpeed,
            Damage         = baseStats.Damage          + meta.Damage,
            AttackSpeed    = baseStats.AttackSpeed     + meta.AttackSpeed,
            ProjectileSize = baseStats.ProjectileSize  + meta.ProjectileSize,
            PickupRadius   = baseStats.PickupRadius    + meta.PickupRadius,
            CritChance     = baseStats.CritChance      + meta.CritChance,
            CritMultiplier = baseStats.CritMultiplier  + meta.CritMultiplier,
            Armor          = baseStats.Armor           + meta.Armor,
            XpGain         = baseStats.XpGain          + meta.XpGain,
            GoldGain       = baseStats.GoldGain        + meta.GoldGain,
            BuildingCooldown = baseStats.BuildingCooldown + meta.BuildingCooldown,
        };

        // 사슬 전체를 더한다. 진화는 갈아타기가 아니라 쌓기이므로 T1 의 보너스도 계속 살아 있다.
        foreach (var cls in _classChain)
            if (cls != null) cls.ApplyBonus(Final);

        foreach (var p in _activePassives)
            p.Apply(Final);
    }

    // ── 피해 / 회복 ──────────────────────────────────────────────

    /// <summary>
    /// 접촉·투사체 같은 "한 방" 피해. 무적 중이면 무시하고 <c>false</c> 를 반환한다.
    /// 피해가 들어가면 무적 시간이 시작되고 넉백·피격 연출이 재생된다.
    /// </summary>
    /// <param name="sourcePos">가해자 위치. 여기서 멀어지는 방향으로 밀려난다.</param>
    public bool TryTakeHit(float raw, Vector2 sourcePos)
    {
        if (IsDead || IsInvincible) return false;

        _invincibleTimer = invincibleTime;

        Vector2 dir = (Vector2)transform.position - sourcePos;
        // 완전히 겹쳐 있으면 방향이 0 이라 밀려나지 않는다 → 아무 방향으로라도 빼낸다
        dir = dir.sqrMagnitude < 0.0001f ? Random.insideUnitCircle.normalized : dir.normalized;

        TakeDamage(raw);
        if (!IsDead) _controller?.PlayHitFeedback(dir, invincibleTime);
        return true;
    }

    public void TakeDamage(float raw)
    {
        if (IsDead) return;
        float dmg = Mathf.Max(1, raw - Final.Armor);
        CurrentHp -= dmg;
        DamagePopupManager.Instance?.Show(transform.position, dmg);

        // 적 쪽과 같은 이유로, 죽는 타격에는 피격음을 내지 않는다.
        if (CurrentHp <= 0) { Die(); return; }

        AudioManager.Play(SfxId.PlayerHit);
    }

    public void Heal(float amount)
    {
        CurrentHp = Mathf.Min(Final.MaxHp, CurrentHp + amount);
    }

    private void Die()
    {
        IsDead = true;
        AudioManager.Play(SfxId.PlayerDie);
        GameManager.Instance.OnPlayerDied();
    }
}
