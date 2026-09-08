using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// <b>공전체</b> — 플레이어 둘레를 도는 구체가 <b>닿는 것을 때린다</b>
/// (D118 · 사용자 요구 "공전체").
///
/// <para>🔑 <b>이 게임 무기 11종이 전부 "겨눠서 쏘거나 벤다".</b>
/// 조준이 없고 늘 켜져 있는 것이 하나도 없어서 <b>뒤가 비어 있었다.</b>
/// 공전체는 겨누지 않는다 — 그래서 <b>등 뒤가 안전해진다</b>는 것이 이 무기가 파는 전부다.</para>
///
/// <para><b>CSV 열을 이렇게 읽는다</b> (<c>Weapons.csv</c>) — 새 열을 만들지 않았다:</para>
/// <list type="table">
///   <item><c>ProjectileCount</c> → <b>구체 개수</b>. 🔑 <see cref="WeaponBase.ScaledProjectileCount"/> 를
///         그대로 쓰므로 <c>DoubleProjectiles</c> 특전이 <b>구체를 두 배로</b> 만든다</item>
///   <item><c>Range</c> → 날아가는 거리가 아니라 <b>공전 반지름</b></item>
///   <item><c>ProjectileSpeed</c> → 유닛/초가 아니라 <b>회전 속도(도/초)</b></item>
///   <item><c>ProjectileSize</c> → 구체 그림 크기 <b>겸 판정 반지름</b>의 배수</item>
///   <item><c>Cooldown</c> → 날리는 주기가 아니라 <b>같은 적을 다시 때리기까지의 간격</b></item>
///   <item><c>ProjectilePrefab</c> → 날아가는 발사체가 아니라 <b>구체 그림</b></item>
/// </list>
///
/// <para>🔴 <b><see cref="Update"/> 를 선언하면 안 된다.</b> <see cref="WeaponBase"/> 가
/// <c>Update</c> 로 쿨다운을 돌린다 — 여기서 같은 이름을 쓰면 그걸 가려
/// <b>이 무기가 영영 발동하지 않는다.</b> 자리 잡기는 <see cref="LateUpdate"/> 에서 한다
/// (플레이어가 움직인 <b>뒤</b>라야 맞기도 하다). <see cref="SummonWeapon"/> 이 같은 함정을 적어 두었다.</para>
///
/// <para>🔵 <b>구체는 풀에서 살고 풀로 돌아간다.</b> 상점에서 무기를 환불하면
/// <see cref="WeaponManager.RemoveWeapon"/> → 풀 → <c>SetActive(false)</c> → <see cref="OnDisable"/> 로
/// 이어지므로, 거기서 회수하지 않으면 <b>구체만 필드에 영원히 남는다</b>(소환수가 겪은 일이다).</para>
/// </summary>
public class OrbitWeapon : WeaponBase
{
    /// <summary>컴파일 반영 확인용 (D27).</summary>
    public const int Version = 1;

    [Header("공전")]
    [Tooltip("구체 하나의 판정 반지름(유닛). ProjectileSize 가 여기에 곱해진다.")]
    [SerializeField] private float hitRadius = 0.42f;

    [Tooltip("구체가 도는 높이의 흔들림. 0 이면 완전한 원, 크면 타원처럼 눌린다.")]
    [SerializeField, Range(0f, 0.6f)] private float wobble = 0.12f;

    [Tooltip("구체 그림이 스스로 도는 속도(도/초). 공전과 별개다.")]
    [SerializeField] private float spinSpeed = 180f;

    private readonly List<GameObject> _orbs = new();

    /// <summary>지금까지 돈 각도(도). 개수가 바뀌어도 이어져야 튀지 않는다.</summary>
    private float _angle;

    /// <summary>
    /// 같은 적을 연달아 때리지 않게 기억해 둔다.
    /// 🔴 없으면 <see cref="Fire"/> 가 쿨다운마다 도는데도 <b>구체가 겹친 적이 여러 번</b> 맞는다.
    /// </summary>
    private readonly HashSet<EnemyBase> _hitThisSweep = new();

    private int DesiredOrbs => Mathf.Max(1, ScaledProjectileCount());
    private float OrbitRadius => Data != null ? Mathf.Max(0.3f, Data.GetRange(Level)) : 1f;
    private float OrbScale => Data != null ? Mathf.Max(0.1f, Data.GetProjectileSize(Level)) : 1f;

    // ── 생애 ────────────────────────────────────────────────────

    protected override void OnInitialized() => SyncOrbCount();

    protected override void OnLevelUp() => SyncOrbCount();

    private void OnDisable()
    {
        // 🔴 여기서 안 돌려주면 구체만 필드에 남는다 (SummonWeapon 이 겪은 일).
        for (int i = 0; i < _orbs.Count; i++)
            if (_orbs[i] != null && Pool != null) Pool.Return(_orbs[i]);
        _orbs.Clear();
    }

    /// <summary>
    /// 구체 수를 <see cref="DesiredOrbs"/> 에 맞춘다.
    ///
    /// <para>🔴 <b>매 프레임 다시 맞춘다.</b> <c>DoubleProjectiles</c> 특전은 <b>판 도중에</b> 켜지므로
    /// 소환 시점에 한 번만 세면 그 무기는 영영 특전을 못 받는다
    /// (<see cref="SummonWeapon"/> 이 소환수 수에서 같은 함정을 밟았다).</para>
    /// </summary>
    private void SyncOrbCount()
    {
        if (Data == null || Data.ProjectilePrefab == null || Pool == null) return;

        for (int i = _orbs.Count - 1; i >= 0; i--)
            if (_orbs[i] == null) _orbs.RemoveAt(i);

        int want = DesiredOrbs;
        while (_orbs.Count > want)
        {
            int last = _orbs.Count - 1;
            if (_orbs[last] != null) Pool.Return(_orbs[last]);
            _orbs.RemoveAt(last);
        }
        while (_orbs.Count < want)
            _orbs.Add(Pool.Get(Data.ProjectilePrefab, transform.position, Quaternion.identity));
    }

    // ── 자리 잡기 ───────────────────────────────────────────────

    /// <summary>
    /// 🔴 <c>Update</c> 가 아니라 <c>LateUpdate</c> 다 — 위 클래스 주석 참조.
    /// 플레이어가 움직인 뒤에 놓아야 구체가 뒤늦게 끌려오지 않는다.
    /// </summary>
    private void LateUpdate()
    {
        SyncOrbCount();
        if (_orbs.Count == 0) return;

        float speed = Data != null ? Data.ProjectileSpeed : 90f;
        _angle = Mathf.Repeat(_angle + speed * Time.deltaTime, 360f);

        Vector2 center = transform.position;
        float step = 360f / _orbs.Count;
        float scale = OrbScale;

        for (int i = 0; i < _orbs.Count; i++)
        {
            if (_orbs[i] == null) continue;
            float rad = (_angle + step * i) * Mathf.Deg2Rad;
            // 🔵 세로를 살짝 눌러 바닥에 누운 원처럼 보이게 한다. 탑다운이라 완전한 원은 떠 보인다.
            var pos = center + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad) * (1f - wobble)) * OrbitRadius;
            var t = _orbs[i].transform;
            t.position = pos;
            t.localScale = Vector3.one * scale;
            if (spinSpeed != 0f) t.Rotate(0f, 0f, spinSpeed * Time.deltaTime);
        }
    }

    // ── 때리기 ──────────────────────────────────────────────────

    /// <summary>
    /// 구체가 지금 겹쳐 있는 적을 때린다. <c>Cooldown</c> 이 곧 <b>같은 적을 다시 때리는 간격</b>이다.
    ///
    /// <para>🔴 <b>구체마다 따로 세면 겹친 적이 여러 번 맞는다.</b> 한 번의 쓸기(sweep) 안에서는
    /// 적을 한 번만 때린다 — 관통 발사체가 <c>_alreadyHit</c> 을 쓰는 것과 같은 이유다.</para>
    /// </summary>
    protected override void Fire()
    {
        if (_orbs.Count == 0) return;

        _hitThisSweep.Clear();
        float r   = hitRadius * OrbScale;
        float dmg = CalculateDamage();
        int   mask = LayerMask.GetMask("Enemy");
        bool  any = false;

        for (int i = 0; i < _orbs.Count; i++)
        {
            if (_orbs[i] == null) continue;
            Vector2 at = _orbs[i].transform.position;
            var hits = Physics2D.OverlapCircleAll(at, r, mask);
            for (int h = 0; h < hits.Length; h++)
            {
                var e = hits[h].GetComponent<EnemyBase>();
                if (e == null || e.Dead || !_hitThisSweep.Add(e)) continue;
                // 넉백 기준점은 구체 — 공전체 바깥으로 밀려난다.
                e.TakeDamage(dmg, at);
                any = true;
            }
        }

        // 허공을 때렸을 때는 소리를 내지 않는다. 늘 도는 무기라 그러면 판 내내 시끄럽다.
        if (any) AudioManager.Play(SfxId.WeaponSwing);
    }
}
