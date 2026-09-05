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

    [Header("버프 안전장치")]
    [Tooltip("쿨다운 배율의 하한. 0 이하가 되면 무기가 매 프레임 발사된다.")]
    [SerializeField] private float minAttackSpeed = 0.1f;

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

    /// <summary>
    /// 이 티어의 직업을 이미 가지고 있는가. <b>승급 배타의 판정식</b>이다 —
    /// 한 티어에 하나만 가질 수 있으므로, T2 를 하나 타면 다른 T2 는 전부 막힌다
    /// (<see cref="EvolutionManager.IsClassSatisfied"/>).
    /// </summary>
    public bool HasTier(int tier)
    {
        foreach (var cls in _classChain)
            if (cls != null && cls.Tier == tier) return true;
        return false;
    }

    public float CurrentHp { get; private set; }
    public bool  IsDead    { get; private set; }

    /// <summary>
    /// 지금 아무 피해도 안 받는가. <b>피격 무적과 픽업 무적을 합쳐서</b> 본다 —
    /// <see cref="TryTakeHit"/> 이 물어보는 건 *"맞을 수 있나"* 하나뿐이다.
    /// </summary>
    public bool IsInvincible => _invincibleTimer > 0f || _buffInvincibleTimer > 0f;

    /// <summary>피격 무적(i-frame). 맞을 때마다 <see cref="invincibleTime"/> 로 채워진다.</summary>
    private float _invincibleTimer;

    /// <summary>
    /// 픽업 무적 (D67). 🔴 <b>피격 무적과 타이머를 갈랐다.</b>
    ///
    /// <para>예전에는 하나였다. 그때는 <c>TryTakeHit</c> 이 무적 중에 되돌아가므로
    /// 서로 덮어쓸 일이 없어서 문제가 없었는데, <b>오라를 붙이는 순간 달라졌다</b>(사용자 요구 8) —
    /// 합쳐 두면 <b>적에게 맞을 때마다 0.x초짜리 오라가 번쩍인다.</b>
    /// 그건 *"무적 아이템을 먹었다"* 와 정반대 신호다.</para>
    ///
    /// <para>가르고 나니 예전 주석의 *"짧은 피격 무적이 긴 버프를 잘라내지 않게 긴 쪽을 남긴다"*
    /// 라는 조심이 <b>통째로 필요 없어졌다</b> — 애초에 서로 못 건드린다.</para>
    /// </summary>
    private float _buffInvincibleTimer;

    /// <summary>픽업 무적이 켜져 있는가. 오라가 이걸 본다 (피격 무적은 안 본다).</summary>
    public bool  IsBuffInvincible        => _buffInvincibleTimer > 0f;

    /// <summary>픽업 무적의 남은 초. 만료가 가까우면 오라를 점등시킨다.</summary>
    public float BuffInvincibleRemaining => Mathf.Max(0f, _buffInvincibleTimer);

    // ── 픽업 버프 (시간이 지나면 저절로 꺼진다) ──────────────────
    //
    // 패시브(_activePassives)와 나란히 두지 않은 이유: 패시브는 한 번 얻으면 런이 끝날 때까지
    // 남는 영구값이고, 이쪽은 몇 초 뒤에 사라진다. 같은 목록에 섞으면 만료 처리를 위해
    // 매 프레임 목록을 훑어야 하고 "지금 내 스탯이 왜 이러지"를 추적하기 어려워진다.
    private float _hasteTimer;
    private float _hasteAttackSpeed;

    /// <summary>공속 버프가 켜져 있는가. HUD·VFX 가 물어볼 자리다.</summary>
    public bool IsHasted => _hasteTimer > 0f;

    /// <summary>공속 버프의 남은 초 (D67). 오라 점등 판정에 쓴다.</summary>
    public float HasteRemaining => Mathf.Max(0f, _hasteTimer);

    // ── 이속 버프 (D68 · 사용자 요구 12 "이속증가(추가해)") ──────
    private float _swiftTimer;
    private float _swiftMoveSpeed;

    /// <summary>이속 버프가 켜져 있는가.</summary>
    public bool  IsSwift        => _swiftTimer > 0f;
    /// <summary>이속 버프의 남은 초. 오라 점등 판정에 쓴다.</summary>
    public float SwiftRemaining => Mathf.Max(0f, _swiftTimer);

    // ── 진화 특전 (D68 · 사용자 요구 12) ─────────────────────────
    //
    // 🔑 패시브·직업과 달리 **한 번 켜지면 런이 끝날 때까지 끄지 않는다.**
    //    그래서 RecalculateStats 가 매번 다시 더할 필요가 없는 값(투사체 수·버프 지속)은
    //    프로퍼티로 그냥 내놓고, 스탯에 섞이는 값(골드)만 재계산에 태운다.

    /// <summary>무기 투사체(근접은 연타) 수 배율. 기본 1, <c>DoubleProjectiles</c> 로 2.</summary>
    public int   ProjectileCountMult { get; private set; } = 1;

    /// <summary>필드 드랍 버프의 지속시간 배율. 기본 1, <c>DoubleBuffDuration</c> 로 2.</summary>
    public float BuffDurationMult    { get; private set; } = 1f;

    /// <summary>골드 획득 배율(특전분). <see cref="RecalculateStats"/> 가 <c>Final.GoldGain</c> 에 곱한다.</summary>
    private float _perkGoldMult = 1f;

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
        if (_invincibleTimer     > 0f) _invincibleTimer     -= Time.deltaTime;
        if (_buffInvincibleTimer > 0f) _buffInvincibleTimer -= Time.deltaTime;

        if (_hasteTimer > 0f)
        {
            _hasteTimer -= Time.deltaTime;
            // 만료된 프레임에 한 번만 재계산한다. 매 프레임 돌리면 패시브·직업 사슬을
            // 통째로 다시 더하게 되므로 켜져 있는 동안 내내 비용을 낸다.
            if (_hasteTimer <= 0f) { _hasteTimer = 0f; RecalculateStats(); }
        }

        if (_swiftTimer > 0f)
        {
            _swiftTimer -= Time.deltaTime;
            if (_swiftTimer <= 0f) { _swiftTimer = 0f; _swiftMoveSpeed = 0f; RecalculateStats(); }
        }
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
        DiscoverClass(cls);   // 도감 (D54) — 런 시작 시 고른 직업

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
        DiscoverClass(cls);   // 도감 (D54) — 승급 직업은 시작 선택 목록에 없어 이 경로로만 들어온다
        RecalculateStats();

        CurrentHp = Mathf.Min(Final.MaxHp, CurrentHp + Mathf.Max(0f, Final.MaxHp - before));

        ApplyClassVisual(cls);
    }

    // ── 도감 발견 (D54) ─────────────────────────────────────────
    //
    // 호출부가 둘(시작 적용·승급)이라 한 곳으로 모은다.
    // 🔴 GameManager.Instance.MetaProgression 을 Awake 에서 캐시하지 않는다 (I-8 / I-38) —
    //    그 참조는 GameManager.Start 에서 채워지는데 모든 Awake 가 모든 Start 보다 먼저 돈다.
    private static void DiscoverClass(CharacterClassData cls)
    {
        if (cls == null) return;
        var gm = GameManager.Instance;
        if (gm == null || gm.MetaProgression == null) return;   // 🔴 ?. 금지 (I-24)
        gm.MetaProgression.Discover(CodexKind.Class, cls.name);
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

        // 픽업 버프는 패시브 다음이다 — 임시값이 영구값 위에 얹히는 순서여야
        // 버프가 꺼졌을 때 원래 자리로 정확히 돌아간다.
        if (_hasteTimer > 0f) Final.AttackSpeed += _hasteAttackSpeed;
        if (_swiftTimer > 0f) Final.MoveSpeed   += _swiftMoveSpeed;   // D68

        // 진화 특전 (D68). 🔑 배율이라 마지막에 곱한다 — 먼저 곱하면 뒤에 더해지는
        //    패시브·직업 보너스가 배율을 안 받아 "2배" 가 2배가 아니게 된다.
        Final.GoldGain *= _perkGoldMult;

        // 🔴 쿨다운 배율이 0 이하로 내려가면 WeaponBase 의 _timer 가 0 이하에서 시작해
        // 무기가 매 프레임 발사된다. 지금 최악은 Aegis(-0.05) + AttackSpeed Lv5(-0.30)
        // + 공속 버프(-0.50) = 0.15 라 이 하한에 안 닿지만, 값이 하나만 더 붙으면 닿는다.
        Final.AttackSpeed = Mathf.Max(minAttackSpeed, Final.AttackSpeed);
    }

    // ── 픽업 버프 ────────────────────────────────────────────────

    /// <summary>
    /// 픽업으로 얻는 완전 무적. 🔴 <b>피격 무적과 다른 타이머를 쓴다</b> (D67).
    ///
    /// <para>예전에는 <c>_invincibleTimer</c> 하나를 공유했다. 피해 판정만 놓고 보면
    /// 그래도 맞았지만, <b>화면에 표시를 붙이는 순간 틀린 설계가 된다</b> —
    /// 오라가 피격 i-frame 에도 켜져서 맞을 때마다 번쩍인다 (사용자 요구 8).</para>
    ///
    /// <para>겹쳐 먹으면 <b>긴 쪽을 남긴다.</b> 더하면 무적 픽업 두 개로 판이 끝난다.</para>
    /// </summary>
    public void GrantInvincibility(float seconds)
    {
        _buffInvincibleTimer = Mathf.Max(_buffInvincibleTimer, seconds);
    }

    /// <summary>
    /// 픽업으로 얻는 공격 속도 버프. <paramref name="attackSpeedBonus"/> 는 쿨다운 배율에
    /// <b>더해지는 값</b>이라 음수가 "더 빠름"이다 (-0.5 = 쿨다운 절반).
    ///
    /// <para>겹치면 <b>더 센 것 하나만</b> 남기고 시간만 늘린다. 더하면 두 개만 겹쳐도
    /// 쿨다운이 0 이 되므로, 슬로우 중첩(<c>EnemyBase.ApplySlow</c>)과 같은 규칙을 쓴다.</para>
    /// </summary>
    public void GrantHaste(float seconds, float attackSpeedBonus)
    {
        _hasteAttackSpeed = Mathf.Min(_hasteAttackSpeed, attackSpeedBonus); // 음수라 Min 이 "더 셈"
        _hasteTimer       = Mathf.Max(_hasteTimer, seconds);
        RecalculateStats();
    }

    /// <summary>
    /// 픽업으로 얻는 이동 속도 버프 (D68). <paramref name="moveSpeedBonus"/> 는
    /// <c>MoveSpeed</c> 에 <b>더해지는 값</b>이라 양수가 "더 빠름"이다.
    ///
    /// <para>겹치면 <see cref="GrantHaste"/> 와 같은 규칙 — <b>더 센 것 하나만</b> 남기고
    /// 시간만 늘린다. 더하면 두 개만 겹쳐도 화면 밖으로 나간다.</para>
    /// </summary>
    public void GrantSwift(float seconds, float moveSpeedBonus)
    {
        _swiftMoveSpeed = Mathf.Max(_swiftMoveSpeed, moveSpeedBonus);   // 양수라 Max 가 "더 셈"
        _swiftTimer     = Mathf.Max(_swiftTimer, seconds);
        RecalculateStats();
    }

    /// <summary>
    /// 진화를 끝냈을 때 특전을 켠다 (D68 · <see cref="EvolutionManager"/> 가 부른다).
    ///
    /// <para>🔑 <b>끄는 경로가 없다.</b> 진화는 되돌릴 수 없으므로 런이 끝날 때까지 남는다 —
    /// 씬을 다시 읽으면(<c>GameManager.ReloadScene</c>) 저절로 초기값으로 돌아간다.</para>
    ///
    /// <para>🔴 같은 특전을 두 번 받아도 <b>2배가 3배가 되지 않는다</b> — 대입이지 곱셈이 아니다.
    /// 같은 진화를 두 번 못 하도록 <c>EvolutionManager._completed</c> 가 막고 있지만,
    /// 막는 쪽이 하나뿐이면 언젠가 새 경로가 생겼을 때 조용히 깨진다.</para>
    /// </summary>
    public void ApplyEvolutionPerk(EvolutionPerk perk)
    {
        switch (perk)
        {
            case EvolutionPerk.DoubleProjectiles:  ProjectileCountMult = 2;   break;
            case EvolutionPerk.DoubleBuffDuration: BuffDurationMult    = 2f;  break;
            case EvolutionPerk.DoubleGold:         _perkGoldMult       = 2f;  break;
            default: return;
        }
        Debug.Log($"[PlayerStats] 진화 특전 — {perk}");
        RecalculateStats();
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
