using UnityEngine;

/// <summary>
/// <b>튕기는 발사체</b> — 적을 맞히면 사라지지 않고 <b>다음 적으로 꺾인다</b>
/// (D112 · 사용자 요구 "적들 사이를 튕겨다니는 쿠나이").
///
/// <para>🔑 <b>관통과 다르다.</b> 관통(<see cref="ProjectileBase"/> 의 <c>pierceCount</c>)은
/// <b>직선을 유지한 채</b> 여럿을 뚫는다 — 일렬로 선 적에게만 값어치가 있다.
/// 튕김은 <b>방향을 바꾼다</b> — 흩어진 적에게 강하다.
/// 그래서 수리검(관통 3)의 진화로 자연스럽다: <b>같은 무기가 반대 상황을 커버하게 된다.</b></para>
///
/// <para>🔑 <b>중복 방지는 새로 만들지 않았다.</b> 부모의 <c>_alreadyHit</c> 이 이미
/// "이 발사체가 맞힌 적"을 들고 있다 — 관통용으로 만든 것인데 튕김에도 그대로 맞는다.
/// 🔴 이게 없으면 두 적 사이를 <b>무한히 오간다.</b></para>
///
/// <para>🔴 <b>다음 목표가 없으면 사라진다.</b> 방향만 유지하고 계속 날리면
/// 벽 없는 맵에서 <b>화면 밖으로 영영 날아간다</b> — 풀이 안 돌아온다.</para>
/// </summary>
public class BouncingProjectile : ProjectileBase
{
    /// <summary>컴파일 반영 확인용 (D27).</summary>
    public const int Version = 1;

    [Header("튕김")]
    [Tooltip("몇 번까지 꺾이는지. 3 이면 최대 4마리를 맞힌다(첫 명중 + 3회 도약).")]
    [SerializeField] private int bounceCount = 3;

    [Tooltip("다음 목표를 찾는 반경(유닛). 이 안에 아직 안 맞은 적이 없으면 사라진다.")]
    [SerializeField] private float searchRadius = 6f;

    [Tooltip("도약마다 피해에 곱해지는 값. 1 이면 안 줄고, 0.85 면 갈수록 약해진다.")]
    [SerializeField, Range(0.3f, 1f)] private float damageFalloff = 0.85f;

    private int _bouncesLeft;

    public override void Initialize(Vector2 dir, float dmg, float size, float speed, float range, ObjectPool pool)
    {
        base.Initialize(dir, dmg, size, speed, range, pool);
        // 🔴 풀에서 재사용되므로 반드시 되돌린다 — 안 하면 두 번째 생애부터 안 튕긴다.
        //
        // 🔑 <b>특전은 여기서 "관통"이 아니라 "도약"으로 읽힌다</b> (D114).
        //    부모는 ExtraPierce 를 관통 +1 로 더해 두는데, 아래 SetPierce 가 그걸 덮어쓴다 —
        //    튕기는 발사체에게 관통 +1 은 <b>직선으로 하나 더 뚫는 것</b>이라 뜻이 어긋난다.
        //    같은 특전을 이 무기의 언어(도약 +1)로 옮겨야 "적을 하나 더 맞힌다"가 유지된다.
        _bouncesLeft = Mathf.Max(0, bounceCount) + BonusPierceNow;

        // 🔴 <b>이 줄이 없으면 첫 명중에 사라져서 한 번도 안 튕긴다.</b>
        //    부모는 맞힐 때마다 관통을 깎고 0 이면 Despawn 한다 — 도약 수만큼 여유를 줘야 한다.
        //    프리팹에서 사람이 맞추게 하지 않고 여기서 계산한다(어긋나면 조용히 깨진다).
        SetPierce(_bouncesLeft + 1);
    }

    protected override void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy")) return;
        var enemy = other.GetComponent<EnemyBase>();
        if (enemy == null || AlreadyHit(enemy)) return;

        // 🔑 부모가 피해·중복 기록·관통 감소를 다 한다. 여기서 다시 때리면 두 번 맞는다.
        base.OnTriggerEnter2D(other);

        if (_bouncesLeft <= 0) return;      // 도약을 다 썼다 — 부모가 이미 처리했다

        var next = FindNextTarget();
        if (next == null) { Despawn(); return; }

        _bouncesLeft--;
        Damage *= damageFalloff;
        Redirect((Vector2)next.position - (Vector2)transform.position);
    }

    /// <summary>아직 안 맞은 가장 가까운 적. 없으면 null.</summary>
    private Transform FindNextTarget()
    {
        var hits = Physics2D.OverlapCircleAll(transform.position, searchRadius,
                                              LayerMask.GetMask("Enemy"));
        Transform best = null; float bestSqr = float.MaxValue;
        for (int i = 0; i < hits.Length; i++)
        {
            var e = hits[i].GetComponent<EnemyBase>();
            if (e == null || e.Dead || AlreadyHit(e)) continue;
            float sqr = ((Vector2)e.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (sqr < bestSqr) { bestSqr = sqr; best = e.transform; }
        }
        return best;
    }
}
