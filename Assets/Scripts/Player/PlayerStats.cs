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

    /// <summary>이번 런의 직업. <see cref="ApplyClass"/> 로 정해진다.</summary>
    public CharacterClassData Class { get; private set; }

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
        Class = cls;
        RecalculateStats();
        CurrentHp = Final.MaxHp;

        if (cls == null) return;

        if (cls.BodySprite != null) _controller?.ApplyBodySprite(cls.BodySprite);

        // 걷기 프레임은 정지 그림 다음에 넘겨야 한다. PlayerVisual 이 첫 프레임으로
        // sr.sprite 를 다시 덮어쓰므로 순서가 뒤바뀌면 정지 그림이 이겨 버린다.
        var visual = GetComponent<PlayerVisual>();
        if (visual != null) visual.SetWalkFrames(cls.WalkFrames);
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

        Class?.ApplyBonus(Final);

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
