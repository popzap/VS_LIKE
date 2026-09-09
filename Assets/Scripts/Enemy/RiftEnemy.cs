using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// <b>균열</b> — 적이 <b>거기서</b> 나오는 자리 (D122 · 사용자가 고름).
///
/// <para>🔑 <b>이 게임에는 지리가 없었다.</b> <see cref="WaveManager.GetSpawnPosition"/> 이
/// 늘 <b>플레이어를 중심으로</b> 한 링 위에 적을 놓는다 — 어느 쪽으로 도망쳐도 세상은 똑같이 생겼고,
/// 위치는 오직 *"적과 나의 거리"* 로만 의미를 가졌다. 바닥에 점을 찍는 것(오벨리스크·석상)만으로는
/// 지리가 안 생긴다. <b>적이 태어나는 자리를 옮겨야</b> 생긴다.</para>
///
/// <para>🔑 <b>그래서 건물에게 처음으로 지킬 대상이 생긴다.</b> 터렛·포대·바리케이드·지뢰는
/// 전부 *"한 지점을 붙든다"* 인데 이 게임에는 <b>붙들 지점이 없었다</b>(D110·D117·D120 이
/// 세 번에 걸쳐 우회한 문제다). 균열이 그 빈칸이다.</para>
///
/// <para>🔵 <b><see cref="EnemyBase"/> 를 상속한 이유는 무기 13종을 안 건드리려는 것이다.</b>
/// 모든 무기·터렛은 <c>OverlapCircle(Enemy 레이어)</c> → <c>GetComponent&lt;EnemyBase&gt;()</c> →
/// <c>TakeDamage</c> 로 때린다. 균열을 별도 타입으로 만들면 <b>어떤 무기도 균열을 못 본다</b> —
/// 13곳을 고쳐야 한다. 적으로 태어나면 피격·체력·사망·드랍·개체 상한이 전부 공짜로 따라온다.</para>
///
/// <para>🔴 <b>방치하면 나빠진다.</b> 소환 간격이 열려 있는 시간에 비례해 줄어든다 —
/// 안 그러면 균열은 *"무시해도 되는 장식"* 이 된다. 무시할 수 있는 위협은 위협이 아니다.</para>
///
/// <para><b>CSV 는 <c>Enemies.csv</c> 의 <c>Rift</c> 행을 쓴다</b> — 새 표를 만들지 않았다:</para>
/// <list type="bullet">
/// <item><c>MaxHp</c> = <b>닫는 데 드는 화력</b>. 층 배율·<c>Bounty</c> 세금이 그대로 걸린다</item>
/// <item><c>MoveSpeed</c>·<c>ContactDamage</c> = <b>0</b>. 균열은 걷지도 때리지도 않는다</item>
/// <item><c>XpDrop</c>·<c>CurrencyDrop</c> = <b>닫았을 때의 보상</b>(엘리트급)</item>
/// <item><c>AI</c> = 안 쓴다. <see cref="FixedUpdate"/> 를 통째로 덮어 이동을 없앴다</item>
/// </list>
/// </summary>
public class RiftEnemy : EnemyBase
{
    /// <summary>컴파일 반영 확인용 (D27).</summary>
    public new const int Version = 3;   // 2 = 뱉은 수를 반환값으로 센다 · 3 = base.OnInitialized (크기)

    [Header("소환")]
    [Tooltip("열리고 첫 소환까지의 여유(초). 🔴 0 이면 열리자마자 무리가 쏟아져 예고가 없다.")]
    [SerializeField] private float firstSpawnDelay = 2f;

    [Tooltip("처음 소환 간격(초).")]
    [SerializeField] private float startInterval = 3.5f;

    [Tooltip("아무리 방치해도 이보다 빨라지지는 않는다(초).")]
    [SerializeField] private float minInterval = 1f;

    [Tooltip("초당 간격이 줄어드는 양. 🔑 이 값이 '무시하면 나빠진다'를 만든다.")]
    [SerializeField] private float intervalDecayPerSec = 0.06f;

    [Tooltip("한 번에 내보내는 수.")]
    [SerializeField] private int spawnBatch = 1;

    [Tooltip("균열 입에서 흩어져 나오는 반경(유닛). 정확히 같은 점에 겹쳐 나오지 않게 한다.")]
    [SerializeField] private float scatter = 0.9f;

    [Header("연출")]
    [Tooltip("입이 도는 속도(도/초). 🔵 멈춰 있는 것이 살아 있어 보이게 하는 유일한 신호다.")]
    [SerializeField] private float spinSpeed = 35f;

    /// <summary>지금 열려 있는 균열들. <see cref="RiftDirector"/> 가 개수를 센다.</summary>
    public static readonly List<RiftEnemy> Active = new();

    private Vector2 _anchor;
    private float   _openedAt;
    private float   _nextSpawnAt;
    private int     _spawned;

    /// <summary>
    /// 🔴 <b>재배치 대상에서 뺀다.</b> <see cref="WaveManager.RecycleFarEnemies"/> 가
    /// 멀어진 적을 플레이어 곁으로 옮기는데, 균열이 따라오면 <b>자리라는 개념 자체가 없어진다.</b>
    /// </summary>
    public override bool IsAnchored => true;

    /// <summary>열린 지 몇 초 됐나 (검증용).</summary>
    public float OpenSeconds => Time.time - _openedAt;

    /// <summary>지금까지 뱉은 수 (검증용).</summary>
    public int SpawnedCount => _spawned;

    /// <summary>지금 소환 간격 — 열려 있을수록 짧아진다 (검증용).</summary>
    public float CurrentInterval
        => Mathf.Max(minInterval, startInterval - OpenSeconds * Mathf.Max(0f, intervalDecayPerSec));

    // ── 생애 ────────────────────────────────────────────────────

    protected override void OnInitialized()
    {
        // 🔴 <b>base 를 반드시 부른다.</b> 여기가 그림·색·<b>크기</b>·<c>EnemyVisual.Setup</c> 을 넣는 자리다 —
        //    빼먹으면 <c>SizeScale 3.2</c> 가 통째로 무시돼 균열이 <b>플레이어보다 작게</b> 그려진다
        //    (실제로 그렇게 나왔다: 화면 폭 0.46 유닛 vs 플레이어 1.0).
        base.OnInitialized();

        _anchor      = transform.position;
        _openedAt    = Time.time;
        _nextSpawnAt = Time.time + Mathf.Max(0f, firstSpawnDelay);
        _spawned     = 0;

        // 🔴 풀에서 재사용되므로 중복 등록을 막는다.
        if (!Active.Contains(this)) Active.Add(this);
    }

    private void OnDisable() => Active.Remove(this);

    protected override void OnDeath()
    {
        Active.Remove(this);

        // 🔑 닫는 것이 곧 보상이다. 엘리트가 아니라서 base.Die 가 상자를 안 놓는다 —
        //    여기서 직접 놓는다. 균열은 "가서 부술 만한 것"이어야 한다.
        if (ExperienceManager.Instance != null)
            ExperienceManager.Instance.SpawnChest(transform.position);
    }

    // ── 이동 없음 ───────────────────────────────────────────────

    /// <summary>
    /// 🔴 <b><c>base.FixedUpdate()</c> 를 부르지 않는다.</b> 균열은 걷지 않는다.
    ///
    /// <para>🔴 그런데 <b>가만히 두는 것만으로는 안 된다</b> — 피격 넉백이
    /// <c>Rb.linearVelocity</c> 를 직접 밀어 넣으므로(<c>ApplyKnockback</c>),
    /// 아무것도 안 하면 균열이 <b>맞을 때마다 뒤로 밀려 흘러간다.</b>
    /// 자리로 되돌려 놓는 이 두 줄이 "균열은 자리다"를 지킨다.</para>
    /// </summary>
    protected override void FixedUpdate()
    {
        if (IsDead) return;
        Rb.linearVelocity = Vector2.zero;
        if (Rb.position != _anchor) Rb.position = _anchor;
    }

    // ── 뱉기 ────────────────────────────────────────────────────

    /// <summary>
    /// 🔵 <see cref="EnemyBase"/> 에는 <c>Update</c> 가 없다 — 여기서 선언해도 가리는 것이 없다
    /// (<see cref="WeaponBase"/> 를 상속할 때와 다르다 · D118).
    /// </summary>
    private void Update()
    {
        if (IsDead) return;

        if (spinSpeed != 0f) transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);

        if (Time.time < _nextSpawnAt) return;
        _nextSpawnAt = Time.time + CurrentInterval;
        Disgorge();
    }

    private void Disgorge()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.WaveManager == null) return;    // 🔴 ?. 금지 (I-24)

        var wm = gm.WaveManager;
        if (!wm.IsWaveActive) return;

        for (int i = 0; i < Mathf.Max(1, spawnBatch); i++)
        {
            var data = wm.RandomCurrentEnemy();
            if (data == null) return;                        // 이 웨이브에 잡몹이 없다

            Vector2 at = _anchor + Random.insideUnitCircle * scatter;
            if (ArenaBounds.Enabled) at = ArenaBounds.Clamp(at);

            // 🔴 <b>반환값을 본다.</b> 개체 상한에 걸리면 <c>SpawnMinion</c> 은 조용히 <c>null</c> 을 준다 —
            //    세어 두기만 하면 "뱉었다"는 숫자가 <b>실제로 태어난 수와 어긋난다.</b>
            if (wm.SpawnMinion(data, at) != null) _spawned++;
        }
    }
}
