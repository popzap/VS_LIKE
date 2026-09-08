using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// <b>바리케이드</b> — 적이 <b>통과하지 못하고</b>, 밀고 있는 동안 <b>계속 피해를 입는다</b>
/// (D117 · 사용자 요구 "적들이 통과 못하고 도트데미지 주는 벽").
///
/// <para>🔴 <b>물리로 막지 않는다 — 막을 수가 없다.</b>
/// 이 프로젝트의 적 콜라이더는 <c>isTrigger = true</c> 다(접촉 피해를 <c>OnTriggerStay2D</c> 로 받는다).
/// 트리거는 겹침을 <b>알려 줄 뿐 밀어내지 않는다</b> — 그래서 적은 지금 <b>모든 건물을 그냥 통과한다.</b>
/// 콜라이더를 실체로 바꾸면 적이 플레이어·다른 건물·서로와 전부 부딪히기 시작해
/// 이동과 접촉 피해가 통째로 달라진다. ⇒ <b>이동 코드에서 막는다</b> —
/// <see cref="ArenaBounds"/> 가 이미 <see cref="EnemyBase.FixedUpdate"/> 에서 쓰는 그 방식이다.</para>
///
/// <para>🔵 <b>플레이어는 통과한다.</b> 요구가 "<b>적들이</b> 통과 못하고" 였고,
/// 막는 코드가 <see cref="EnemyBase"/> 에만 있으므로 <b>공짜로 그렇게 된다.</b>
/// 플레이어까지 막으면 제 벽에 갇히는 사고가 난다 — 지어 주는 자리를 사람이 못 고르기 때문이다.</para>
///
/// <para>🔴 <b>그래서 내구도가 필요하다.</b> 안 그러면 구석에 벽 두 장으로 자신을 봉인하고
/// 도트 피해로 farming 하는 길이 열린다. 미는 적이 많을수록 빨리 부서지므로
/// <b>가두려 할수록 빨리 무너진다</b> — 봉인을 막는 것이 다른 규칙이 아니라 같은 규칙이다.</para>
///
/// <para><b>CSV 필드를 이렇게 읽는다</b> (<c>Buildings.csv</c>):</para>
/// <list type="bullet">
/// <item><c>AttackRange</c> = <b>벽의 반길이</b>(유닛). 두께는 <see cref="halfThickness"/> 고정</item>
/// <item><c>Damage</c> = <b>초당</b> 피해. 🔴 한 방이 아니다 — 밀고 있는 동안 계속 들어간다</item>
/// <item><c>Output</c> = <b>내구도</b>. 적 하나가 1초 미는 데 1 씩 닳는다</item>
/// <item><c>AttackCooldown</c> = <b>부서진 뒤 다시 서기까지의 시간(초)</b></item>
/// </list>
///
/// <para>🔑 <b>부활 타이머를 새로 만들지 않았다</b> — <see cref="DecoyBuilding"/> 과 같은 손잡이다.
/// <see cref="BuildingBase.CooldownActive"/> 를 <b>부서져 있는 동안만 true</b> 로 두면
/// <c>AttackCooldown</c> 이 그대로 부활 시간이 된다.</para>
///
/// <para>🔵 <b>미끼와 무엇이 다른가</b> — 동사가 다르다.
/// 미끼는 <b>끌어당기고</b>(적이 제 발로 온다), 벽은 <b>막는다</b>(적이 안 오는 게 아니라 못 지나간다).
/// 미끼는 어디에 두든 일하고, 벽은 <b>어디에 두느냐가 전부</b>다.</para>
/// </summary>
public class BarricadeBuilding : BuildingBase
{
    /// <summary>컴파일 반영 확인용 (D27).</summary>
    public new const int Version = 1;

    [Header("모양")]
    [Tooltip("벽의 반두께(유닛). 반길이는 Buildings.csv 의 AttackRange 가 정한다.")]
    [SerializeField] private float halfThickness = 0.38f;

    [Tooltip("놓을 때 플레이어가 보던 쪽을 가로막게 세운다. 끄면 항상 가로로 눕는다.")]
    [SerializeField] private bool faceThePlayerHeading = true;

    /// <summary>
    /// 접촉 여유(유닛). <b>밀어내는 거리가 아니라 "닿았다"고 볼 거리</b>다.
    ///
    /// <para>🔴 <b>이게 없으면 도트 피해가 격프레임으로 빠진다.</b>
    /// <see cref="PushOut"/> 은 적을 <b>정확히 경계</b>에 놓는데, 그러면 다음 프레임의
    /// <see cref="Contains"/> 가 <c>&lt;</c> 비교라 <b>false</b> 가 된다 —
    /// 밀고 있는데도 안 아픈 프레임이 생긴다.
    /// 실측에서 3마리가 2.9초를 밀었는데 내구도가 <b>0.32</b> 밖에 안 닳았다(설계는 8.7).</para>
    ///
    /// <para>🔑 물리 엔진이 contact skin 을 쓰는 것과 같은 이유다.
    /// 판정만 넉넉하게 하고 <b>밀어내는 자리는 그대로</b> 둔다 — 그래야 안 떨린다.</para>
    /// </summary>
    private const float contactSkin = 0.06f;

    [Header("부서진 모습")]
    [Tooltip("부서졌을 때 갈아 끼울 그림. 비우면 색만 어두워진다.")]
    [SerializeField] private Sprite brokenSprite;

    [Tooltip("brokenSprite 가 없을 때 쓰는 색. 그림이 오면 무시된다.")]
    [SerializeField] private Color brokenTint = new(0.45f, 0.45f, 0.45f, 1f);

    // ── 살아 있는 벽 목록 ────────────────────────────────────────
    //
    // 🔴 적이 매 물리 프레임 씬을 훑으면 안 된다 (PERF.md 의 방침). 벽 쪽이 스스로 등록한다.
    //    부서지면 목록에서 빠지므로 적은 "서 있는 벽"만 보게 된다.
    private static readonly List<BarricadeBuilding> Active = new();

    private SpriteRenderer _sr;
    private Sprite _intactSprite;
    private Color  _intactColor;
    private Vector3 _intactScale;
    private float  _hp;
    private bool   _broken;

    /// <summary>서 있는 벽이 하나라도 있나. <see cref="EnemyBase"/> 가 매 프레임 묻는 값이라 싸야 한다.</summary>
    public static bool AnyStanding => Active.Count > 0;

    /// <summary>지금 부서져 있나 (검증용).</summary>
    public bool IsBroken => _broken;

    /// <summary>남은 내구도 (검증용).</summary>
    public float HpNow => _hp;

    /// <summary>최대 내구도. <c>Buildings.csv</c> 의 <c>Output</c> 이다.</summary>
    public float HpMax => Data != null ? Mathf.Max(1f, Data.GetOutput(Level)) : 1f;

    /// <summary>벽의 반길이. <c>Buildings.csv</c> 의 <c>AttackRange</c> 다.</summary>
    private float HalfLength => Data != null ? Mathf.Max(0.2f, Data.GetRange(Level)) : 1f;

    // ── 생애 ────────────────────────────────────────────────────

    protected override void OnInitialized()
    {
        if (_sr == null) _sr = GetComponent<SpriteRenderer>();
        if (_sr != null)
        {
            // 🔴 풀에서 재사용되므로 "성한 모습"을 매번 다시 잡지 않는다 — 부서진 그림이 원본이 된다.
            if (_intactSprite == null) { _intactSprite = _sr.sprite; _intactColor = _sr.color; }
            _sr.sprite = _intactSprite;
            _sr.color  = _intactColor;
        }
        if (_intactScale == Vector3.zero) _intactScale = transform.localScale;

        _hp = HpMax;
        _broken = false;
        if (!Active.Contains(this)) Active.Add(this);

        Orient();
        Stretch();
    }

    protected override void OnUpgraded()
    {
        // 레벨이 오르면 길이도 내구도도 는다. 지금 서 있는 벽에 바로 반영한다.
        _hp = Mathf.Min(HpMax, _hp + (HpMax - _hp));   // 보수 = 만복
        Stretch();
    }

    private void OnDisable()
    {
        // 🔴 목록에서 빼지 않으면 적이 "있지도 않은 벽"에 계속 부딪힌다.
        Active.Remove(this);
    }

    /// <summary>
    /// 놓을 때 <b>플레이어가 보던 쪽을 가로막게</b> 세운다 (D113 의 <see cref="PlayerController.LastMoveDir"/> 재사용).
    ///
    /// <para>🔴 <see cref="BuildingManager"/> 는 모든 건물을 <c>Quaternion.identity</c> 로 놓는다.
    /// 다른 건물은 방향이 없어서 상관없지만 벽은 <b>방향이 전부</b>다 —
    /// 가는 길에 가로로 누워야 막지, 세로로 서면 옆으로 지나간다.</para>
    /// </summary>
    private void Orient()
    {
        if (!faceThePlayerHeading) { transform.rotation = Quaternion.identity; return; }

        var pc = PlayerStats.Current != null ? PlayerStats.Current.GetComponent<PlayerController>() : null;
        Vector2 heading = pc != null ? pc.LastMoveDir : Vector2.right;
        if (heading.sqrMagnitude < 1e-6f) heading = Vector2.right;

        // 벽의 길이 방향(로컬 +X)이 진행 방향과 **직각**이어야 앞을 가로막는다.
        float deg = Mathf.Atan2(heading.y, heading.x) * Mathf.Rad2Deg + 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, deg);
    }

    /// <summary>
    /// 그림을 실제 막는 크기에 맞춘다. 🔴 안 하면 <b>그림이 막는 범위를 속인다.</b>
    ///
    /// <para>🔴 <c>sprite.bounds</c> 는 <b>텍스처 전체</b>지 그려진 잉크가 아니다.
    /// 벽 그림은 세로로 26 % 밖에 안 차 있었는데 그대로 쓰면
    /// <b>보이는 벽보다 막는 범위가 네 배</b>가 된다.
    /// ⇒ PNG 자체를 잉크 상자에 맞춰 잘라 두었다(<c>Barricade.png</c> 992x277).
    /// <b>그림을 다시 만들면 반드시 다시 자를 것</b> — 안 자르면 조용히 이 함정으로 돌아온다.</para>
    ///
    /// <para>🔵 부서진 그림은 비율이 달라도 <b>같은 상자에 맞춰 늘인다</b> —
    /// 자리가 그대로여야 "여기 있던 벽이 부서졌다"로 읽힌다.</para>
    /// </summary>
    private void Stretch()
    {
        if (_sr == null || _sr.sprite == null) return;
        var size = _sr.sprite.bounds.size;      // 로컬 스케일 1 일 때의 월드 크기
        if (size.x <= 1e-4f || size.y <= 1e-4f) return;
        transform.localScale = new Vector3(HalfLength * 2f / size.x,
                                           halfThickness * 2f / size.y, 1f);
    }

    // ── 부서짐 / 부활 ────────────────────────────────────────────

    /// <summary>🔴 부서져 있는 동안에만 쿨다운이 흐른다 = <c>AttackCooldown</c> 이 곧 부활 시간이다.</summary>
    protected override bool CooldownActive => _broken;

    protected override void OnCooldownElapsed()
    {
        if (_broken) Rebuild();
    }

    private void Break()
    {
        if (_broken) return;
        _broken = true;
        Active.Remove(this);                    // 🔴 즉시 통과 가능해진다

        if (_sr != null)
        {
            if (brokenSprite != null) { _sr.sprite = brokenSprite; Stretch(); }
            else                        _sr.color  = brokenTint;
        }
        AudioManager.Play(SfxId.EnemyDie);      // 🟡 자리표시 — 부서지는 소리는 아직 없다
    }

    private void Rebuild()
    {
        _broken = false;
        _hp = HpMax;
        if (_sr != null)
        {
            if (_intactSprite != null) _sr.sprite = _intactSprite;
            _sr.color = _intactColor;
        }
        if (!Active.Contains(this)) Active.Add(this);
        Stretch();
        AudioManager.Play(SfxId.BuildingPlace);
    }

    // ── 적이 물어보는 것 ─────────────────────────────────────────

    /// <summary>
    /// <paramref name="p"/> 가 어느 벽 안에 들어와 있나. 없으면 null.
    /// <see cref="EnemyBase.FixedUpdate"/> 가 매 물리 프레임 부른다.
    /// </summary>
    /// <param name="radius">적의 반지름. 벽을 이만큼 부풀려 판정한다(몸이 벽에 박히지 않게).</param>
    public static BarricadeBuilding Blocking(Vector2 p, float radius)
    {
        for (int i = 0; i < Active.Count; i++)
        {
            var w = Active[i];
            if (w == null || w._broken) continue;
            // 🔴 판정은 여유를 두고(표면에 붙어 있어도 "닿았다"), 밀어내기는 여유 없이 한다.
            if (w.Contains(p, radius + contactSkin)) return w;
        }
        return null;
    }

    private bool Contains(Vector2 p, float radius)
    {
        Vector2 local = transform.InverseTransformPoint(p);
        // 🔴 로컬 좌표는 스케일이 이미 반영돼 있다 — 반길이를 스케일로 나눠 맞춘다.
        return Mathf.Abs(local.x) < HalfLocalX + radius / Mathf.Max(1e-4f, transform.lossyScale.x)
            && Mathf.Abs(local.y) < HalfLocalY + radius / Mathf.Max(1e-4f, transform.lossyScale.y);
    }

    private float HalfLocalX => _sr != null && _sr.sprite != null ? _sr.sprite.bounds.extents.x : 0.5f;
    private float HalfLocalY => _sr != null && _sr.sprite != null ? _sr.sprite.bounds.extents.y : 0.5f;

    /// <summary>
    /// 벽 밖으로 밀어낸 자리. <b>가장 가까운 면</b>으로 내보낸다 —
    /// 그래야 벽을 따라 옆으로 미끄러지지, 반대편으로 순간이동하지 않는다.
    /// </summary>
    public Vector2 PushOut(Vector2 p, float radius)
    {
        Vector3 local = transform.InverseTransformPoint(p);
        float sx = Mathf.Max(1e-4f, transform.lossyScale.x);
        float sy = Mathf.Max(1e-4f, transform.lossyScale.y);
        float hx = HalfLocalX + radius / sx;
        float hy = HalfLocalY + radius / sy;

        // 각 면까지 나가야 하는 거리(로컬). 작은 쪽으로 나간다.
        float outX = hx - Mathf.Abs(local.x);
        float outY = hy - Mathf.Abs(local.y);
        // 🔴 로컬 거리를 그대로 비교하면 안 된다 — 스케일이 축마다 다르다(길고 얇다).
        //    월드 거리로 환산해서 비교해야 "가장 가까운 면"이 실제로 가장 가깝다.
        if (outX * sx <= outY * sy) local.x = Mathf.Sign(local.x == 0f ? 1f : local.x) * hx;
        else                        local.y = Mathf.Sign(local.y == 0f ? 1f : local.y) * hy;

        return transform.TransformPoint(local);
    }

    /// <summary>
    /// 벽에 밀고 있는 적을 <b>초당</b> 갉는다. 그리고 벽도 그만큼 닳는다.
    /// <see cref="EnemyBase.FixedUpdate"/> 에서 물리 프레임마다 불린다.
    ///
    /// <para>🔴 한 방으로 주면 안 된다 — 물리 프레임마다 <c>Damage</c> 를 통째로 넣으면
    /// 초당 50배가 된다. <c>fixedDeltaTime</c> 을 곱해 <b>초당 피해</b>로 만든다
    /// (<see cref="DecoyBuilding"/> 이 접촉 피해에 쓴 것과 같은 규칙이다).</para>
    /// </summary>
    public void Grind(EnemyBase enemy)
    {
        if (_broken || enemy == null || enemy.Dead) return;

        float dps = Data != null ? Data.GetDamage(Level) : 0f;
        if (dps > 0f) enemy.TakeDamage(dps * Time.fixedDeltaTime, transform.position);

        // 🔑 미는 적이 많을수록 빨리 닳는다 — 가두려 할수록 빨리 무너진다.
        _hp -= Time.fixedDeltaTime;
        if (_hp <= 0f) Break();
    }
}
