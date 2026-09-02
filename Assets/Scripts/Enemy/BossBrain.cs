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
