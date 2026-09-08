using UnityEngine;

/// <summary>
/// 깔려 있는 지뢰 한 개 (D119 · <see cref="MineWeapon"/> 이 낳는다).
///
/// <para>🔴 <b>콜라이더로 밟힘을 잡지 않는다.</b> 이 프로젝트의 적 콜라이더는
/// <c>isTrigger</c> 라 겹침은 알려 주지만, 지뢰 쪽에도 트리거를 달면
/// <b>플레이어·발사체·다른 건물까지 전부 들어온다.</b> 필요한 건 "적이 반경 안에 있나" 하나뿐이라
/// <see cref="Physics2D.OverlapCircle"/> 로 <b>적 레이어만</b> 본다
/// (<see cref="BarricadeBuilding"/> 이 같은 이유로 물리를 안 쓴 것과 같은 판단이다).</para>
///
/// <para>🔴 <b>놓자마자는 안 터진다.</b> 플레이어 발밑에 놓는데 적이 이미 붙어 있으면
/// 즉발 폭탄과 똑같아진다 — <c>armDelay</c> 동안은 잠들어 있고, 그동안 <b>어둡게</b> 보여
/// "아직 안 켜졌다"를 눈으로 알 수 있게 한다.</para>
///
/// <para>🔵 <b>폭발은 새로 만들지 않았다.</b> <see cref="AoeProjectile"/> 이 이미
/// "반경만큼 커져서 한 번 터지고 풀로 돌아가는" 일을 한다 — 그대로 빌려 쓴다.</para>
/// </summary>
public class MineBody : MonoBehaviour
{
    /// <summary>컴파일 반영 확인용 (D27).</summary>
    public const int Version = 1;

    [SerializeField] private SpriteRenderer sr;

    [Tooltip("아직 안 켜졌을 때의 색. 켜지면 원래 색으로 돌아온다.")]
    [SerializeField] private Color armingTint = new(0.45f, 0.45f, 0.5f, 0.75f);

    [Tooltip("켜진 뒤 깜빡이는 주기(초). 0 이면 안 깜빡인다.")]
    [SerializeField] private float blinkInterval = 0.5f;

    private float      _damage, _radius, _trigger, _life;
    private float      _armAt;          // 이 시각이 지나야 밟힌다
    private float      _dieAt;          // 이 시각이 지나면 조용히 사라진다
    private GameObject _explosion;
    private ObjectPool _pool;
    private MineWeapon _owner;
    private Color      _baseColor;
    private bool       _armed, _spent;

    /// <summary>지금 밟히면 터지나 (검증용).</summary>
    public bool IsArmed => _armed && !_spent;

    public void Initialize(float damage, float radius, float trigger, float armDelay,
                           float life, GameObject explosion, ObjectPool pool, MineWeapon owner)
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        // 🔴 풀에서 재사용되므로 "원래 색"을 한 번만 잡는다 — 안 그러면 어두운 색이 원본이 된다.
        if (_baseColor == default) _baseColor = sr != null ? sr.color : Color.white;

        _damage = damage; _radius = radius; _trigger = trigger; _life = life;
        _explosion = explosion; _pool = pool; _owner = owner;

        _armAt  = Time.time + Mathf.Max(0f, armDelay);
        _dieAt  = Time.time + life;
        _armed  = false;
        _spent  = false;

        if (sr != null) sr.color = armingTint;
        transform.localScale = Vector3.one;
    }

    private void Update()
    {
        if (_spent) return;

        if (!_armed)
        {
            if (Time.time < _armAt) return;
            _armed = true;
            if (sr != null) sr.color = _baseColor;
        }

        // 수명이 다하면 조용히 걷는다 — 터지지 않는다.
        if (Time.time >= _dieAt) { Recall(); return; }

        // 깜빡임 — "여기 켜져 있다"를 알린다. 안 하면 바닥 무늬와 구분이 안 된다.
        if (sr != null && blinkInterval > 0f)
        {
            float k = Mathf.PingPong(Time.time / blinkInterval, 1f);
            var c = _baseColor; c.a = Mathf.Lerp(0.55f, 1f, k);
            sr.color = c;
        }

        // 🔴 적 레이어만 본다. 트리거를 달면 플레이어·발사체까지 들어온다.
        var hit = Physics2D.OverlapCircle(transform.position, _trigger, LayerMask.GetMask("Enemy"));
        if (hit == null) return;
        var e = hit.GetComponent<EnemyBase>();
        if (e == null || e.Dead) return;

        Detonate();
    }

    private void Detonate()
    {
        if (_spent) return;
        _spent = true;

        // 🔵 폭발은 AoeProjectile 을 그대로 빌려 쓴다 (새로 만들지 않았다).
        if (_explosion != null && _pool != null)
        {
            var go = _pool.Get(_explosion, transform.position, Quaternion.identity);
            var proj = go.GetComponent<AoeProjectile>();
            if (proj != null) proj.Initialize(_damage, _radius, _pool);
            else              _pool.Return(go);
        }
        AudioManager.Play(SfxId.Explosion);
        Release();
    }

    /// <summary>터뜨리지 않고 걷는다 (수명 만료 · 개수 상한 · 무기 환불).</summary>
    public void Recall()
    {
        if (_spent) return;
        _spent = true;
        Release();
    }

    private void Release()
    {
        if (_owner != null) _owner.Forget(this);
        if (sr != null) sr.color = _baseColor;
        if (_pool != null) _pool.Return(gameObject);
        else               gameObject.SetActive(false);
    }
}
