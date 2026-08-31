using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyBase : MonoBehaviour
{
    // ── 런타임 수치 ──────────────────────────────────────────────
    protected float MaxHp;
    protected float CurrentHp;
    protected float MoveSpeed;
    protected float ContactDamage;
    protected float Armor;
    protected int   XpDrop;
    protected int   CurrencyDrop;

    protected EnemyData    Data;
    protected bool         IsDead;

    // 등급은 밖에서도 읽는다 (WaveManager 가 보스는 재배치 대상에서 뺀다).
    public bool IsElite { get; protected set; }
    public bool IsBoss  { get; protected set; }

    protected Rigidbody2D Rb;
    protected Transform   PlayerTransform;
    protected EnemyVisual Visual;

    // ── 슬로우 ───────────────────────────────────────────────────
    // 독 장판처럼 "매 틱 다시 걸어 주는" 방식이다. 안 걸어 주면 _slowUntil 이 지나
    // 저절로 풀리므로, 적이 장판을 벗어났는지를 아무도 추적하지 않아도 된다.
    private float _slowMult = 1f;   // 1 = 슬로우 없음. 작을수록 느리다
    private float _slowUntil;       // 이 시각을 넘기면 저절로 풀린다

    /// <summary>이동 속도에 슬로우를 반영한 값. 원본 <see cref="MoveSpeed"/> 는 건드리지 않는다.</summary>
    protected float CurrentSpeed => MoveSpeed * (Time.time <= _slowUntil ? _slowMult : 1f);

    /// <param name="mult">속도 배율(0~1).</param>
    /// <param name="duration">이번에 걸어 줄 지속 시간(초).</param>
    public void ApplySlow(float mult, float duration)
    {
        // "가장 센 것 하나만" 적용한다. 곱해 버리면 장판 2개가 겹칠 때
        // 0.6 * 0.6 = 0.36 이 되어 적이 사실상 멈춘다.
        if (Time.time > _slowUntil || mult < _slowMult) _slowMult = mult;
        _slowUntil = Mathf.Max(_slowUntil, Time.time + duration);
    }

    // ── 초기화 ───────────────────────────────────────────────────

    public virtual void Initialize(EnemyData data, bool isElite = false, bool isBoss = false)
    {
        Data    = data;
        IsElite = isElite;
        IsBoss  = isBoss;
        IsDead  = false;
        Rb      = GetComponent<Rigidbody2D>();
        Visual  = GetComponent<EnemyVisual>();

        PlayerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;

        float hpMult     = isBoss ? data.BossHpMult     : isElite ? data.EliteHpMult     : 1f;
        float dmgMult    = isBoss ? data.BossDamageMult : isElite ? data.EliteDamageMult  : 1f;
        float speedMult  = isBoss ? data.BossSpeedMult  : isElite ? data.EliteSpeedMult   : 1f;
        int   xpMult     = isBoss ? data.BossXpMult     : isElite ? data.EliteXpMult      : 1;

        MaxHp         = data.MaxHp         * hpMult;
        CurrentHp     = MaxHp;
        MoveSpeed     = data.MoveSpeed     * speedMult;
        ContactDamage = data.ContactDamage * dmgMult;
        Armor         = data.Armor;
        XpDrop        = data.XpDrop        * xpMult;
        CurrencyDrop  = data.CurrencyDrop;

        // ── 풀 재사용 대비 초기화 ────────────────────────────────
        // 이전 생애의 넉백/사망 연출 잔재를 지운다. 안 지우면 갓 스폰된 적이
        // 잠깐 못 움직이거나(넉백 타이머) 충돌이 꺼진 채로 살아난다(사망 연출).
        _knockbackTimer = 0f;
        _slowMult       = 1f;   // 안 지우면 다음 웨이브의 멀쩡한 적이 느린 채로 태어난다
        _slowUntil      = 0f;
        if (_deathPopRoutine != null) { StopCoroutine(_deathPopRoutine); _deathPopRoutine = null; }
        SetCollidersEnabled(true);

        // ── 행동 상태 초기화 ─────────────────────────────────────
        // 쿨다운에 랜덤 초기값을 주는 게 핵심이다. 0 으로 맞춰 두면 같은 프레임에 스폰된
        // 무리가 전부 같은 순간에 쏘고 같은 순간에 돌진해 "한 몸"처럼 보인다.
        _attackTimer  = Random.Range(0f, data.AttackCooldown);
        _chargeCd     = Random.Range(0f, data.ChargeCooldown);
        _chargeState  = ChargeState.Chase;
        _strafeDir    = Random.value < 0.5f ? -1f : 1f;
        _separation   = Vector2.zero;
        _sepCountdown = Random.Range(1, SeparationEveryNSteps + 1);

        OnInitialized();
    }

    protected virtual void OnInitialized()
    {
        // 적 종류가 프리팹을 공유하므로 외형은 EnemyData 로 매번 덮어쓴다.
        // (덮어쓰지 않으면 풀에서 재사용될 때 이전 적의 외형이 남는다)
        var sr = GetComponent<SpriteRenderer>();
        if (sr)
        {
            if (Data.Sprite != null) sr.sprite = Data.Sprite;

            // 등급은 "외곽선"으로 표시한다. 예전에는 sr.color 에 보라/빨강을 곱했는데,
            // 종별 고유 스프라이트가 생긴 뒤로는 그 곱셈이 원화를 탁하게 만들었다
            // (녹색 좀비 x 보라 = 거의 검정). 외곽선은 그림 자체를 건드리지 않는다.
            sr.color = Data.Tint;
            ApplyRankOutline(sr);
        }

        float scale = Data.SizeScale * (IsBoss ? 2f : IsElite ? 1.3f : 1f);
        transform.localScale = Vector3.one * scale;

        // 바운스 속도는 이동 속도에 맞춘다. 스케일이 아니다 — 큰 보스가 종종거리면 우스꽝스럽다.
        // 걷기 시트도 여기서 넘긴다. 시트가 없는 적은 위에서 넣은 Data.Sprite 한 장으로 버틴다.
        if (Visual != null) Visual.Setup(sr, MoveSpeed, Data.WalkFrames);
    }

    // ── 등급 외곽선 ──────────────────────────────────────────────

    private static readonly Color EliteOutline = new Color(0.75f, 0.35f, 1f);   // 보라
    private static readonly Color BossOutline  = new Color(1f, 0.25f, 0.15f);   // 빨강

    private static MaterialPropertyBlock _mpb;

    private void ApplyRankOutline(SpriteRenderer sr)
    {
        // MaterialPropertyBlock 이라 머티리얼 인스턴스가 복제되지 않는다.
        // (풀에서 수백 마리가 돌아가므로 sr.material 접근은 피한다)
        if (_mpb == null) _mpb = new MaterialPropertyBlock();

        sr.GetPropertyBlock(_mpb);
        // 폭은 "텍셀" 단위다 — D15 가 `_OutlineTexSize` 를 고친 뒤로 **진짜 텍셀 수**가 됐다.
        // 오브젝트 스케일(보스 2배·엘리트 1.3배)과 `Enemies.csv` 의 `SizeScale`(0.85~1.60)이
        // 여기에 같이 곱해지므로, 화면에 몇 px 로 보이는지는 적마다 다르다.
        //
        // ⚠️ 두 자 사이에 낀 값이다 (C21 실측 · ortho 6 / 1080p 기준 1유닛 = 90px):
        //   하한 = 2 화면 px  — 시트가 Point 필터라 그 밑은 선이 끊겨 점선이 된다
        //   상한 = 키의 ~9 % — 슬라임 엘리트는 화면에서 23px 뿐이라 더 굵으면 실루엣을 먹는다
        // 10.5 면 하한 Wolf 2.04px · 상한 Slime 9.2% 로 **양쪽에 딱 걸친다.** 더 못 내리고 못 올린다.
        // 🔴 종별 보정은 하지 않는다 — 두 자가 정반대를 가리켜(Ogre 6.6 vs 16.1) 보정할수록 나빠진다.
        _mpb.SetFloat(OutlineWidthId, IsBoss ? 9f : IsElite ? 10.5f : 0f);
        _mpb.SetColor(OutlineColorId, IsBoss ? BossOutline : EliteOutline);
        _mpb.SetFloat(FlashAmountId, 0f);   // 풀 재사용 시 이전 피격 플래시가 남지 않게
        sr.SetPropertyBlock(_mpb);
    }

    protected static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");
    protected static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
    protected static readonly int FlashAmountId  = Shader.PropertyToID("_FlashAmount");

    // ── 이동 ────────────────────────────────────────────────────

    protected virtual void FixedUpdate()
    {
        if (IsDead || PlayerTransform == null) return;

        // 넉백 중에는 추적을 멈춘다.
        // ⚠️ MoveTowardsPlayer 가 linearVelocity 를 통째로 덮어쓰기 때문에,
        //    이 return 이 없으면 넉백 속도가 다음 물리 프레임에 즉시 지워진다.
        if (_knockbackTimer > 0f)
        {
            _knockbackTimer -= Time.fixedDeltaTime;
            return;
        }

        switch (Data.AI)
        {
            case EnemyAI.Ranged:  TickRanged();  break;
            case EnemyAI.Charger: TickCharger(); break;
            default:              MoveTowardsPlayer(); break;
        }
    }

    /// <summary>
    /// 적을 다른 위치로 옮긴다. <b>죽이는 게 아니라 재활용</b>이다 (WaveManager 가 호출).
    ///
    /// <para>Rigidbody2D 는 <c>transform</c> 만 바꾸면 보간이 이전 위치에서 새 위치까지
    /// 한 줄로 미끄러지듯 그려진다. <c>Rb.position</c> 을 같이 옮겨 그 잔상을 없앤다.</para>
    /// </summary>
    public void Reposition(Vector2 pos)
    {
        if (IsDead) return;

        transform.position = pos;
        Rb.position        = pos;
        Rb.linearVelocity  = Vector2.zero;
        _knockbackTimer    = 0f;
    }

    protected virtual void MoveTowardsPlayer()
    {
        Rb.linearVelocity = Steer(ToPlayer().normalized) * CurrentSpeed;
    }

    protected Vector2 ToPlayer() => (Vector2)PlayerTransform.position - (Vector2)transform.position;

    // ── 무리 분리 (모든 적 공통) ─────────────────────────────────
    //
    // 예전에는 전원이 플레이어를 향해 정확히 같은 방향으로 달려서 좌표가 수렴하고
    // 결국 한 점에 겹쳐 "한 덩어리"가 됐다. 겹친 적은 여러 마리인지 알 수가 없고,
    // 광역기 한 방에 몰살돼 전투가 밋밋해진다.
    //
    // 물리 충돌로 밀어내지 않는 이유: 적 콜라이더는 트리거라 반발이 없고,
    // 트리거를 끄면 접촉 피해 판정(OnTriggerStay2D)이 통째로 바뀐다.
    // 그래서 이동 방향에 "이웃에서 멀어지는 성분"을 섞는 조향으로 푼다.

    private const float SeparationRadius     = 0.85f;
    private const float SeparationWeight     = 0.9f;
    private const int   SeparationEveryNSteps = 4;

    private static readonly Collider2D[] NeighborBuf = new Collider2D[12];
    private static ContactFilter2D _enemyFilter;
    private static bool            _enemyFilterReady;

    private Vector2 _separation;
    private int     _sepCountdown;

    /// <summary>가려는 방향에 이웃 회피를 섞는다.</summary>
    protected Vector2 Steer(Vector2 desired)
    {
        Vector2 sep = GetSeparation();
        if (sep == Vector2.zero) return desired;
        return (desired + sep * SeparationWeight).normalized;
    }

    private Vector2 GetSeparation()
    {
        // 매 물리 프레임 전부 재면 적 100마리 x 50Hz = 초당 5000번 질의다.
        // 개체마다 다른 위상으로 4스텝에 한 번만 다시 재고 그 사이에는 값을 재사용한다.
        if (--_sepCountdown > 0) return _separation;
        _sepCountdown = SeparationEveryNSteps;

        if (!_enemyFilterReady)
        {
            _enemyFilter = new ContactFilter2D { useTriggers = true };
            _enemyFilter.SetLayerMask(LayerMask.GetMask("Enemy"));
            _enemyFilter.useLayerMask = true;
            _enemyFilterReady = true;
        }

        Vector2 pos = transform.position;
        float   r   = SeparationRadius * Mathf.Max(0.5f, transform.localScale.x);

        int n = Physics2D.OverlapCircle(pos, r, _enemyFilter, NeighborBuf);

        Vector2 sum = Vector2.zero;
        for (int i = 0; i < n; i++)
        {
            var c = NeighborBuf[i];
            if (c == null || c.transform == transform) continue;

            Vector2 away = pos - (Vector2)c.transform.position;
            float   d    = away.magnitude;

            // 완전히 겹친 경우엔 방향이 없다. 아무 쪽으로나 흩어뜨린다.
            if (d < 0.0001f) { away = Random.insideUnitCircle.normalized; d = 0.01f; }

            sum += away / (d * d);   // 가까울수록 강하게
        }

        _separation = Vector2.ClampMagnitude(sum, 1f);
        return _separation;
    }

    // ── 원거리 (AI = Ranged) ─────────────────────────────────────

    private float _attackTimer;
    private float _strafeDir;   // +1 / -1. 개체마다 옆걸음 방향을 다르게

    protected virtual void TickRanged()
    {
        Vector2 toPlayer = ToPlayer();
        float   dist     = toPlayer.magnitude;
        float   want     = Mathf.Max(1f, Data.PreferredRange);

        Vector2 desired;
        if      (dist > want)         desired =  toPlayer.normalized;                       // 접근
        else if (dist < want * 0.7f)  desired = -toPlayer.normalized;                       // 후퇴
        else                          desired = Vector2.Perpendicular(toPlayer).normalized  // 옆걸음
                                              * _strafeDir;

        Rb.linearVelocity = Steer(desired) * CurrentSpeed;

        _attackTimer -= Time.fixedDeltaTime;
        if (_attackTimer > 0f || dist > want * 1.2f) return;

        _attackTimer = Mathf.Max(0.2f, Data.AttackCooldown);
        FireProjectile(toPlayer.normalized);
    }

    private void FireProjectile(Vector2 dir)
    {
        if (Data.ProjectilePrefab == null || SharedPool == null) return;

        var go = SharedPool.Get(Data.ProjectilePrefab, transform.position, Quaternion.identity);
        var p  = go.GetComponent<EnemyProjectile>();
        if (p == null) return;

        float dmg = Data.ProjectileDamage > 0f ? Data.ProjectileDamage : ContactDamage;

        // 사거리를 시간으로 환산한다. 넉넉히 1.6배 — 옆걸음 중에 쏜 탄이 도중에 사라지면
        // 플레이어 입장에선 그냥 안 맞은 것처럼 보인다.
        float life = Data.PreferredRange * 1.6f / Mathf.Max(0.1f, Data.ProjectileSpeed);

        p.Initialize(dir, dmg, Data.ProjectileSpeed, life, SharedPool);

        // 탄이 실제로 나간 뒤에만 운다. 위 두 조기 return 은 "쏘려다 못 쏜" 경우다.
        AudioManager.Play(SfxId.EnemyShoot);
    }

    // ── 돌진 (AI = Charger) ──────────────────────────────────────

    private enum ChargeState { Chase, Windup, Dash, Recover }

    private ChargeState _chargeState;
    private float       _chargeTimer;
    private float       _chargeCd;
    private Vector2     _chargeDir;
    private float       _blinkTimer;

    protected virtual void TickCharger()
    {
        float dt = Time.fixedDeltaTime;
        _chargeCd -= dt;

        switch (_chargeState)
        {
            case ChargeState.Chase:
                MoveTowardsPlayer();
                if (_chargeCd <= 0f && ToPlayer().magnitude <= Data.ChargeRange)
                {
                    _chargeState = ChargeState.Windup;
                    _chargeTimer = Data.ChargeWindup;
                    _blinkTimer  = 0f;
                }
                break;

            case ChargeState.Windup:
                // 멈춰서 깜빡인다 = "지금 온다"는 예고. 이게 없으면 그냥 갑자기 빨라지는
                // 적일 뿐이라 피할 방법이 없고, 맞으면 억울하다.
                Rb.linearVelocity = Vector2.zero;
                _chargeDir        = ToPlayer().normalized;   // 마지막 순간까지 조준을 갱신

                _blinkTimer -= dt;
                if (_blinkTimer <= 0f)
                {
                    _blinkTimer = 0.12f;
                    if (Visual != null) Visual.Flash();
                }

                _chargeTimer -= dt;
                if (_chargeTimer <= 0f)
                {
                    _chargeState = ChargeState.Dash;
                    _chargeTimer = Data.ChargeDuration;
                }
                break;

            case ChargeState.Dash:
                // 돌진 중에는 조향하지 않는다. 유도되는 돌진은 피할 수 없다.
                Rb.linearVelocity = _chargeDir * (CurrentSpeed * Data.ChargeSpeedMult);
                _chargeTimer -= dt;
                if (_chargeTimer <= 0f)
                {
                    _chargeState = ChargeState.Recover;
                    _chargeTimer = Data.ChargeRecover;
                }
                break;

            case ChargeState.Recover:
                Rb.linearVelocity = Vector2.zero;   // 경직 — 플레이어의 반격 기회
                _chargeTimer -= dt;
                if (_chargeTimer <= 0f)
                {
                    _chargeState = ChargeState.Chase;
                    _chargeCd    = Data.ChargeCooldown;
                }
                break;
        }
    }

    // ── 전투 ────────────────────────────────────────────────────

    // ── 넉백 ────────────────────────────────────────────────────
    // 수치는 씬의 CombatFeel 컴포넌트가 들고 있다 (원본은 Economy.csv).
    // 🔴 Awake 에서 캐시하지 말 것 — 쓰는 순간 읽는다 (I-8 / I-38).
    private float _knockbackTimer;

    /// <param name="from">피해가 날아온 위치. 넉백 방향을 정한다. 생략하면 넉백 없음.</param>
    public virtual void TakeDamage(float raw, Vector2? from = null)
    {
        if (IsDead) return;
        float dmg = Mathf.Max(1, raw - Armor);
        CurrentHp -= dmg;

        if (Visual != null) Visual.Flash();

        // 데미지 팝업 (이벤트로 분리하거나 DamagePopupManager 사용)
        DamagePopupManager.Instance?.Show(transform.position, dmg);

        // 죽는 타격에는 피격음을 내지 않는다 — 사망음과 겹쳐서 뭉개진다.
        if (CurrentHp <= 0) { Die(); return; }

        AudioManager.Play(SfxId.EnemyHit);

        ApplyKnockback(from);
    }

    private void ApplyKnockback(Vector2? from)
    {
        if (from == null) return;

        // 등급이 높을수록 덜 밀린다. 보스는 아예 안 밀린다 —
        // 밀리는 보스는 위압감이 없고, 벽 없는 아레나에서 무한히 밀려나 도망가 버린다.
        // 잡몹의 1f 는 기준값이라 CSV 로 빼지 않는다 — 다른 저항이 이 값의 비율이다.
        float resist = IsBoss  ? CombatFeel.BossKnockbackResist
                     : IsElite ? CombatFeel.EliteKnockbackResist
                               : 1f;
        if (resist <= 0f) return;

        Vector2 dir = (Vector2)transform.position - from.Value;
        if (dir.sqrMagnitude < 0.0001f) dir = Random.insideUnitCircle;   // 정확히 겹친 경우
        dir.Normalize();

        Rb.linearVelocity = dir * (CombatFeel.EnemyKnockbackForce * resist);
        _knockbackTimer   = CombatFeel.EnemyKnockbackTime;
    }

    protected virtual void Die()
    {
        if (IsDead) return;
        IsDead = true;
        Rb.linearVelocity = Vector2.zero;

        // 경험치 드랍
        ExperienceManager.Instance?.SpawnExpDrop(transform.position, XpDrop);

        // 보물상자 / 픽업 드랍.
        // 상자는 엘리트·보스 확정, 픽업은 잡몹에게만 낮은 확률로 나온다 —
        // 엘리트가 픽업까지 떨구면 상자와 겹쳐 어느 쪽을 먹은 건지 알 수 없다.
        if (IsElite || IsBoss) ExperienceManager.Instance?.SpawnChest(transform.position);
        else                   ExperienceManager.Instance?.RollPickupDrop(transform.position);

        // 재화 드랍 (GrantGold 가 GoldGain 배율을 적용한다)
        if (CurrencyDrop > 0)
            GameManager.Instance?.GrantGold(CurrencyDrop);

        // 킬 카운트
        GameManager.Instance?.WaveManager.OnEnemyKilled(this);

        PlayDeathImpact();

        // 등급 경계를 PlayDeathImpact 와 똑같이 맞춘다 — 흔들림·히트스톱이 걸리는 죽음에만
        // 다른 소리가 나야 연출과 소리가 한 몸으로 움직인다.
        AudioManager.Play(IsElite || IsBoss ? SfxId.EnemyDieElite : SfxId.EnemyDie);

        OnDeath();

        // 사망 연출이 끝나면 코루틴이 ForceDespawn 을 부른다.
        // 연출을 못 돌리는 상황(비활성 상태 등)이면 즉시 반환한다.
        if (isActiveAndEnabled) _deathPopRoutine = StartCoroutine(DeathPopRoutine());
        else                    ForceDespawn();
    }

    // ── 사망 연출 ────────────────────────────────────────────────
    //
    // 파티클 애셋이 아직 없어서 스케일 "팝"으로 대신한다.
    // 살짝 부풀었다가 0 으로 줄어들며 사라진다.
    // EnemyVisual 은 셰이더로 흔들 뿐 localScale 을 건드리지 않으므로 충돌하지 않고,
    // OnInitialized() 가 재사용 때마다 localScale 을 다시 세팅하므로 잔재도 남지 않는다.

    private Coroutine _deathPopRoutine;

    private IEnumerator DeathPopRoutine()
    {
        SetCollidersEnabled(false);   // 시체에 부딪혀 피해를 입지 않게

        Vector3 baseScale = transform.localScale;
        float   t         = 0f;

        // 연출 도중에 수치가 바뀌면 보간이 튀므로 시작할 때 한 번만 읽는다.
        float popTime  = CombatFeel.DeathPopTime;
        float popScale = CombatFeel.DeathPopScale;

        while (t < popTime)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / popTime);
            // 앞 30% 는 popScale 까지 부풀고, 나머지 70% 는 0 으로 수축
            float s = p < 0.3f ? Mathf.Lerp(1f, popScale, p / 0.3f)
                               : Mathf.Lerp(popScale, 0f, (p - 0.3f) / 0.7f);
            transform.localScale = baseScale * s;
            yield return null;
        }

        transform.localScale = baseScale;   // 풀에 돌아가기 전 원복
        _deathPopRoutine     = null;
        ForceDespawn();
    }

    /// <summary>처치 순간의 화면 연출. 엘리트/보스에만 건다.</summary>
    private void PlayDeathImpact()
    {
        // 잡몹은 초당 수십 마리가 죽는다. 매번 흔들거나 멈추면 화면이 계속 덜컹거려
        // 타격감이 아니라 멀미가 된다. 무게가 있는 대상에만 준다.
        if (!IsElite && !IsBoss) return;

        var cam = Camera.main != null ? Camera.main.GetComponent<CameraController>() : null;
        if (cam != null)
            cam.Shake(IsBoss ? CombatFeel.BossShakeMagnitude : CombatFeel.EliteShakeMagnitude,
                      IsBoss ? CombatFeel.BossShakeDuration  : CombatFeel.EliteShakeDuration);

        GameManager.Instance?.DoHitstop(IsBoss ? CombatFeel.BossHitstop : CombatFeel.EliteHitstop);
    }

    private void SetCollidersEnabled(bool on)
    {
        foreach (var col in GetComponents<Collider2D>())
            col.enabled = on;
    }

    protected virtual void OnDeath() { }

    public void ForceDespawn()
    {
        IsDead = true;
        Rb.linearVelocity = Vector2.zero;

        if (SharedPool != null) SharedPool.Return(gameObject);
        else                    gameObject.SetActive(false);
    }

    // ── 오브젝트 풀 ──────────────────────────────────────────────
    //
    // 예전에는 ForceDespawn 마다 FindFirstObjectByType 을 돌렸다. 씬 전체 순회라
    // 적이 초당 수십 마리씩 죽는 이 게임에서는 무시할 수 없는 비용이고,
    // 적탄 발사까지 같은 조회를 하게 되면서 더 나빠졌다. 한 번 찾아 두고 재사용한다.
    //
    // 파괴된 오브젝트는 Unity 가 == null 을 true 로 만들어 주므로
    // 씬을 다시 로드해도 알아서 다시 찾는다. (?. 는 그 판정을 못 한다 — I-24)

    private static ObjectPool _sharedPool;

    protected static ObjectPool SharedPool
    {
        get
        {
            if (_sharedPool == null) _sharedPool = FindFirstObjectByType<ObjectPool>();
            return _sharedPool;
        }
    }

    // ── 접촉 데미지 ──────────────────────────────────────────────

    // 접촉 피해는 "한 방"이다. 예전에는 ContactDamage * fixedDeltaTime 을 매 물리 프레임
    // 넣는 지속 피해였는데, PlayerStats 의 `Mathf.Max(1, ...)` 바닥값 때문에 그 값이 1 로
    // 올라가 적 하나당 초당 50 피해가 됐다. 지금은 무적 시간이 연타를 막는다.
    // Stay 로 둔 이유: 계속 붙어 있으면 무적이 풀리는 즉시 다시 맞아야 한다.

    private void OnTriggerStay2D(Collider2D other)
    {
        if (IsDead || !other.CompareTag("Player")) return;
        other.GetComponent<PlayerStats>()?.TryTakeHit(ContactDamage, transform.position);
    }
}
