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
    protected bool         IsElite;
    protected bool         IsBoss;
    protected bool         IsDead;

    protected Rigidbody2D Rb;
    protected Transform   PlayerTransform;
    protected EnemyVisual Visual;

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
        if (_deathPopRoutine != null) { StopCoroutine(_deathPopRoutine); _deathPopRoutine = null; }
        SetCollidersEnabled(true);

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
        if (Visual != null) Visual.Setup(sr, MoveSpeed);
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
        // 폭은 "텍스처 픽셀" 단위라 오브젝트 스케일에 같이 곱해진다.
        // 보스는 2배, 엘리트는 1.3배로 커지므로 숫자를 그만큼 낮춰야 화면상 두께가 비슷해진다.
        _mpb.SetFloat(OutlineWidthId, IsBoss ? 12f : IsElite ? 14f : 0f);
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

        MoveTowardsPlayer();
    }

    protected virtual void MoveTowardsPlayer()
    {
        Vector2 dir = ((Vector2)(PlayerTransform.position - transform.position)).normalized;
        Rb.linearVelocity = dir * MoveSpeed;
    }

    // ── 전투 ────────────────────────────────────────────────────

    // ── 넉백 ────────────────────────────────────────────────────
    private const float KnockbackForce = 6f;
    private const float KnockbackTime  = 0.10f;
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

        if (CurrentHp <= 0) { Die(); return; }

        ApplyKnockback(from);
    }

    private void ApplyKnockback(Vector2? from)
    {
        if (from == null) return;

        // 등급이 높을수록 덜 밀린다. 보스는 아예 안 밀린다 —
        // 밀리는 보스는 위압감이 없고, 벽 없는 아레나에서 무한히 밀려나 도망가 버린다.
        float resist = IsBoss ? 0f : IsElite ? 0.4f : 1f;
        if (resist <= 0f) return;

        Vector2 dir = (Vector2)transform.position - from.Value;
        if (dir.sqrMagnitude < 0.0001f) dir = Random.insideUnitCircle;   // 정확히 겹친 경우
        dir.Normalize();

        Rb.linearVelocity = dir * (KnockbackForce * resist);
        _knockbackTimer   = KnockbackTime;
    }

    protected virtual void Die()
    {
        if (IsDead) return;
        IsDead = true;
        Rb.linearVelocity = Vector2.zero;

        // 경험치 드랍
        ExperienceManager.Instance?.SpawnExpDrop(transform.position, XpDrop);

        // 재화 드랍 (GrantGold 가 GoldGain 배율을 적용한다)
        if (CurrencyDrop > 0)
            GameManager.Instance?.GrantGold(CurrencyDrop);

        // 킬 카운트
        GameManager.Instance?.WaveManager.OnEnemyKilled(this);

        PlayDeathImpact();

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

    private const float DeathPopTime = 0.14f;
    private Coroutine _deathPopRoutine;

    private IEnumerator DeathPopRoutine()
    {
        SetCollidersEnabled(false);   // 시체에 부딪혀 피해를 입지 않게

        Vector3 baseScale = transform.localScale;
        float   t         = 0f;

        while (t < DeathPopTime)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / DeathPopTime);
            // 앞 30% 는 1.25배까지 부풀고, 나머지 70% 는 0 으로 수축
            float s = p < 0.3f ? Mathf.Lerp(1f, 1.25f, p / 0.3f)
                               : Mathf.Lerp(1.25f, 0f, (p - 0.3f) / 0.7f);
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
        if (cam != null) cam.Shake(IsBoss ? 0.45f : 0.20f, IsBoss ? 0.5f : 0.25f);

        GameManager.Instance?.DoHitstop(IsBoss ? 0.09f : 0.05f);
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
        // 오브젝트 풀로 반환
        var pool = FindFirstObjectByType<ObjectPool>();
        if (pool != null) pool.Return(gameObject);
        else gameObject.SetActive(false);
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
