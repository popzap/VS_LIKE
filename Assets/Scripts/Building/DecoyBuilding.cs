using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// <b>미끼</b> — 주변의 적이 플레이어 대신 <b>이쪽</b>으로 온다. 맞으면 부서지고, 얼마 뒤 다시 선다
/// (D110 · 사용자 요구 "일정 시간마다 젠이 되는 어그로건물 (부셔진 모습 따로 추가)").
///
/// <para>🔑 <b>이 건물은 이 게임의 구조적 문제를 건드린다.</b> 뱀서라이크에서 플레이어의 유일한
/// 방어 동사는 <b>"걸어서 벗어난다"</b> 인데, 건물은 <b>못 걷는 유일한 것</b>이다.
/// 그래서 지어 놓고 3초 뒤에 사거리 밖으로 나가면 그걸로 끝이었다.
/// 미끼는 그 관계를 뒤집는다 — <b>건물이 전장에 갈 필요 없이 전장이 건물에 온다.</b></para>
///
/// <para><b>CSV 필드를 이렇게 읽는다</b> (<c>Buildings.csv</c>):</para>
/// <list type="bullet">
/// <item><c>AttackRange</c> = <b>어그로 반경</b>. 이 안의 적이 플레이어 대신 미끼를 쫓는다</item>
/// <item><c>Output</c> = <b>최대 체력</b></item>
/// <item><c>AttackCooldown</c> = <b>부서진 뒤 다시 서기까지의 시간(초)</b></item>
/// <item><c>Damage</c> = <b>안 쓴다</b> (0)</item>
/// </list>
///
/// <para>🔑 <b>부활 타이머를 새로 만들지 않았다.</b> <see cref="BuildingBase.CooldownActive"/> 가
/// 이미 <i>"false 면 타이머를 얼린다"</i> 는 계약이다(식당이 힐템 회수를 기다릴 때 쓴다).
/// ⇒ <b>부서져 있는 동안만 true</b> 로 두면 <c>AttackCooldown</c> 이 그대로 부활 시간이 되고
/// <see cref="OnCooldownElapsed"/> 가 그대로 부활 시점이 된다.</para>
///
/// <para>🔴 <b>부서진 동안에는 어그로가 풀린다.</b> 안 그러면 적들이 잔해 앞에 모여
/// 아무것도 안 하고 서 있는다 — 플레이어에게는 "적이 사라진" 것처럼 보인다.</para>
/// </summary>
public class DecoyBuilding : BuildingBase
{
    /// <summary>컴파일 반영 확인용 (D27).</summary>
    public new const int Version = 1;

    [Header("부서진 모습")]
    [Tooltip("부서졌을 때 갈아 끼울 그림. 비우면 색만 어두워진다.")]
    [SerializeField] private Sprite brokenSprite;

    [Tooltip("brokenSprite 가 없을 때 쓰는 색. 그림이 오면 무시된다.")]
    [SerializeField] private Color brokenTint = new(0.45f, 0.45f, 0.45f, 1f);

    // ── 살아 있는 미끼 목록 ──────────────────────────────────────
    //
    // 🔴 적이 매 프레임 씬을 훑으면 안 된다 (PERF.md 의 방침). 미끼 쪽이 스스로 등록한다.
    //    부서지면 목록에서 빠지므로 적은 "살아 있는 미끼"만 보게 된다.
    private static readonly List<DecoyBuilding> Active = new();

    private SpriteRenderer _sr;
    private Sprite _intactSprite;
    private Color  _intactColor;
    private float  _hp;
    private bool   _broken;

    /// <summary>지금 부서져 있나. 부서진 동안에는 어그로도 접촉 피해도 없다.</summary>
    public bool IsBroken => _broken;

    /// <summary>남은 체력 (검증용).</summary>
    public float HpNow => _hp;

    /// <summary>최대 체력. <c>Buildings.csv</c> 의 <c>Output</c> 이다.</summary>
    public float HpMax => Data != null ? Mathf.Max(1f, Data.GetOutput(Level)) : 1f;

    protected override void OnInitialized()
    {
        _sr = GetComponent<SpriteRenderer>();
        if (_sr != null) { _intactSprite = _sr.sprite; _intactColor = _sr.color; }
        Repair();
    }

    protected override void OnUpgraded()
    {
        // 레벨이 오르면 체력 상한이 오른다. 이미 부서진 상태를 억지로 되살리진 않는다.
        if (!_broken) _hp = HpMax;
    }

    private void OnEnable()  { if (!_broken && !Active.Contains(this)) Active.Add(this); }
    private void OnDisable() => Active.Remove(this);

    // ── 어그로 ───────────────────────────────────────────────────

    /// <summary>
    /// <paramref name="from"/> 에 있는 적이 지금 향해야 할 지점.
    /// 반경 안에 살아 있는 미끼가 있으면 <b>가장 가까운 미끼</b>, 없으면 <paramref name="fallback"/>(플레이어).
    ///
    /// <para>🔑 <b>미끼의 반경이 기준이지 적의 시야가 아니다.</b> 미끼마다 반경이 다를 수 있으므로
    /// 거리만 비교하면 안 되고 <b>각 미끼의 사거리 안에 들어왔는지</b>를 봐야 한다.</para>
    /// </summary>
    public static Vector2 ResolveTarget(Vector2 from, Vector2 fallback)
    {
        DecoyBuilding best = null;
        float bestSqr = float.MaxValue;

        for (int i = 0; i < Active.Count; i++)
        {
            var d = Active[i];
            if (d == null || d._broken) continue;

            float r = d.Data != null ? d.Data.GetRange(d.Level) : 0f;
            if (r <= 0f) continue;

            float sqr = ((Vector2)d.transform.position - from).sqrMagnitude;
            if (sqr > r * r) continue;              // 이 미끼의 반경 밖이다
            if (sqr < bestSqr) { bestSqr = sqr; best = d; }
        }
        return best != null ? (Vector2)best.transform.position : fallback;
    }

    /// <summary>살아 있는 미끼 수 (검증용).</summary>
    public static int ActiveCount
    {
        get
        {
            int n = 0;
            for (int i = 0; i < Active.Count; i++)
                if (Active[i] != null && !Active[i]._broken) n++;
            return n;
        }
    }

    // ── 피해 · 파괴 · 부활 ───────────────────────────────────────

    /// <summary>적이 때린다. 부서져 있으면 아무 일도 없다.</summary>
    public void TakeHit(float amount)
    {
        if (_broken || amount <= 0f) return;
        _hp -= amount;
        if (_hp <= 0f) Break();
    }

    private void Break()
    {
        _broken = true;
        _hp     = 0f;
        Active.Remove(this);                        // 🔴 어그로를 즉시 푼다

        if (_sr != null)
        {
            if (brokenSprite != null) _sr.sprite = brokenSprite;
            else                      _sr.color  = brokenTint;
        }
        AudioManager.Play(SfxId.EnemyDie);          // 🔵 전용 SFX 가 오면 갈아 끼운다
    }

    private void Repair()
    {
        _broken = false;
        _hp     = HpMax;
        if (!Active.Contains(this)) Active.Add(this);

        if (_sr != null)
        {
            if (_intactSprite != null) _sr.sprite = _intactSprite;
            _sr.color = _intactColor;
        }
    }

    /// <summary>🔑 부서져 있는 동안에만 타이머가 돈다 — 그게 곧 부활 시간이다.</summary>
    protected override bool CooldownActive => _broken;

    protected override void OnCooldownElapsed()
    {
        // base 는 "가장 가까운 적을 때린다" 다. 미끼는 공격하지 않는다.
        if (_broken) Repair();
    }
}
