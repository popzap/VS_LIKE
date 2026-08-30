using UnityEngine;

/// <summary>
/// 바닥에 <b>장판</b>을 까는 무기. 독 장판(Toxin)이 쓴다.
///
/// <para><see cref="AoeWeapon"/> 과 노리는 곳이 다르다 — 저쪽은 <i>가장 가까운 적</i>이고,
/// 이쪽은 <i>플레이어 주변 무작위 좌표</i>다. 장판은 지금 적이 있는 자리가 아니라
/// 적이 <b>지나갈</b> 자리를 막는 것이라, 적을 조준하면 오히려 쓸모가 없다.</para>
///
/// <para>그래서 <see cref="WeaponData.Range"/> 는 사거리가 아니라
/// <b>장판이 떨어질 수 있는 반경</b>으로 읽는다.</para>
/// </summary>
public class FieldWeapon : WeaponBase
{
    // 장판 그림이 이 반경에 맞춰 커진다(ToxinField.spriteRadiusAtScaleOne).
    [Tooltip("장판 하나가 덮는 반경(월드 유닛). ProjectileSize 배율이 곱해진다.")]
    [SerializeField] private float fieldRadius = 1.2f;

    protected override void Fire()
    {
        if (Data.ProjectilePrefab == null) return;

        // 적을 조준하지 않는다. 주변 아무 데나 깐다.
        Vector2 spot = (Vector2)transform.position + Random.insideUnitCircle * Data.GetRange(Level);

        var go    = Pool.Get(Data.ProjectilePrefab, spot, Quaternion.identity);
        var field = go.GetComponent<ToxinField>();
        if (field == null) { Pool.Return(go); return; }

        // 데미지는 "틱당" 이다. 장판이 사는 동안 여러 번 들어간다.
        field.Initialize(CalculateDamage(), fieldRadius * Data.GetProjectileSize(Level), Pool);
        AudioManager.Play(SfxId.ToxinSpill);
    }
}
