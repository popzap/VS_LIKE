using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 소환수 무기. 다른 무기와 달리 <b>판정 기준점이 플레이어가 아니라 "몸통"</b>이다.
///
/// <para>이 컴포넌트 자체는 눈에 보이지 않는 로직 오브젝트다. <see cref="WeaponManager"/> 가
/// 플레이어 밑에 붙여 주고, 여기서 <see cref="bodyPrefab"/> 을 씬에 따로 낳는다.</para>
///
/// <para>🔴 <b>몸통을 자식으로 붙이지 않는다.</b> 붙이면 플레이어에 뻣뻣하게 매달려 다녀서
/// "따라온다"는 느낌이 죽는다. 대신 <see cref="LateUpdate"/> 에서 목표점을 향해 Lerp 한다.</para>
///
/// <para>몸통에는 <b>HP·Collider·Rigidbody2D 가 없다.</b> 적이 아니라서 맞지도 막지도 않고,
/// 강체가 없어야 넉백에 휩쓸려 날아가지 않는다.</para>
/// </summary>
public class SummonWeapon : WeaponBase
{
    /// <summary>드래곤은 화염구를 쏘고(<see cref="Ranged"/>), 문어는 제자리에서 360° 를 후린다(<see cref="Ring"/>).</summary>
    public enum AttackMode { Ranged, Ring }

    [Header("몸통")]
    [Tooltip("씬에 낳을 소환수 몸통. Collider·Rigidbody2D 가 없어야 한다.")]
    [SerializeField] private GameObject bodyPrefab;
    [Tooltip("⚠️ 물러날 때만 쓴다 (D19). 길 기록이 없을 때 설 방향(도). " +
             "평소에는 PlayerTrail 을 따라가므로 이 값은 안 쓰인다.")]
    [SerializeField] private float offsetAngle = 90f;
    [Tooltip("기차에서 내 <b>앞칸과의 간격</b>(유닛). 맨 앞이면 플레이어와의 간격이다. " +
             "합이 PlayerTrail.maxLength 를 넘으면 뒤쪽이 곧게 늘어진다.")]
    [SerializeField] private float offsetDistance = 1.2f;
    [Tooltip("따라붙는 빠르기. 클수록 찰싹 붙고, 작을수록 굼뜨게 끌려온다.")]
    [SerializeField] private float followLerp = 6f;

    [Header("공격")]
    [SerializeField] private AttackMode mode = AttackMode.Ranged;
    [Tooltip("Ranged 전용. 착탄 폭발의 반경(유닛). AoeWeapon 과 같은 기본값이다.")]
    [SerializeField] private float explosionRadius = 2f;
    [Tooltip("Ring 전용. 후리기 시작해서 피해가 들어가기까지. TentacleLash 가 30fps 이고 " +
             "촉수가 가장 멀리 뻗는 건 프레임 3 이라 0.1 = 그 순간이다.")]
    [SerializeField] private float lashHitDelay = 0.1f;

    /// <summary>
    /// 이 무기가 낳은 몸통들 (D113). <b>예전에는 하나였다</b> —
    /// "소환수 개수 증가" 패시브(<see cref="StatBlock.SummonCount"/>) 때문에 목록이 됐다.
    ///
    /// <para>🔴 <b>0번이 특별하지 않다.</b> 전부 같은 자격으로 줄에 서고 각자 공격한다 —
    /// 하나만 "진짜"로 두면 죽거나 사라질 때 특수 처리가 생긴다.</para>
    /// </summary>
    private readonly List<GameObject>   _bodies  = new();
    private readonly List<SummonVisual> _visuals = new();

    /// <summary>지금 있어야 할 몸통 수. 패시브를 판 도중에 얻을 수 있으므로 매 프레임 다시 읽는다.</summary>
    private int DesiredBodyCount
        => 1 + Mathf.Max(0, Mathf.RoundToInt(OwnerStats != null ? OwnerStats.Final.SummonCount : 0f));

    // ── 기차 ────────────────────────────────────────────────────
    //
    // 소환수는 플레이어 둘레를 공전하지 않는다. 플레이어가 지나온 길을 되짚어 줄을 선다 (D19).
    // 🔴 순서는 "얻은 순서"다 — 먼저 얻은 놈이 앞칸, 나중에 얻은 놈이 뒤칸.
    //    그래서 소환수를 더 모을수록 줄이 뒤로 길어진다.
    private static readonly List<SummonWeapon> Train = new();

    /// <summary>
    /// 내가 플레이어에게서 길을 따라 몇 유닛 뒤에 서야 하나 — <b>앞칸들의 간격을 전부 더한 값</b>.
    /// 칸마다 <see cref="offsetDistance"/> 가 다를 수 있으므로 곱셈이 아니라 누적 합이다.
    /// </summary>
    private float TrainBackDistance()
    {
        float d = 0f;
        for (int i = 0; i < Train.Count; i++)
        {
            var car = Train[i];
            // 도메인 리로드를 끄고 플레이하면 static 이 살아남아 파괴된 칸이 남는다.
            if (car == null) continue;

            // 🔴 한 무기가 몸통을 여럿 낳으므로 앞칸이 먹는 길이도 마릿수만큼이다 (D113).
            //    안 곱하면 앞 무기의 2번째 몸통과 내 1번째가 같은 자리에 겹친다.
            d += car.offsetDistance * car._bodies.Count;
            if (car == this) return d;
        }
        return d + offsetDistance;   // 아직 등록 전이면 맨 뒤로 친다
    }

    /// <summary>내 <c>i</c> 번째 몸통이 서야 할 길 위의 거리. 같은 무기 안에서도 한 칸씩 뒤로 선다.</summary>
    private float BackDistanceOf(int i) => TrainBackDistance() - offsetDistance * (_bodies.Count - 1 - i);

    // ── 생애 ────────────────────────────────────────────────────

    protected override void OnInitialized()
    {
        // 🔴 몸통보다 먼저 줄에 선다. 아래에서 FollowTarget() 으로 첫 자리를 잡는데,
        //    줄에 없으면 그 한 프레임 동안 엉뚱한 칸 자리에 태어난다.
        Train.RemoveAll(car => car == null);
        if (!Train.Contains(this)) Train.Add(this);

        if (bodyPrefab == null)
        {
            Debug.LogWarning($"[SummonWeapon] bodyPrefab 이 비었다 — {name}");
            return;
        }

        // 풀에서 재사용될 때 Initialize 가 다시 불린다. 이미 있는 몸통은 그대로 두고
        // 모자란 만큼만 채운다 — 무조건 낳으면 재사용 때마다 마릿수가 불어난다.
        SyncBodyCount();
    }

    /// <summary>
    /// 몸통 수를 <see cref="DesiredBodyCount"/> 에 맞춘다 (D113).
    ///
    /// <para>패시브는 <b>판 도중에</b> 얻고 올린다. 소환 시점에 한 번만 세면
    /// 이미 소환해 둔 드래곤은 영영 한 마리로 남는다 — 그래서 매 프레임 맞춘다.</para>
    ///
    /// <para>🔴 새로 낳은 몸통은 <b>줄의 제 자리에서</b> 태어나야 한다.
    /// 플레이어 발밑에 낳으면 한 프레임 동안 앞칸을 뚫고 나온 것처럼 보인다.</para>
    /// </summary>
    private void SyncBodyCount()
    {
        if (bodyPrefab == null || Pool == null) return;

        // 파괴된 몸통이 목록에 남아 있으면 마릿수가 틀어진다 (도메인 리로드·씬 전환).
        for (int i = _bodies.Count - 1; i >= 0; i--)
            if (_bodies[i] == null) { _bodies.RemoveAt(i); _visuals.RemoveAt(i); }

        int want = DesiredBodyCount;

        while (_bodies.Count > want)
        {
            int last = _bodies.Count - 1;
            if (_bodies[last] != null) Pool.Return(_bodies[last]);
            _bodies.RemoveAt(last);
            _visuals.RemoveAt(last);
        }

        while (_bodies.Count < want)
        {
            _bodies.Add(null); _visuals.Add(null);          // 자리부터 잡아야 BackDistanceOf 가 맞는다
            int i  = _bodies.Count - 1;
            var go = Pool.Get(bodyPrefab, FollowTarget(BackDistanceOf(i)), Quaternion.identity);
            _bodies[i]  = go;
            _visuals[i] = go.GetComponent<SummonVisual>();
        }
    }

    /// <summary>
    /// 🔴 이게 없으면 상점에서 무기를 환불했을 때 <b>소환수만 필드에 영원히 남는다.</b>
    /// <see cref="WeaponManager.RemoveWeapon"/> 이 무기를 풀에 넣으면
    /// <see cref="ObjectPool.Return"/> 의 <c>SetActive(false)</c> 가 곧 여기다 —
    /// 그래서 <see cref="WeaponBase"/> 에 새 훅을 팔 필요가 없다.
    /// </summary>
    private void OnDisable()
    {
        // 🔴 몸통 유무와 상관없이 줄에서 빠져야 한다. 남아 있으면 뒤칸들이
        //    있지도 않은 앞칸의 간격만큼 계속 뒤로 밀린다.
        Train.Remove(this);

        // 씬을 내릴 때도 여기가 불린다. 그때 풀은 이미 파괴돼 있을 수 있다.
        for (int i = 0; i < _bodies.Count; i++)
            if (_bodies[i] != null && Pool != null) Pool.Return(_bodies[i]);
        _bodies.Clear();
        _visuals.Clear();
    }

    // ── 따라다니기 ──────────────────────────────────────────────

    /// <summary>
    /// 🔴 <see cref="WeaponBase"/> 가 <c>Update</c> 로 쿨다운을 돌린다.
    /// 여기서 <c>Update</c> 를 선언하면 그걸 가려 <b>모든 소환수가 공격을 멈춘다.</b>
    /// 위치 보정은 어차피 플레이어가 움직인 뒤에 해야 맞으므로 <c>LateUpdate</c> 가 옳다.
    /// </summary>
    private void LateUpdate()
    {
        SyncBodyCount();          // 패시브를 판 도중에 얻을 수 있다 (D113)

        // 프레임률에 좌우되지 않는 감쇠. Lerp(t = lerp * dt) 로 쓰면 프레임이 튈 때 따라오는 속도가 변한다.
        float k = 1f - Mathf.Exp(-followLerp * Time.deltaTime);

        for (int i = 0; i < _bodies.Count; i++)
        {
            var body = _bodies[i];
            if (body == null) continue;

            Vector2 cur  = body.transform.position;
            Vector2 next = Vector2.Lerp(cur, FollowTarget(BackDistanceOf(i)), k);
            body.transform.position = next;

            if (_visuals[i] != null)
                _visuals[i].SetVelocity((next - cur) / Mathf.Max(Time.deltaTime, 1e-5f));
        }
    }

    /// <summary>
    /// 내가 서야 할 자리 — <b>플레이어가 지나온 길 위</b>의 한 점 (D19).
    ///
    /// <para>예전에는 고정 각도로 공전했다. 그러면 플레이어가 방향을 꺾을 때 소환수가
    /// <b>옆으로 미끄러져</b> 따라오는 느낌이 안 났고, 각도를 겹치지 않게 손으로 배분해야 했다.
    /// 길을 되짚으면 배분이 필요 없다 — <b>줄 순서만 있으면 자동으로 안 겹친다.</b></para>
    /// </summary>
    private Vector2 FollowTarget(float backDistance = -1f)
    {
        Transform owner = OwnerStats != null ? OwnerStats.transform : transform;
        if (backDistance < 0f) backDistance = TrainBackDistance();

        var trail = PlayerTrail.Of(owner);
        if (trail != null) return trail.SampleBack(backDistance);

        // 길을 못 얻는 경우(소유자가 없다)에만 옛 방식으로 물러난다.
        float rad = offsetAngle * Mathf.Deg2Rad;
        return (Vector2)owner.position + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * offsetDistance;
    }

    // ── 공격 ────────────────────────────────────────────────────

    /// <summary>
    /// 🔴 기본 구현은 <c>transform.position</c>(= 플레이어)에서 적을 찾는다.
    /// 소환수는 저 혼자 떨어져 있으므로 <b>몸통 자리</b>에서 찾아야 한다.
    /// </summary>
    protected override Transform FindNearestEnemy()
        => _bodies.Count > 0 && _bodies[0] != null
             ? NearestFrom(_bodies[0].transform.position)
             : base.FindNearestEnemy();

    /// <summary>몸통마다 사거리가 따로다 (D113) — 어느 자리에서 찾는지를 밖에서 정한다.</summary>
    private Transform NearestFrom(Vector2 origin)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, Data.GetRange(Level),
                            LayerMask.GetMask("Enemy"));
        if (hits.Length == 0) return null;

        Transform nearest = null;
        float minDist = float.MaxValue;
        foreach (var h in hits)
        {
            float d = Vector2.Distance(origin, h.transform.position);
            if (d < minDist) { minDist = d; nearest = h.transform; }
        }
        return nearest;
    }

    /// <summary>
    /// 🔴 <b>몸통마다 따로 쏜다</b> (D113). 대표 하나만 쏘면 "소환수 증가"가
    /// 화면에만 늘고 <b>세기는 그대로</b>라 패시브가 거짓말이 된다.
    /// </summary>
    protected override void Fire()
    {
        for (int i = 0; i < _bodies.Count; i++)
        {
            if (_bodies[i] == null) continue;
            if (mode == AttackMode.Ranged) FireRanged(_bodies[i]);
            else                           FireRing(_bodies[i]);
        }
    }

    /// <summary>드래곤 — 몸통 자리에서 화염구를 쏘고, 목표 지점에서 터진다.</summary>
    private void FireRanged(GameObject body)
    {
        var target = NearestFrom(body.transform.position);
        if (target == null) return;
        if (Data.TravelPrefab == null || Data.ProjectileSpeed <= 0f) return;

        // 목표는 좌표로 굳힌다. 날아가는 동안 적이 죽으면 그 Transform 은 풀에서
        // 재사용돼 엉뚱한 자리로 옮겨 간다 (AoeWeapon 과 같은 이유).
        Vector2 spot   = target.position;
        float   damage = CalculateDamage();
        float   radius = explosionRadius * Data.GetProjectileSize(Level);

        var go   = Pool.Get(Data.TravelPrefab, body.transform.position, Quaternion.identity);
        var bomb = go.GetComponent<BombProjectile>();
        if (bomb == null) { Pool.Return(go); return; }

        bomb.Initialize(spot, damage, radius, Data.ProjectileSpeed, Data.ProjectilePrefab, Pool);
        AudioManager.Play(SfxId.DragonSpit);
    }

    /// <summary>문어 — 사거리 안에 아무도 없으면 허공을 후리지 않는다.</summary>
    private void FireRing(GameObject body)
    {
        if (NearestFrom(body.transform.position) == null) return;
        StartCoroutine(Lash(body));
    }

    private IEnumerator Lash(GameObject body)
    {
        float range = Data.GetRange(Level);

        SpawnLash(body, range);
        // ⚠️ 클립은 "0.1초에 때리는 휘두르기"로 구워져 있다. lashHitDelay 를 바꾸면
        //    Tools/Audio/gen_tentacle_lash.py 의 HIT_AT 도 같이 바꿔 다시 구워야 한다.
        AudioManager.Play(SfxId.TentacleLash);

        // 🔴 피해는 후리는 동안 딱 한 번이다. 프레임마다 굴리면 몇 배가 된다 (MeleeWeapon 과 같다).
        yield return new WaitForSeconds(lashHitDelay);

        // 몸통이 사라졌을 수 있다 — 기다리는 사이에 무기가 환불됐거나 마릿수가 줄었다면.
        if (body == null || !body.activeInHierarchy) yield break;

        Vector2 origin = body.transform.position;
        float   dmg    = CalculateDamage();

        // 🔴 부채꼴이 아니라 링이다. 각도를 안 보므로 뒤쪽 적도 맞는다.
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, range, LayerMask.GetMask("Enemy"));
        foreach (var h in hits)
        {
            var enemy = h.GetComponent<EnemyBase>();
            // 넉백 기준점은 몸통 — 소환수 바깥으로 밀려난다.
            if (enemy != null) enemy.TakeDamage(dmg, origin);
        }
    }

    private void SpawnLash(GameObject body, float range)
    {
        if (Data.ProjectilePrefab == null || body == null) return;

        // 🔴 z 를 돌리지 않는다. 6프레임에 9°/프레임 회전이 이미 그려져 있다.
        var go = Pool.Get(Data.ProjectilePrefab, body.transform.position, Quaternion.identity);

        var fx = go.GetComponent<SwingArcFx>();
        // Initialize 를 안 부르면 촉수가 풀로 돌아가지 않고 화면에 그대로 남는다 (I-39).
        if (fx == null) { Pool.Return(go); return; }

        // 후리는 0.2초 동안에도 소환수는 움직인다. 붙여 두지 않으면 촉수만 뒤에 남는다.
        go.transform.SetParent(body.transform, true);
        fx.Initialize(range, Pool);
    }
}
