using System.Collections.Generic;
using UnityEngine;

public class ProjectileBase : MonoBehaviour
{
    [Header("관통")]
    [Tooltip("적을 몇 명까지 뚫는지. 1 이면 첫 적에게 맞고 사라진다(총알·화살·파이어볼).")]
    [SerializeField] private int pierceCount = 1;

    [Header("연출")]
    [Tooltip("초당 자전 각도. 0 이면 날아가는 방향으로 고정된다. 수리검처럼 " +
             "대칭이라 방향을 가리킬 필요가 없는 그림에만 쓴다.")]
    [SerializeField] private float spinSpeed = 0f;

    protected float    Damage;
    protected float    Speed;
    protected float    MaxRange;
    protected ObjectPool Pool;

    private Vector2   _direction;
    private Vector2   _startPos;
    private float     _size;

    private int _pierceLeft;
    // 관통 중에 같은 적의 콜라이더를 다시 스치면 또 때리게 된다. 한 마리를 세 번 때리는 건
    // 관통이 아니므로 맞힌 적을 기억해 둔다. 풀에서 다시 꺼낼 때 Initialize 가 비운다.
    private readonly HashSet<EnemyBase> _alreadyHit = new HashSet<EnemyBase>();

    public virtual void Initialize(Vector2 dir, float dmg, float size, float speed, float range, ObjectPool pool)
    {
        _direction = dir.normalized;
        Damage     = dmg;
        _size      = size;
        Speed      = speed;
        MaxRange   = range;
        Pool       = pool;
        _startPos  = transform.position;

        _pierceLeft = Mathf.Max(1, pierceCount);
        _alreadyHit.Clear();

        transform.localScale = Vector3.one * size;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    protected virtual void Update()
    {
        // 🔴 Translate(Space.Self) 로 밀면 진행 방향이 곧 현재 회전이다. 자전하는 발사체는
        // 그러면 나선을 그린다. 방향은 Initialize 때 굳힌 _direction 으로 따로 들고 간다
        // — 자전이 0 인 기존 발사체에게는 수식이 완전히 같다.
        transform.position += (Vector3)(_direction * (Speed * Time.deltaTime));

        if (spinSpeed != 0f)
            transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);

        if (Vector2.Distance(_startPos, transform.position) > MaxRange)
            Despawn();
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy")) return;

        var enemy = other.GetComponent<EnemyBase>();
        if (enemy == null) return;
        if (!_alreadyHit.Add(enemy)) return;   // 이미 뚫고 지나간 적

        // 넉백 기준점은 투사체 위치 — 날아온 방향으로 밀린다.
        enemy.TakeDamage(Damage, transform.position);

        _pierceLeft--;
        if (_pierceLeft <= 0) Despawn();
    }

    /// <summary>
    /// 날아가던 방향을 바꾼다 (D112 · <see cref="BouncingProjectile"/> 가 쓴다).
    ///
    /// <para>🔴 <b>사거리 기준점도 같이 옮긴다.</b> <see cref="Update"/> 는 <c>_startPos</c> 에서
    /// <c>MaxRange</c> 만큼 가면 사라지는데, 튕길 때 그대로 두면 <b>두 번째 도약이 거의 못 간다</b> —
    /// 이미 사거리를 다 쓴 지점에서 다시 재기 때문이다.</para>
    ///
    /// <para>🔵 <c>_alreadyHit</c> 은 <b>일부러 안 비운다.</b> 비우면 두 적 사이를 오가며
    /// 무한히 튕긴다 — 관통용으로 만든 이 집합이 튕김의 <b>중복 방지</b>에도 그대로 맞는다.</para>
    /// </summary>
    protected void Redirect(Vector2 newDir)
    {
        if (newDir.sqrMagnitude < 1e-6f) return;
        _direction = newDir.normalized;
        _startPos  = transform.position;
        transform.rotation = Quaternion.Euler(0f, 0f,
            Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg);
    }

    /// <summary>이미 맞힌 적인가 (파생 클래스가 다음 목표를 고를 때 쓴다).</summary>
    protected bool AlreadyHit(EnemyBase e) => e != null && _alreadyHit.Contains(e);

    /// <summary>
    /// 이번 생애에 몇 명까지 맞힐 수 있는지 다시 정한다 (D112).
    ///
    /// <para>🔴 <b><see cref="BouncingProjectile"/> 이 이걸 반드시 불러야 한다.</b>
    /// 튕김은 <c>OnTriggerEnter2D</c> 안에서 방향만 바꾸는데, 그 직전에 부모가
    /// <c>_pierceLeft</c> 를 깎고 <b>0 이 되면 <see cref="Despawn"/> 해 버린다</b> —
    /// 프리팹의 <c>pierceCount</c> 가 1 이면 <b>첫 명중에 사라져서 한 번도 안 튕긴다.</b></para>
    ///
    /// <para>🔑 프리팹에 값을 두 개(관통·도약) 두고 사람이 맞추게 하지 않는다 —
    /// 어긋나면 <b>조용히 깨진다.</b> 도약 수에서 <b>코드가 계산해</b> 넣는다.</para>
    /// </summary>
    protected void SetPierce(int count)
    {
        _pierceLeft = Mathf.Max(1, count);
    }

    protected void Despawn() => Pool.Return(gameObject);
}
