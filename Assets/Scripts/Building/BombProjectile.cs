using UnityEngine;

/// <summary>
/// 곡사포(<see cref="BombardBuilding"/>) 가 쏘는 폭탄. 목표 <b>지점</b>까지 포물선을 그리며
/// 날아가고, 도착하면 폭발 오브젝트를 꺼내 놓고 자기는 풀로 돌아간다.
///
/// <para>목표를 <c>Transform</c> 이 아니라 좌표로 받는다. 날아가는 도중에 적이 죽으면
/// <c>Transform</c> 은 풀로 돌아가 다른 자리로 재사용되기 때문에, 폭탄이 엉뚱한 곳까지
/// 따라가 터진다.</para>
/// </summary>
public class BombProjectile : MonoBehaviour
{
    [Tooltip("포물선 정점의 높이(월드 유닛). 0 이면 직선으로 날아간다.")]
    [SerializeField] private float arcHeight = 1.2f;
    [Tooltip("날아가는 동안 회전하는 속도(도/초).")]
    [SerializeField] private float spinSpeed = 540f;
    [Tooltip("도착한 뒤 터지기까지 기다리는 시간(초). 0 이면 즉시 터진다(곡사포).")]
    [SerializeField] private float fuseTime = 0f;
    [Tooltip("신관이 타들어가는 정도를 보여 줄 그림. 시간이 아니라 게이지다 — 0=차가움, 마지막=벌겋게 달아오름. 비워 두면 안 바뀐다.")]
    [SerializeField] private Sprite[] fuseFrames;

    private Vector2        _start;
    private Vector2        _target;
    private float          _progress;
    private float          _flightTime;
    private float          _damage;
    private float          _explosionRadius;
    private GameObject     _explosionPrefab;
    private ObjectPool     _pool;
    private bool           _fusing;
    private float          _fuseElapsed;
    private SpriteRenderer _sr;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
    }

    public void Initialize(Vector2 target, float damage, float explosionRadius, float speed,
                           GameObject explosionPrefab, ObjectPool pool)
    {
        _start           = transform.position;
        _target          = target;
        _damage          = damage;
        _explosionRadius = explosionRadius;
        _explosionPrefab = explosionPrefab;
        _pool            = pool;

        _flightTime = Mathf.Max(0.05f, Vector2.Distance(_start, _target) / Mathf.Max(0.1f, speed));
        _progress   = 0f;
        transform.rotation = Quaternion.identity;

        // 풀에서 재사용되므로 신관 상태도 매번 되돌린다.
        _fusing      = false;
        _fuseElapsed = 0f;
        if (_sr != null && fuseFrames != null && fuseFrames.Length > 0) _sr.sprite = fuseFrames[0];
    }

    private void Update()
    {
        // Initialize 전이면 아직 날 준비가 안 된 것이다 (풀에서 갓 꺼낸 한 프레임).
        if (_pool == null) return;

        if (_fusing) { TickFuse(); return; }

        _progress += Time.deltaTime / _flightTime;
        float t = Mathf.Clamp01(_progress);

        Vector2 pos = Vector2.Lerp(_start, _target, t);
        pos.y += Mathf.Sin(Mathf.PI * t) * arcHeight;
        transform.position = pos;
        transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);

        if (_progress < 1f) return;

        transform.position = _target;

        if (fuseTime <= 0f)
        {
            Explode();
            return;
        }

        // 바닥에 놓인 폭탄이 팽이처럼 돌면 안 된다.
        transform.rotation = Quaternion.identity;
        _fusing = true;
        TickFuse();
    }

    private void TickFuse()
    {
        _fuseElapsed += Time.deltaTime;

        if (_sr != null && fuseFrames != null && fuseFrames.Length > 0)
        {
            int i = Mathf.Clamp(Mathf.FloorToInt(_fuseElapsed / fuseTime * fuseFrames.Length),
                                0, fuseFrames.Length - 1);
            _sr.sprite = fuseFrames[i];
        }

        if (_fuseElapsed >= fuseTime) Explode();
    }

    private void Explode()
    {
        var pool = _pool;
        _pool = null;   // 같은 프레임에 두 번 터지지 않도록 먼저 끊는다

        if (_explosionPrefab != null)
        {
            var fx  = pool.Get(_explosionPrefab, _target, Quaternion.identity);
            var aoe = fx.GetComponent<AoeProjectile>();

            // Initialize 를 안 부르면 폭발이 풀로 돌아가지 않고 화면에 그대로 남는다 (I-39).
            if (aoe != null) aoe.Initialize(_damage, _explosionRadius, pool);
            else             pool.Return(fx);
        }

        pool.Return(gameObject);
    }
}
