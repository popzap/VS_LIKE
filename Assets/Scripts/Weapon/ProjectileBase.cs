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

    protected void Despawn() => Pool.Return(gameObject);
}
