using System.Collections;
using UnityEngine;

/// <summary>
/// 보스를 <b>큰 잡몹에서 벗어나게</b> 하는 뇌 (D31).
/// 페이즈 전환 · 예고 있는 내려찍기 · 소환 · 등장 연출을 담당한다.
///
/// <para>🔴 <b>왜 <see cref="EnemyBase"/> 안에 안 넣었나.</b>
/// <c>EnemyBase.FixedUpdate</c> 는 적 800마리가 <b>매 물리 프레임</b> 도는 자리다
/// (<c>Docs/PERF.md</c>). 거기에 보스 분기를 하나 넣으면 <b>보스가 없는 웨이브에서도</b>
/// 800번씩 검사한다. 보스는 한 판에 한 마리뿐이니 <b>그 인스턴스에만 붙이는 게 맞다.</b></para>
///
/// <para>그래서 <c>Update</c> 를 쓴다. 이동은 <c>EnemyBase</c> 가 물리로 계속 하고,
/// 여기서는 <b>타이머와 상태만</b> 본다 — 물리 프레임에 묶일 이유가 없다.</para>
///
/// <para>수명은 보스와 같다. <see cref="EnemyBase.Dead"/> 가 되면 스스로 멈춘다.</para>
/// </summary>
[RequireComponent(typeof(EnemyBase))]
public class BossBrain : MonoBehaviour
{
    private EnemyBase       _boss;
    private BossPatternData _data;
    private ObjectPool      _pool;
    private GameObject      _slamPrefab;
    private EnemyData       _summonData;

    private int   _phase = -1;      // -1 = 아직 등장 처리 전
    private float _slamCd;
    private float _summonCd;
    private float _chargeCd;      // B-1 (D73)
    private float _lineCd;        // B-1 (D73)
    private bool  _busy;          // 🔴 돌진처럼 여러 프레임을 쓰는 기술이 도는 중
    private bool  _finished;

    /// <summary>지금 몇 페이즈인가 (0 = 1페이즈). HP 바가 읽는다.</summary>
    public int  Phase      => Mathf.Max(0, _phase);
    public int  PhaseCount => _data != null ? _data.PhaseCount : 1;

    /// <summary>보스가 등장할 때 <see cref="WaveManager"/> 가 부른다.</summary>
    public void Initialize(BossPatternData data, ObjectPool pool, GameObject slamPrefab, EnemyData summonData)
    {
        _boss       = GetComponent<EnemyBase>();
        _data       = data;
        _pool       = pool;
        _slamPrefab = slamPrefab;
        _summonData = summonData;

        _phase    = -1;
        _finished = false;
        _busy     = false;
        _boss.AiSuspended = false;   // 풀에서 재사용된 몸일 수 있다

        EnterPhase(0, isEntry: true);
    }

    private void Update()
    {
        if (_finished || _data == null || _boss == null) return;

        // 보스가 죽으면 조용히 멈춘다. 풀로 돌아간 오브젝트가 계속 소환하면 안 된다.
        if (_boss.Dead) { _finished = true; return; }

        // ── 페이즈 판정 ──
        int want = _data.PhaseOf(_boss.HpFraction);
        if (want > _phase) EnterPhase(want, isEntry: false);

        // ── 기술 ──
        _slamCd   -= Time.deltaTime;
        _summonCd -= Time.deltaTime;
        _chargeCd -= Time.deltaTime;
        _lineCd   -= Time.deltaTime;

        // 🔴 여러 프레임짜리 기술이 도는 중에는 다른 기술을 시작하지 않는다.
        //    돌진하면서 내려찍기까지 나가면 **예고를 보고 피할 자리가 없다** —
        //    그건 어려운 게 아니라 불공평한 것이다.
        if (_busy) return;

        if (_slamCd <= 0f)
        {
            Slam();
            _slamCd = BossPatternData.At(_data.SlamCooldown, _phase);
        }

        if (_summonCd <= 0f)
        {
            Summon(BossPatternData.At(_data.SummonCount, _phase));
            _summonCd = BossPatternData.At(_data.SummonCooldown, _phase);
        }

        float lineCd = BossPatternData.At(_data.LineCooldown, _phase);
        if (lineCd > 0f && _lineCd <= 0f)
        {
            LineSlam();
            _lineCd = lineCd;
        }

        float chargeCd = BossPatternData.At(_data.ChargeCooldown, _phase);
        if (chargeCd > 0f && _chargeCd <= 0f)
        {
            StartCoroutine(SummonCharge());
            _chargeCd = chargeCd;
        }
    }

    // ── B-1 기술 (D73) ──────────────────────────────────────────

    /// <summary>
    /// <b>쫄 소환 + 돌진</b> (사용자 요구 B-1).
    ///
    /// <para>🔑 <b>둘을 하나로 묶은 게 핵심이다.</b> 따로 두면 각각 *"가끔 일어나는 일"* 인데,
    /// 묶으면 <b>쫄이 나타난 것 자체가 돌진의 예고</b>가 되어 플레이어가 읽을 수 있다.
    /// 그리고 쫄이 길을 막아 <b>피할 자리를 좁힌다</b> — 두 기술이 서로를 돕는다.</para>
    ///
    /// <para>🔴 돌진 방향은 <b>노려보기가 끝나는 순간</b> 정한다. 매 프레임 다시 겨누면
    /// 유도 미사일이 되어 <b>피할 수 없는 공격</b>이 된다.</para>
    ///
    /// <para>🔴 <c>WaitForSeconds</c> 는 <c>timeScale</c> 을 따른다 — 레벨업 패널이 뜨면
    /// 보스도 같이 멈춘다. 그게 맞다(<c>unscaled</c> 로 하면 카드를 고르는 동안 맞는다).</para>
    /// </summary>
    private IEnumerator SummonCharge()
    {
        _busy = true;

        // ① 쫄이 먼저 나온다 — 이게 예고다
        Summon(BossPatternData.At(_data.ChargeSummonCount, _phase));

        // ② 멈춰 서서 노려본다
        _boss.AiSuspended = true;
        _boss.DriveVelocity(Vector2.zero);
        yield return new WaitForSeconds(Mathf.Max(0.05f, _data.ChargeWindup));

        // 죽었으면 여기서 끝. 아래에서 몸을 계속 밀면 시체가 날아간다.
        if (_boss == null || _boss.Dead) { EndCharge(); yield break; }

        // ③ 방향 확정 — 여기서 한 번만 겨눈다
        var target = PlayerStats.Current;
        Vector2 dir = target != null
            ? ((Vector2)target.transform.position - (Vector2)transform.position).normalized
            : Vector2.right;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;

        float t = 0f;
        while (t < _data.ChargeDuration)
        {
            if (_boss == null || _boss.Dead) { EndCharge(); yield break; }
            _boss.DriveVelocity(dir * Mathf.Max(0.1f, _data.ChargeSpeed));
            t += Time.deltaTime;
            yield return null;
        }

        // ④ 경직 — 플레이어의 반격 기회
        _boss.DriveVelocity(Vector2.zero);
        yield return new WaitForSeconds(Mathf.Max(0f, _data.ChargeRecover));

        EndCharge();
    }

    /// <summary>돌진 상태를 반드시 되돌린다. 🔴 어느 갈래로 끝나도 여기를 지나야 한다.</summary>
    private void EndCharge()
    {
        if (_boss != null)
        {
            _boss.AiSuspended = false;
            _boss.DriveVelocity(Vector2.zero);
        }
        _busy = false;
    }

    /// <summary>
    /// <b>예고 범위 일직선 공격</b> (사용자 요구 B-1).
    ///
    /// <para>🔑 <b>새 시스템을 안 만들었다.</b> <see cref="BossSlam"/>(예고 → 폭발 → 풀 반환)을
    /// <b>줄 세워</b> 놓는다 — <c>D37</c> 의 지뢰밭이 같은 부품을 썼고 이미 검증된 코드다.
    /// 직선 판정을 새로 짜면 예고 그림·풀 반환·피해 판정을 전부 다시 만들어야 한다.</para>
    ///
    /// <para>🔴 마디가 서로 겹치므로 <b>한 번에 여러 마디에 맞을 수 있다.</b>
    /// 그래서 <see cref="BossPatternData.LineDamage"/> 는 내려찍기보다 낮게 둔다.</para>
    ///
    /// <para>보스 몸에서 시작해 플레이어 쪽으로 뻗는다 — <b>보스가 쏘는 것</b>으로 읽혀야 한다.</para>
    /// </summary>
    private void LineSlam()
    {
        if (_slamPrefab == null || _pool == null) return;

        int segments = Mathf.Max(1, _data.LineSegments);
        var target = PlayerStats.Current;

        Vector2 origin = transform.position;
        Vector2 dir = target != null
            ? ((Vector2)target.transform.position - origin).normalized
            : Vector2.right;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;

        float step = _data.LineLength / segments;

        for (int i = 0; i < segments; i++)
        {
            // 🔑 보스 몸 바로 밖(0.5칸)부터 깐다. 몸 위에 깔면 보스가 자기 그림에 가린다.
            Vector2 at = origin + dir * (step * (i + 0.5f));

            var go = _pool.Get(_slamPrefab, at, Quaternion.identity);
            var slam = go.GetComponent<BossSlam>();
            if (slam != null)
                slam.Initialize(_data.LineDamage, _data.LineRadius, _data.LineWindup, _pool);
        }
    }

    // ── 페이즈 ──────────────────────────────────────────────────

    /// <summary>
    /// 페이즈에 들어간다. <b>되돌아가지 않는다</b> — 회복 수단이 있어도 페이즈는 한 방향이다.
    /// (임계값 근처에서 HP 가 오르내리면 연출이 계속 터진다)
    /// </summary>
    private void EnterPhase(int phase, bool isEntry)
    {
        _phase = phase;

        _boss.SetSpeedMultiplier(BossPatternData.At(_data.PhaseSpeedMult, phase));

        // 등장이든 페이즈 전환이든 "무슨 일이 났다"를 화면으로 알린다.
        var cam = Camera.main != null ? Camera.main.GetComponent<CameraController>() : null;
        if (cam != null)
            cam.Shake(_data.EntryShakeMagnitude, _data.EntryShakeDuration);

        // 첫 기술까지 잠깐 준다 — 등장하자마자 터지면 예고를 볼 새가 없다.
        _slamCd   = isEntry ? Mathf.Max(1.2f, _data.SlamWindup * 2f)
                            : BossPatternData.At(_data.SlamCooldown, phase) * 0.5f;
        _summonCd = isEntry ? BossPatternData.At(_data.SummonCooldown, phase)
                            : 0f;   // 페이즈가 바뀌면 그 자리에서 부른다

        // 🔴 B-1 의 두 기술도 <b>등장 유예를 받아야 한다</b> (D73).
        //    처음엔 이 두 줄이 없어서 <c>_chargeCd</c>·<c>_lineCd</c> 가 0 인 채로 시작했고,
        //    <b>보스가 나타난 첫 프레임에 일직선과 돌진이 동시에 나갔다.</b>
        //    등장음이 울리기도 전에 맞으면 그건 패턴이 아니라 사고다.
        //    (시험에서는 이 결함을 <b>거꾸로 이용해</b> 기술을 즉시 발동시켜 쟀다)
        //
        //    페이즈가 바뀔 때는 절반만 준다 — 내려찍기와 같은 규칙이다.
        float chargeCd = BossPatternData.At(_data.ChargeCooldown, phase);
        float lineCd   = BossPatternData.At(_data.LineCooldown,   phase);
        _chargeCd = isEntry ? Mathf.Max(chargeCd, _data.ChargeWindup + 2f) : chargeCd * 0.5f;
        _lineCd   = isEntry ? Mathf.Max(lineCd,   _data.LineWindup   + 2f) : lineCd   * 0.5f;
    }

    // ── 기술 ────────────────────────────────────────────────────

    /// <summary>플레이어가 <b>지금 있는 자리</b>에 예고를 깐다. 따라다니지 않는다 — 그래야 피할 수 있다.</summary>
    private void Slam()
    {
        if (_slamPrefab == null || _pool == null) return;

        var target = PlayerStats.Current;
        if (target == null) return;

        var go = _pool.Get(_slamPrefab, target.transform.position, Quaternion.identity);
        var slam = go.GetComponent<BossSlam>();
        if (slam != null)
            slam.Initialize(_data.SlamDamage, _data.SlamRadius, _data.SlamWindup, _pool);
    }

    private void Summon(int count)
    {
        if (count <= 0 || _summonData == null) return;

        var wm = GameManager.Instance != null ? GameManager.Instance.WaveManager : null;
        if (wm == null) return;

        for (int i = 0; i < count; i++)
        {
            Vector2 at = (Vector2)transform.position + Random.insideUnitCircle.normalized
                       * Random.Range(_data.SummonRadius * 0.5f, _data.SummonRadius);
            wm.SpawnMinion(_summonData, at);
        }
    }
}
