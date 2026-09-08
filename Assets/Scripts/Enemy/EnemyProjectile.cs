using UnityEngine;

/// <summary>
/// 적이 쏘는 투사체. <see cref="ProjectileBase"/> 와 따로 두는 이유는 <b>맞히는 대상이 반대</b>이기 때문이다.
///
/// <para>⚠️ 충돌 판정을 콜라이더가 아니라 <b>플레이어와의 거리</b>로 한다.
/// 이 프로젝트의 Projectile 레이어는 플레이어를 때리라고 만든 게 아니라서
/// 물리 레이어 행렬에 적탄용 조합이 없다. 레이어 설정을 건드리면 기존 무기 판정까지
/// 같이 흔들리므로, 화면에 몇 발 없는 적탄 쪽을 거리 검사로 처리했다.</para>
/// </summary>
public class EnemyProjectile : MonoBehaviour
{
    /// <summary>명중 판정 반경. 스프라이트 크기와 무관한 고정값이다.</summary>
    private const float HitRadius = 0.45f;

    private Vector2    _dir;
    private float      _speed;
    private float      _damage;
    private float      _life;
    private ObjectPool _pool;
    private Transform  _player;

    public void Initialize(Vector2 dir, float damage, float speed, float life, ObjectPool pool)
    {
        _dir    = dir.normalized;
        _damage = damage;
        _speed  = speed;
        _life   = life;
        _pool   = pool;

        var p   = GameObject.FindGameObjectWithTag("Player");
        _player = p != null ? p.transform : null;

        float angle = Mathf.Atan2(_dir.y, _dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void Update()
    {
        transform.position += (Vector3)(_dir * (_speed * Time.deltaTime));

        _life -= Time.deltaTime;
        if (_life <= 0f) { Despawn(); return; }

        if (_player == null) return;

        Vector2 gap = (Vector2)_player.position - (Vector2)transform.position;
        if (gap.sqrMagnitude > HitRadius * HitRadius) return;

        // TryTakeHit 안에 무적 시간이 들어 있다. 여기서 따로 연타를 막을 필요는 없다.
        // 🔴 ranged: true — 보호막이 막는 유일한 종류다 (D113).
        _player.GetComponent<PlayerStats>()?.TryTakeHit(_damage, transform.position, ranged: true);
        Despawn();
    }

    private void Despawn()
    {
        if (_pool != null) _pool.Return(gameObject);
        else               gameObject.SetActive(false);
    }
}
