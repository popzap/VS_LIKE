using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// <b>지뢰</b> — 발밑에 두고 간다. 적이 밟으면 터진다 (D119 · 사용자 요구 "지뢰").
///
/// <para>🔑 <b>폭탄과 무엇이 다른가 — 던지는 것과 두고 가는 것.</b>
/// <see cref="AoeWeapon"/>(폭탄·화염구)은 <b>가장 가까운 적</b>을 노려 날린다.
/// 지뢰는 <b>플레이어가 지금 선 자리</b>에 놓는다 — 조준이 아니라 <b>내가 지나온 길</b>이 무기가 된다.</para>
///
/// <para>🔑 <b>그래서 이 게임의 핵심 동사와 짝이 맞는다.</b> 뱀서라이크에서 플레이어가
/// 늘 하는 일은 <b>도망치는 것</b>인데, 지금까지 그 시간은 아무것도 낳지 않았다.
/// 지뢰는 <b>도망친 거리만큼 피해가 된다</b> — 쫓아오는 무리가 제 발로 밟는다.</para>
///
/// <para><b>CSV 열을 이렇게 읽는다</b> (<c>Weapons.csv</c>) — 새 열을 만들지 않았다:</para>
/// <list type="table">
///   <item><c>ProjectileCount</c> → 한 번에 <b>몇 개를 놓는지</b></item>
///   <item><c>Range</c> → 사거리가 아니라 <b>밟았다고 볼 반경</b>(기폭 반경)</item>
///   <item><c>ProjectileSize</c> → <b>폭발 반경</b>의 배수</item>
///   <item><c>ProjectileSpeed</c> → 유닛/초가 아니라 <b>수명(초)</b>. 지나면 조용히 사라진다</item>
///   <item><c>ProjectilePrefab</c> → 지뢰 그림(<see cref="MineBody"/> 가 붙어 있어야 한다)</item>
///   <item><c>TravelPrefab</c> → 🔑 <b>터질 때 쓸 폭발</b>(<see cref="AoeProjectile"/>).
///         AoE 무기와 열 이름이 반대로 쓰이는데, 저쪽은 "날아가는 몸체"이고 여기는 지뢰가 몸체다</item>
/// </list>
///
/// <para>🔴 <b>놓자마자 터지면 안 된다.</b> 플레이어 발밑에 놓는데 적이 이미 붙어 있으면
/// 즉발 폭탄과 똑같아진다 — <see cref="MineBody"/> 가 <c>armDelay</c> 동안 잠들어 있는다.</para>
///
/// <para>🔴 <b>개수 상한이 필요하다.</b> 수명이 길고 쿨다운이 짧으면 화면이 지뢰로 덮인다.
/// 오래된 것부터 회수한다.</para>
/// </summary>
public class MineWeapon : WeaponBase
{
    /// <summary>컴파일 반영 확인용 (D27).</summary>
    public const int Version = 1;

    [Header("지뢰")]
    [Tooltip("동시에 깔아 둘 수 있는 최대 개수. 넘으면 가장 오래된 것부터 사라진다.")]
    [SerializeField] private int maxLive = 6;

    [Tooltip("놓은 뒤 이만큼은 안 터진다(초). 🔴 0 이면 즉발 폭탄과 같아진다.")]
    [SerializeField] private float armDelay = 0.45f;

    [Tooltip("폭발 반경의 기준(유닛). ProjectileSize 가 여기에 곱해진다.")]
    [SerializeField] private float explosionRadius = 1.8f;

    [Tooltip("놓을 때 서로 겹치지 않게 흩뿌리는 반경(유닛). 여러 개를 한 번에 놓을 때만 쓴다.")]
    [SerializeField] private float scatter = 0.55f;

    /// <summary>깔려 있는 지뢰들. 🔑 <b>오래된 것이 앞</b>이라 상한을 넘으면 앞에서 뺀다.</summary>
    private readonly List<MineBody> _live = new();

    protected override void Fire()
    {
        if (Data.ProjectilePrefab == null) return;

        int    count  = ScaledProjectileCount();
        float  dmg    = CalculateDamage();
        float  radius = explosionRadius * Data.GetProjectileSize(Level);
        float  trigger = Mathf.Max(0.2f, Data.GetRange(Level));
        float  life   = Mathf.Max(1f, Data.ProjectileSpeed);

        for (int i = 0; i < count; i++)
        {
            // 🔑 겨누지 않는다 — 지금 선 자리에 둔다. 이게 폭탄과 갈리는 지점이다.
            Vector2 at = (Vector2)transform.position;
            if (count > 1) at += Random.insideUnitCircle * scatter;

            var go   = Pool.Get(Data.ProjectilePrefab, at, Quaternion.identity);
            var mine = go.GetComponent<MineBody>();
            if (mine == null) { Pool.Return(go); continue; }

            mine.Initialize(dmg, radius, trigger, armDelay, life, Data.TravelPrefab, Pool, this);
            _live.Add(mine);
        }

        Trim();
        AudioManager.Play(SfxId.BuildingPlace);   // 🟡 자리표시 — 놓는 소리는 아직 없다
    }

    /// <summary>터졌거나 사라진 지뢰를 목록에서 뺀다. <see cref="MineBody"/> 가 부른다.</summary>
    public void Forget(MineBody mine) => _live.Remove(mine);

    /// <summary>🔴 상한을 넘으면 <b>가장 오래된 것</b>부터 조용히 회수한다(터뜨리지 않는다).</summary>
    private void Trim()
    {
        for (int i = _live.Count - 1; i >= 0; i--)
            if (_live[i] == null) _live.RemoveAt(i);

        int cap = Mathf.Max(1, maxLive);
        while (_live.Count > cap)
        {
            var oldest = _live[0];
            _live.RemoveAt(0);
            if (oldest != null) oldest.Recall();
        }
    }

    /// <summary>
    /// 🔴 무기를 환불하면 깔아 둔 지뢰도 같이 걷는다.
    /// 안 하면 <b>없는 무기의 지뢰가 필드에 남는다</b> (소환수가 겪은 일 · <see cref="SummonWeapon"/>).
    /// </summary>
    private void OnDisable()
    {
        for (int i = 0; i < _live.Count; i++)
            if (_live[i] != null) _live[i].Recall();
        _live.Clear();
    }
}
