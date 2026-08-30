using UnityEngine;

/// <summary>
/// 바닥에 깔린 독 장판. 정해진 시간 동안 그 자리에 남아 안에 있는 적을
/// <b>주기적으로</b> 깎고 <b>느리게</b> 만든다.
///
/// <para>콜라이더가 없다. 판정은 매 틱 <see cref="Physics2D.OverlapCircleAll"/> 로 한다 —
/// Trigger Enter/Exit 짝을 맞추면 적이 죽거나 풀로 돌아갈 때 Exit 가 안 와서
/// 슬로우가 영구히 걸린 채로 남는다. 대신 슬로우를 <b>매 틱 짧게 다시 걸어</b>
/// 안 걸어 주면 저절로 풀리게 했다.</para>
/// </summary>
public class ToxinField : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sr;

    [Header("장판 애니메이션")]
    [Tooltip("순서대로 무한 반복할 프레임. 이음매 없이 순환하게 그려져 있다(핑퐁 아님).")]
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float frameRate = 12f;

    [Header("크기")]
    [Tooltip("scale 1 일 때 이 스프라이트가 덮는 반경(월드 유닛). 그림을 실측한 값이라 함부로 바꾸면 그림이 거짓말을 한다.")]
    [SerializeField] private float spriteRadiusAtScaleOne = 0.97f;

    [Header("지속")]
    [Tooltip("장판이 바닥에 남아 있는 시간(초).")]
    [SerializeField] private float fieldDuration = 4f;
    [Tooltip("피해·슬로우를 다시 넣는 간격(초).")]
    [SerializeField] private float tickInterval = 0.5f;
    [Tooltip("장판 안에서의 이동 속도 배율. 0.6 이면 40% 감속.")]
    [Range(0.1f, 1f)]
    [SerializeField] private float slowMult = 0.6f;

    private float      _damage;
    private float      _radius;
    private float      _elapsed;
    private float      _tickTimer;
    private ObjectPool _pool;

    public void Initialize(float dmgPerTick, float radius, ObjectPool pool)
    {
        _damage    = dmgPerTick;
        _radius    = radius;
        _pool      = pool;
        _elapsed   = 0f;
        _tickTimer = 0f;   // 깔리자마자 한 번 때린다

        if (spriteRadiusAtScaleOne > 0f)
            transform.localScale = Vector3.one * (radius / spriteRadiusAtScaleOne);

        if (sr != null && frames != null && frames.Length > 0) sr.sprite = frames[0];
    }

    private void Update()
    {
        // Initialize 전이면 풀에서 갓 꺼낸 한 프레임이다.
        if (_pool == null) return;

        _elapsed += Time.deltaTime;

        if (sr != null && frames != null && frames.Length > 0 && frameRate > 0f)
            sr.sprite = frames[Mathf.FloorToInt(_elapsed * frameRate) % frames.Length];

        _tickTimer -= Time.deltaTime;
        if (_tickTimer <= 0f)
        {
            _tickTimer = tickInterval;
            Tick();
        }

        if (_elapsed < fieldDuration) return;

        var pool = _pool;
        _pool = null;   // 같은 프레임에 두 번 반납하지 않도록 먼저 끊는다
        pool.Return(gameObject);
    }

    private void Tick()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _radius,
                            LayerMask.GetMask("Enemy"));
        foreach (var h in hits)
        {
            var enemy = h.GetComponent<EnemyBase>();
            if (enemy == null) continue;

            // from 을 넘기지 않는다. 넘기면 초당 여러 번 밀려서 적이 장판 밖으로 튕겨 나간다.
            enemy.TakeDamage(_damage);
            // 틱보다 조금 길게 걸어야 틱 사이에 슬로우가 끊기지 않는다.
            enemy.ApplySlow(slowMult, tickInterval * 1.6f);
        }
    }
}
