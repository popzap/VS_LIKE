using UnityEngine;

public class ProjectileBase : MonoBehaviour
{
    protected float    Damage;
    protected float    Speed;
    protected float    MaxRange;
    protected ObjectPool Pool;

    private Vector2   _direction;
    private Vector2   _startPos;
    private float     _size;

    public virtual void Initialize(Vector2 dir, float dmg, float size, float speed, float range, ObjectPool pool)
    {
        _direction = dir.normalized;
        Damage     = dmg;
        _size      = size;
        Speed      = speed;
        MaxRange   = range;
        Pool       = pool;
        _startPos  = transform.position;

        transform.localScale = Vector3.one * size;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    protected virtual void Update()
    {
        transform.Translate(Vector2.right * Speed * Time.deltaTime);
        if (Vector2.Distance(_startPos, transform.position) > MaxRange)
            Despawn();
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy")) return;
        // 넉백 기준점은 투사체 위치 — 날아온 방향으로 밀린다.
        other.GetComponent<EnemyBase>()?.TakeDamage(Damage, transform.position);
        Despawn();
    }

    protected void Despawn() => Pool.Return(gameObject);
}
