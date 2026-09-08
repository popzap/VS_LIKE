using UnityEngine;

/// <summary>
/// <b>기관총</b> — 빠르게 쏘지만 잘 안 맞는다 (D109 · 사용자 요구
/// "공격 속도는 빠르지만 명중률은 낮음").
///
/// <para>🔑 <b>기존 <see cref="ProjectileWeapon"/> 과 다른 점은 한 가지뿐이다 — 흩어지는 방식.</b>
/// 저쪽은 발사체 여러 개를 <b>정해진 각도로 부채꼴</b>로 편다(<c>-spread…+spread</c> 균등 분배).
/// 그건 산탄이지 <b>부정확</b>이 아니다 — 같은 상황에서 늘 같은 곳으로 간다.
/// 여기서는 <b>발사할 때마다 무작위</b>로 흔들어 <b>맞을 때도 있고 빗나갈 때도 있게</b> 한다.</para>
///
/// <para>🔴 <b>"빠름" 은 코드가 아니라 데이터다.</b> 연사는 <c>Weapons.csv</c> 의 <c>Cooldown</c> 을
/// 낮게 잡아 만든다 — 여기에 발사 로직을 따로 두지 않는다. 이 클래스가 하는 일은
/// <b>흩뜨리는 것 하나</b>이고, 그래서 <see cref="ProjectileWeapon"/> 를 그대로 물려받는다.</para>
///
/// <para>🔵 <b>레벨이 오르면 정확해진다.</b> 데미지만 올리면 "빠른 총"이 그냥 "센 총"이 되는데,
/// 이 무기의 정체성은 <b>명중률과 화력의 맞바꿈</b>이라 그쪽이 자라야 성장이 느껴진다.
/// <c>WeaponData</c> 에 각도 열이 없으므로 프리팹에서 잡는다 —
/// CSV 열을 늘리면 임포터·익스포터까지 같이 고쳐야 해서, <b>먼저 굴려 보고</b>
/// 값이 굳으면 그때 <c>Weapons.csv</c> 로 올린다.</para>
/// </summary>
public class MachineGunWeapon : ProjectileWeapon
{
    /// <summary>컴파일 반영 확인용 (D27).</summary>
    public const int Version = 1;

    [Header("퍼짐 (명중률)")]
    [Tooltip("Lv1 에서 좌우로 흔들리는 최대 각도(도). 클수록 안 맞는다.")]
    [SerializeField] private float spreadAtLv1 = 14f;

    [Tooltip("최고 레벨에서의 최대 각도(도). Lv1 보다 작아야 '레벨업 = 정확해짐' 이 된다.")]
    [SerializeField] private float spreadAtMaxLv = 5f;

    [Tooltip("퍼짐이 이 레벨에서 최소가 된다. Weapons.csv 의 레벨 수와 맞춘다.")]
    [SerializeField] private int maxLevel = 5;

    /// <summary>이번 발사에 쓸 최대 흔들림 각도. 레벨이 오를수록 줄어든다.</summary>
    private float CurrentSpread
    {
        get
        {
            if (maxLevel <= 1) return spreadAtMaxLv;
            float t = Mathf.Clamp01((Level - 1) / (float)(maxLevel - 1));
            return Mathf.Lerp(spreadAtLv1, spreadAtMaxLv, t);
        }
    }

    protected override void Fire()
    {
        var target = FindNearestEnemy();
        if (target == null) return;

        int   count  = ScaledProjectileCount();
        float spread = Mathf.Max(0f, CurrentSpread);
        Vector2 baseDir = ((Vector2)(target.position - transform.position)).normalized;

        for (int i = 0; i < count; i++)
        {
            // 🔑 균등 분배가 아니라 <b>발사체마다 독립 난수</b>다.
            //    두 발이 같은 곳으로 갈 수도, 정반대로 벌어질 수도 있다 — 그게 "부정확" 이다.
            float angle = Random.Range(-spread, spread);
            Vector2 dir = Quaternion.Euler(0f, 0f, angle) * baseDir;

            var go   = Pool.Get(Data.ProjectilePrefab, transform.position, Quaternion.identity);
            var proj = go.GetComponent<ProjectileBase>();
            proj.Initialize(dir, CalculateDamage(), Data.GetProjectileSize(Level),
                            Data.ProjectileSpeed, Data.GetRange(Level), Pool);
        }

        // 발사체 개수만큼 울리면 소리가 뭉개진다. 한 번만 낸다.
        // 🔵 연사가 빨라 AudioManager 의 중복 컷(0.04초)에 자주 걸린다 —
        //    그게 오히려 "따다다닥" 으로 뭉쳐 들려 기관총답다. 전용 SFX 가 오면 다시 본다.
        AudioManager.Play(SfxId.WeaponFire);
    }
}
