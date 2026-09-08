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

    // ── 밖에서 읽는 상태 (D31) ───────────────────────────────────
    //
    // 보스 HP 바와 BossBrain 이 본다. 🔴 읽기 전용으로만 연다 —
    // 밖에서 HP 를 쓰면 TakeDamage 의 방어·사망 처리를 통째로 건너뛰게 된다.

    public float HpNow      => CurrentHp;
    public float HpMax      => MaxHp;
    public float HpFraction => MaxHp > 0f ? Mathf.Clamp01(CurrentHp / MaxHp) : 0f;
    /// <summary>지금 이 적의 접촉 피해. 등급 배율과 층 배율이 이미 곱해진 값이다.</summary>
    public float ContactDamageNow => ContactDamage;
    public bool  Dead       => IsDead;

    /// <summary>보스 이름표에 쓴다. <c>EnemyData</c> 를 통째로 넘기지 않으려는 것이다.</summary>
    public string DisplayName => Data != null ? Data.EnemyName : name;

    // ── 페이즈 속도 배수 (D31) ───────────────────────────────────
    //
    // 슬로우와 곱해서 쓴다. 슬로우는 "일시적으로 느려짐", 이건 "이 페이즈 동안 빨라짐"이라
    // 성격이 다르므로 한 변수에 합치지 않는다 (D25 의 교훈 181 과 같은 이유다).
    private float _phaseSpeedMult = 1f;

    /// <summary>보스 페이즈에 따른 영구 속도 배수. 1 이 기본이다.</summary>
    public void SetSpeedMultiplier(float mult) => _phaseSpeedMult = Mathf.Max(0.05f, mult);

    /// <summary>
    /// 외부가 이동을 가져갔는가 (D73). 켜져 있는 동안 <see cref="FixedUpdate"/> 의 AI 가 안 돈다.
    ///
    /// <para>🔴 <b>풀에서 재사용되므로 <see cref="Initialize"/> 에서 반드시 끈다.</b>
    /// 돌진 도중에 죽은 보스가 풀로 돌아갔다가 잡몹으로 나오면 그 잡몹이 안 움직인다.</para>
    /// </summary>
    public bool AiSuspended { get; set; }

    /// <summary>AI 를 재운 쪽이 속도를 직접 준다 (D73). <c>Rb</c> 를 공개하지 않으려고 둔 창구다.</summary>
    public void DriveVelocity(Vector2 v)
    {
        if (Rb != null) Rb.linearVelocity = v;
    }

    // 🔴 컴파일 반영 확인용 (D27). Assets/Refresh 는 재컴파일을 보장하지 않는다.
    public const int Version = 6;   // 2 = 행동 3종 (D51) · 3 = ApplyStun (D108) · 4 = 미끼 어그로 (D110) · 5 = 아레나 경계 (D111) · 6 = 바리케이드 (D117)

    protected Rigidbody2D Rb;
    protected Transform   PlayerTransform;
    protected EnemyVisual Visual;

    /// <summary>
    /// 플레이어의 <c>Rigidbody2D</c>. <see cref="EnemyAI.Blocker"/> 만 쓴다 —
    /// 플레이어가 <b>어디로 가고 있는지</b>를 알아야 앞을 막을 수 있다.
    /// <para>🔴 미할당 Unity Object 필드가 아니라 <c>GetComponent</c> 결과라
    /// 여기서는 <c>== null</c> 검사가 진짜 null 검사다 (I-24 의 "가짜 null" 이 아니다).</para>
    /// </summary>
    private Rigidbody2D _playerRb;

    // ── 슬로우 ───────────────────────────────────────────────────
    // 독 장판처럼 "매 틱 다시 걸어 주는" 방식이다. 안 걸어 주면 _slowUntil 이 지나
    // 저절로 풀리므로, 적이 장판을 벗어났는지를 아무도 추적하지 않아도 된다.
    private float _slowMult = 1f;   // 1 = 슬로우 없음. 작을수록 느리다
    private float _slowUntil;       // 이 시각을 넘기면 저절로 풀린다

    /// <summary>이동 속도에 슬로우·스턴을 반영한 값. 원본 <see cref="MoveSpeed"/> 는 건드리지 않는다.</summary>
    protected float CurrentSpeed => IsStunned
        ? 0f
        : MoveSpeed * (Time.time <= _slowUntil ? _slowMult : 1f) * _phaseSpeedMult;

    // ── 스턴 ─────────────────────────────────────────────────────

    private float _stunUntil;

    /// <summary>지금 멈춰 있나. 이동이 0 이 되고 원거리 발사도 건너뛴다.</summary>
    public bool IsStunned => Time.time <= _stunUntil;

    /// <summary>
    /// 완전 정지 (D108 · <see cref="SirenBuilding"/>).
    ///
    /// <para>🔴 <b>슬로우와 일부러 갈라 두었다.</b> <c>ApplySlow(0f, …)</c> 로 대신하면
    /// <b>영구 정지 버그가 난다</b> — <see cref="ApplySlow"/> 는 <c>_slowUntil</c> 이 살아 있는 동안
    /// 더 약한 값으로 덮지 못하는데, 냉각탑은 <b>지속시간을 주기보다 길게</b> 걸어
    /// <c>_slowUntil</c> 을 계속 밀어 준다. 그래서 냉각탑 옆에서 한 번 <c>mult = 0</c> 이 잡히면
    /// <b>그 적은 다시는 안 움직인다.</b> (설계 단계에서 값을 따라가 보고 찾았다.)</para>
    ///
    /// <para>🔑 의미상으로도 다르다 — 슬로우는 <b>느려지는 것</b>, 스턴은 <b>아무것도 못 하는 것</b>이다.
    /// 그래서 스턴은 발사까지 막는다.</para>
    ///
    /// <para>겹치면 <b>더 긴 쪽</b>이 남는다. 여러 사이렌이 번갈아 울려도 시간이 누적되지 않는다.</para>
    /// </summary>
    public void ApplyStun(float duration)
    {
        if (duration <= 0f) return;
        _stunUntil = Mathf.Max(_stunUntil, Time.time + duration);
    }

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
        _phaseSpeedMult = 1f;   // 🔴 풀 재사용 — 안 지우면 이전 보스의 페이즈 배수가 잡몹에 남는다
        Rb      = GetComponent<Rigidbody2D>();
        Visual  = GetComponent<EnemyVisual>();

        PlayerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;

        // 도감 (D54) — 죽였는지가 아니라 **봤는지**가 기준이다.
        // 🔴 여기서 파일을 쓰지 않는다. Discover 는 목록에만 넣고 저장은 나중에 몰아서 한다 —
        //    적 스폰마다 디스크를 건드리면 웨이브 중에 I/O 가 끼어든다.
        var gm = GameManager.Instance;
        if (gm != null && gm.MetaProgression != null && data != null)   // 🔴 ?. 금지 (I-24)
            gm.MetaProgression.Discover(CodexKind.Enemy, data.name);
        _playerRb       = PlayerTransform != null ? PlayerTransform.GetComponent<Rigidbody2D>() : null;

        float hpMult     = isBoss ? data.BossHpMult     : isElite ? data.EliteHpMult     : 1f;
        float dmgMult    = isBoss ? data.BossDamageMult : isElite ? data.EliteDamageMult  : 1f;
        float speedMult  = isBoss ? data.BossSpeedMult  : isElite ? data.EliteSpeedMult   : 1f;
        int   xpMult     = isBoss ? data.BossXpMult     : isElite ? data.EliteXpMult      : 1;

        // 🔴 층 배율은 등급 배율 "위에" 곱한다 (ROADMAP §3 결정 4 · B안).
        //    이동 속도는 일부러 뺐다 — 적이 플레이어(3.5~4.6)보다 빨라지면
        //    피하는 게 아니라 맞는 게 되고, 그건 난이도가 아니라 조작 불능이다.
        // 🔴 플레이어가 스스로 올린 적 체력도 여기서 곱한다 (D121 · Bounty 패시브).
        //    층 배율 **옆**에 두는 이유는 성격이 같아서다 — 둘 다 "이 판의 적이 얼마나 단단한가"이고,
        //    등급 배율(엘리트·보스) 위에 얹혀야 한다.
        //    🔵 PlayerStats.Current 는 정적 프로퍼티이고 이 함수는 웨이브 중에 불리므로
        //       Awake 순서 함정(I-8/I-38)과 무관하다 — ProjectileBase 가 특전을 읽는 것과 같다.
        MaxHp         = data.MaxHp         * hpMult  * LayerScaling.HpMult * PlayerHpTax;
        CurrentHp     = MaxHp;
        MoveSpeed     = data.MoveSpeed     * speedMult;
        ContactDamage = data.ContactDamage * dmgMult * LayerScaling.DamageMult;
        Armor         = data.Armor;
        XpDrop        = data.XpDrop        * xpMult;
        CurrencyDrop  = data.CurrencyDrop;

        // ── 풀 재사용 대비 초기화 ────────────────────────────────
        // 이전 생애의 넉백/사망 연출 잔재를 지운다. 안 지우면 갓 스폰된 적이
        // 잠깐 못 움직이거나(넉백 타이머) 충돌이 꺼진 채로 살아난다(사망 연출).
        _knockbackTimer = 0f;
        AiSuspended     = false;   // 🔴 풀 재사용 — 돌진 중에 죽었으면 켜진 채로 돌아온다 (D73)
        _slowMult       = 1f;   // 안 지우면 다음 웨이브의 멀쩡한 적이 느린 채로 태어난다
        _slowUntil      = 0f;
        _stunUntil      = 0f;   // 스턴도 같은 이유로 지운다 (D108)
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
        _neighborCount = 0;

        // 🔴 도는 방향을 개체마다 다르게 준다. 전부 같은 쪽으로 돌면
        //    측면 접근이 아니라 "다 같이 시계방향으로 도는 띠"가 된다.
        _flankDir     = Random.value < 0.5f ? -1f : 1f;

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

        // 🔴 <b>적도 아레나 안에 가둔다</b> (D111).
        //    소환 지점은 이미 막았지만 그것만으로는 안 된다 —
        //    ① 무리 분리와 콜라이더 밀림이 벽 밖으로 밀어낸다(실측 0.04 유닛)
        //    ② 🔴 <b>Ranged 는 PreferredRange 를 지키려고 뒤로 물러난다</b> — 플레이어가 벽에 붙어 있으면
        //       <b>벽 밖으로 후퇴해서 닿지 않는 곳에서 쏜다.</b> 이쪽이 진짜 문제다.
        //    🔑 <b>early return 보다 위에 둔다</b> — 넉백 중에도(오히려 그때가 제일 많이 밀린다) 걸려야 한다.
        if (ArenaBounds.Enabled)
        {
            Vector2 here = Rb.position;
            Vector2 inside = ArenaBounds.Clamp(here);
            if (inside != here) Rb.position = inside;
        }

        // 🔴 <b>바리케이드는 물리가 아니라 여기서 막는다</b> (D117).
        //    이 프로젝트의 적 콜라이더는 isTrigger 라 **겹침을 알려 줄 뿐 밀어내지 않는다** —
        //    실체로 바꾸면 적이 플레이어·다른 건물·서로와 전부 부딪히기 시작해 이동이 통째로 달라진다.
        //    🔑 위의 ArenaBounds 와 **같은 자리·같은 방식**이다. early return 보다 위에 두는 이유도 같다 —
        //    넉백으로 벽 안에 처박히는 것이 오히려 제일 흔하다.
        if (BarricadeBuilding.AnyStanding)
        {
            Vector2 here = Rb.position;
            var wall = BarricadeBuilding.Blocking(here, blockRadius);
            if (wall != null)
            {
                Rb.position = wall.PushOut(here, blockRadius);
                wall.Grind(this);          // 밀고 있는 동안 계속 아프다 (그리고 벽도 닳는다)
            }
        }

        // 넉백 중에는 추적을 멈춘다.
        // ⚠️ MoveTowardsPlayer 가 linearVelocity 를 통째로 덮어쓰기 때문에,
        //    이 return 이 없으면 넉백 속도가 다음 물리 프레임에 즉시 지워진다.
        if (_knockbackTimer > 0f)
        {
            _knockbackTimer -= Time.fixedDeltaTime;
            return;
        }

        // 🔴 외부가 이동을 가져간 동안에는 AI 를 재운다 (D73 · BossBrain 돌진).
        //    넉백과 같은 이유로 **return 이 필요하다** — 아래 Tick 들이 linearVelocity 를
        //    통째로 덮어써서, 이 줄이 없으면 돌진 속도가 다음 물리 프레임에 지워진다.
        if (AiSuspended) return;

        switch (Data.AI)
        {
            case EnemyAI.Ranged:  TickRanged();  break;
            case EnemyAI.Charger: TickCharger(); break;
            case EnemyAI.Flanker: TickFlanker(); break;
            case EnemyAI.Swarmer: TickSwarmer(); break;
            case EnemyAI.Blocker: TickBlocker(); break;
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

    /// <summary>
    /// 지금 쫓아야 할 지점 (D110).
    ///
    /// <para>🔑 <b>이름은 <c>ToPlayer</c> 지만 늘 플레이어는 아니다.</b> 반경 안에 살아 있는
    /// <see cref="DecoyBuilding"/>(미끼)이 있으면 그쪽이다. 이름을 안 바꾼 이유는
    /// <b>호출부가 여섯 군데</b>(Chaser·Ranged·Charger·Flanker·Swarmer·Blocker)이고
    /// 그 전부가 이 한 줄을 거치기 때문이다 — <b>여기만 바꾸면 여섯이 한꺼번에 따라온다.</b></para>
    ///
    /// <para>🔴 <c>AimPointNow</c>(Blocker 의 예측 조준)는 <b>일부러 안 바꿨다.</b>
    /// 그건 "플레이어가 갈 길을 막는다" 는 행동이라 <b>움직이는 대상에만 뜻이 있다.</b>
    /// 미끼는 가만히 있으므로 앞을 막을 것이 없다.</para>
    /// </summary>
    protected Vector2 ToPlayer()
        => DecoyBuilding.ResolveTarget(transform.position, PlayerTransform.position)
         - (Vector2)transform.position;

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

    /// <summary>
    /// 이웃 버퍼 크기. 🔴 이 값이 곧 <b>분리 계산에 반영되는 이웃 수의 상한</b>이다 (B8).
    ///
    /// <para><b>12 → 64 (D27, 2026-09-02).</b> 12 는 "이웃을 몇 마리까지 볼지"를 정한 값이 아니라
    /// <b>배열을 몇 칸으로 할지</b>를 정한 값이 우연히 판정 규칙이 된 것이었다.
    /// 적 800 기준 질의의 <b>71 %</b> 가 이 상한에 걸려 이웃을 잘라 먹고 있었다
    /// (실측 → <c>Docs/PERF.md</c> §7-C).</para>
    ///
    /// <para><b>64 로 정한 근거는 실측이다</b> (짝 대조군, `Docs/PERF.md` §8):</para>
    /// <list type="bullet">
    /// <item>적 <b>130</b>(`Waves.csv` 의 실제 `MaxAlive` 상한): 절단 <b>0.9 % → 0 %</b>,
    ///       분리 비용 1.2~1.3 % → 1.0~1.1 % (차이 없음)</item>
    /// <item>적 <b>400</b>(상한의 3배): 절단 <b>21 % → 0 %</b>,
    ///       분리 비용 7.3~7.4 % → 6.6~7.0 % (차이 없음)</item>
    /// <item>적 <b>800</b>(게임에 없는 조건): 절단 71 % → 1.8 % 이지만
    ///       분리 비용 26~28 % → <b>39~43 %</b> 로 확실히 비싸진다</item>
    /// </list>
    ///
    /// <para>🔴 <b>측정 전에 못박은 성공 조건("적 800에서 프레임 +5 % 이내")은 실패했다.</b>
    /// 그래도 64 를 택한 것은 <b>판단</b>이지 기준 통과가 아니다 — 근거는 800 이 게임에 존재하지
    /// 않는 조건이고, 실제로 나오는 모든 구간에서는 비용 차이가 측정 노이즈 안이기 때문이다.</para>
    ///
    /// <para>⚠️ <c>MaxAlive</c> 를 <b>400 이상</b>으로 올리면 이 값을 다시 판단해야 한다.</para>
    /// </summary>
    public  const  int NeighborBufSize = 64;

    private static readonly Collider2D[] NeighborBuf = new Collider2D[NeighborBufSize];
    private static ContactFilter2D _enemyFilter;
    private static bool            _enemyFilterReady;

    private Vector2 _separation;
    private int     _sepCountdown;

    /// <summary>
    /// 마지막으로 잰 이웃 수(자신 제외). <see cref="EnemyAI.Swarmer"/> 가 쓴다.
    ///
    /// <para>🔑 <b>새 질의를 추가하지 않았다.</b> 분리 조향이 이미 4스텝마다
    /// <c>OverlapCircle</c> 을 돌고 있으므로 그 결과를 세기만 한다.
    /// 적 800 기준 이 질의가 프레임의 19 % 였다(<c>PERF.md</c> §7) — 하나 더 놓을 자리가 없다.</para>
    ///
    /// <para>⚠️ 상한은 <see cref="NeighborBufSize"/>(64) 다. 그보다 빽빽해도 64 로 보인다.
    /// <c>SwarmFullCount</c> 가 6 이라 실용상 문제가 없다.</para>
    /// </summary>
    private int _neighborCount;

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
        if (--_sepCountdown > 0) return _separation;   // _neighborCount 도 같이 재사용된다
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

        Vector2 sum   = Vector2.zero;
        int     others = 0;
        for (int i = 0; i < n; i++)
        {
            var c = NeighborBuf[i];
            if (c == null || c.transform == transform) continue;
            others++;

            Vector2 away = pos - (Vector2)c.transform.position;
            float   d    = away.magnitude;

            // 완전히 겹친 경우엔 방향이 없다. 아무 쪽으로나 흩어뜨린다.
            if (d < 0.0001f) { away = Random.insideUnitCircle.normalized; d = 0.01f; }

            sum += away / (d * d);   // 가까울수록 강하게
        }

        _neighborCount = others;
        _separation    = Vector2.ClampMagnitude(sum, 1f);
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
        // 🔴 멈춰 있으면 쏘지도 않는다 (D108). 이동만 막으면 "멈췄는데 총알은 날아온다" 가 된다.
        if (IsStunned) return;
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

    // ── 측면 접근 (AI = Flanker) ─────────────────────────────────
    //
    // 문제: Chaser 는 전원이 플레이어를 향해 같은 직선으로 온다. 그래서 무기 앞에
    // 줄을 서고, 플레이어는 한 방향만 보면 된다. 분리 조향이 겹침은 풀어 주지만
    // **오는 방향**은 여전히 하나다.
    //
    // 대신 가려는 방향에 **접선 성분**을 섞는다. 멀 때는 옆으로 크게 돌고,
    // 가까워질수록 그 성분이 줄어 결국 직진으로 수렴한다.
    // 🔴 수렴이 없으면 영원히 원만 그린다 — 그건 적이 아니라 장식이다.

    private float _flankDir;   // +1 / -1. 개체마다 도는 쪽을 다르게

    protected virtual void TickFlanker()
    {
        Vector2 toPlayer = ToPlayer();
        float   dist     = toPlayer.magnitude;
        if (dist < 0.0001f) { MoveTowardsPlayer(); return; }

        Vector2 straight = toPlayer / dist;

        // 가까울수록 0 에 수렴한다. FlankCloseRange 안에서는 완전히 직진.
        float close = Mathf.Max(0.01f, Data.FlankCloseRange);
        float arc   = Mathf.Clamp01(Data.FlankArcWeight) * Mathf.Clamp01((dist - close) / close);

        Vector2 tangent = Vector2.Perpendicular(straight) * _flankDir;
        Vector2 desired = (straight + tangent * arc).normalized;

        Rb.linearVelocity = Steer(desired) * CurrentSpeed;
    }

    // ── 무리 가속 (AI = Swarmer) ─────────────────────────────────
    //
    // 혼자 오는 슬라임은 아무 일도 아니고, 서른 마리가 같은 속도로 오는 것도
    // 결국 "한 마리 x 30" 이다. 이웃 수를 속도로 바꾸면 **뭉치는 것 자체가 위협**이 된다.
    // 플레이어에게 생기는 선택: 무리를 갈라 놓을 것인가, 통째로 지울 것인가.

    protected virtual void TickSwarmer()
    {
        // 🔴 Steer 를 먼저 부른다 — 이웃 수는 그 안에서 갱신된다.
        //    순서를 바꾸면 스폰 직후 첫 4스텝 동안 항상 "혼자"로 읽힌다.
        Vector2 dir = Steer(ToPlayer().normalized);

        int   full = Mathf.Max(1, Data.SwarmFullCount);
        float t    = Mathf.Clamp01((float)_neighborCount / full);
        float mult = Mathf.Lerp(Data.SwarmSoloMult, Data.SwarmPackMult, t);

        Rb.linearVelocity = dir * (CurrentSpeed * Mathf.Max(0.05f, mult));
    }

    /// <summary>지금 이 적이 세고 있는 이웃 수. 검증용으로만 연다.</summary>
    public int NeighborCountNow => _neighborCount;

    // ── 길목 차단 (AI = Blocker) ─────────────────────────────────
    //
    // 🔑 이 행동은 **느린 적을 위한 것**이다.
    //    Ogre 는 이동 1.2, 플레이어는 3.5~4.6 이다 — 추격이 성립하지 않는다.
    //    쫓아가는 한 Ogre 는 화면에 있어도 없는 것과 같다.
    //    그래서 쫓지 않고 플레이어가 **가려는 곳**으로 질러가 막아선다.
    //    (D48 에서 보스 실루엣을 정한 것과 같은 근거다 — 못 쫓아오면 버티고 서야 한다)
    //
    // ⚠️ 플레이어가 멈춰 있으면 예측 지점 = 현재 위치라 그냥 Chaser 가 된다. 의도한 것이다.

    protected virtual void TickBlocker()
    {
        Vector2 toPlayer = ToPlayer();

        // 코앞에서까지 앞을 재면 플레이어를 지나쳐 헛돈다.
        if (toPlayer.magnitude <= Mathf.Max(0.5f, Data.BlockHoldRange) || _playerRb == null)
        {
            MoveTowardsPlayer();
            return;
        }

        Vector2 lead    = _playerRb.linearVelocity * Mathf.Max(0f, Data.BlockLeadTime);
        Vector2 aim     = (Vector2)PlayerTransform.position + lead;
        Vector2 desired = (aim - (Vector2)transform.position).normalized;

        Rb.linearVelocity = Steer(desired) * CurrentSpeed;
    }

    /// <summary>이 적이 지금 노리는 지점. 검증용으로만 연다.</summary>
    public Vector2 AimPointNow
    {
        get
        {
            if (Data == null || PlayerTransform == null) return Vector2.zero;
            if (Data.AI != EnemyAI.Blocker || _playerRb == null) return PlayerTransform.position;
            return (Vector2)PlayerTransform.position
                 + _playerRb.linearVelocity * Mathf.Max(0f, Data.BlockLeadTime);
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

        // 🔑 raw 가 아니라 방어를 뺀 dmg 를 넘긴다 (D66) — 클리어 화면이 보여 주는 건
        //    "얼마를 쐈나" 가 아니라 "얼마가 들어갔나" 다.
        //    🔴 WaveManager 를 필드에 캐시하지 않는다 (I-8/I-38): 이 스크립트의 Awake 는
        //       GameManager.Start 보다 먼저 돈다.
        if (GameManager.Instance != null && GameManager.Instance.WaveManager != null)
            GameManager.Instance.WaveManager.ReportDamage(dmg);

        // 데미지 팝업 (이벤트로 분리하거나 DamagePopupManager 사용)
        DamagePopupManager.Instance?.Show(transform.position, dmg);

        // 죽는 타격에는 피격음을 내지 않는다 — 사망음과 겹쳐서 뭉개진다.
        if (CurrentHp <= 0) { Die(); return; }

        AudioManager.Play(SfxId.EnemyHit);

        ApplyKnockback(from);
    }

    /// <summary>
    /// <b>피해 없이</b> 밀어낸다 (D75 · 승급 충격파).
    ///
    /// <para>🔴 <c>TakeDamage(0, from)</c> 으로는 안 된다 — 그 안의
    /// <c>Mathf.Max(1, raw - Armor)</c> 가 <b>최소 1 피해를 넣는다.</b>
    /// 승급 연출이 잡몹을 조금씩 죽이면 그건 연출이 아니라 기술이다.</para>
    ///
    /// <para>등급별 저항은 그대로 쓴다 — 보스는 승급으로도 안 밀린다.</para>
    /// </summary>
    public void PushAway(Vector2 from, float force)
    {
        if (IsDead) return;

        float resist = IsBoss  ? CombatFeel.BossKnockbackResist
                     : IsElite ? CombatFeel.EliteKnockbackResist
                               : 1f;
        if (resist <= 0f) return;

        Vector2 dir = (Vector2)transform.position - from;
        if (dir.sqrMagnitude < 0.0001f) dir = Random.insideUnitCircle;
        dir.Normalize();

        Rb.linearVelocity = dir * (force * resist);
        _knockbackTimer   = CombatFeel.EnemyKnockbackTime;
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
        if (IsDead) return;

        if (other.CompareTag("Player"))
        {
            other.GetComponent<PlayerStats>()?.TryTakeHit(ContactDamage, transform.position);
            return;
        }

        // 🔑 미끼는 때려서 부술 수 있다 (D110). 그래야 "끌어당기고 끝" 이 아니라
        //    <b>버텨 주는 시간</b>이 자원이 된다 — 얼마나 오래 붙잡아 두느냐가 값어치다.
        // 🔴 태그가 아니라 컴포넌트로 가른다. 건물 레이어에는 미끼가 아닌 건물도 있고,
        //    그것들은 맞아도 아무 일이 없어야 한다.
        var decoy = other.GetComponent<DecoyBuilding>();
        if (decoy != null) decoy.TakeHit(ContactDamage * Time.fixedDeltaTime * decoyDpsScale);
    }

    /// <summary>
    /// 미끼에 넣는 피해의 배율 (D110).
    ///
    /// <para>🔴 <b>플레이어와 같은 계산을 쓸 수 없다.</b> 플레이어 쪽은 <b>무적 시간</b>이
    /// 연타를 막아서 <c>ContactDamage</c> 를 "한 방"으로 넣는데, 미끼는 무적이 없어서
    /// 같은 식이면 <b>물리 프레임마다 한 방</b>이 들어가 초당 50배가 된다
    /// (예전에 플레이어 쪽에서 실제로 났던 문제다 — 위 주석 참고).</para>
    ///
    /// <para>⇒ 미끼에는 <b>초당 피해</b>로 넣는다. <c>fixedDeltaTime</c> 을 곱해 프레임 수와 무관하게 만들고,
    /// 여기에 배율을 하나 더 둬서 <b>얼마나 오래 버티는지</b>를 이 값 하나로 조절한다.</para>
    /// </summary>
    private const float decoyDpsScale = 1f;

    /// <summary>
    /// 바리케이드 판정에 쓸 적의 반지름 (D117). 콜라이더에서 읽지 않는 이유는
    /// 적마다 크기가 달라도 <b>벽에 박히는 깊이는 같아야</b> 보기 좋기 때문이다.
    /// </summary>
    private const float blockRadius = 0.28f;

    /// <summary>
    /// 플레이어가 <b>스스로 올린</b> 적 체력 배율 (D121 · <see cref="StatBlock.EnemyHpBonus"/>).
    /// 0.5 를 골랐으면 1.5 를 돌려준다. 아무도 안 골랐으면 1 이다.
    ///
    /// <para>🔴 <b>이미 나와 있는 적에게는 소급되지 않는다.</b> 태어날 때 한 번 곱하는 값이라
    /// 패시브를 먹는 순간 화면의 적이 갑자기 단단해지지는 않는다 —
    /// <c>LayerScaling</c> 이 층을 넘을 때만 적용되는 것과 같은 규칙이다.</para>
    /// </summary>
    private static float PlayerHpTax
        => PlayerStats.Current != null
            ? 1f + Mathf.Max(0f, PlayerStats.Current.Final.EnemyHpBonus)
            : 1f;
}
