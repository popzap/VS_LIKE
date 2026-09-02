using System.Collections;
using UnityEngine;

/// <summary>
/// 보스의 내려찍기 — <b>예고 원이 먼저 뜨고, 그 다음에 터진다</b> (D31).
///
/// <para>🔴 <see cref="AoeProjectile"/> 을 쓰지 않는다. 그건 <b>플레이어 무기</b>라
/// <c>Enemy</c> 레이어만 때린다. 여기는 반대 방향이다.</para>
///
/// <para>🔑 <b>예고가 이 기술의 전부다.</b> 예고 없이 터지면 "피할 수 없는 피해"가 되고,
/// 그건 보스 패턴이 아니라 그냥 체력 깎기다. 그래서 <c>windup</c> 이 0 이면
/// 임포터가 경고를 찍고, 여기서도 최소값을 강제한다.</para>
///
/// <para>피해 판정은 <b>터지는 순간의 위치</b>로 한다 — 예고를 보고 빠져나갔으면 안 맞아야 한다.</para>
/// </summary>
public class BossSlam : MonoBehaviour
{
    [Tooltip("예고 원. 스프라이트는 아무 원형이나 된다 (지금은 ToxinField 를 붉게 틴트해 쓴다)")]
    [SerializeField] private SpriteRenderer telegraph;

    [Tooltip("scale 1 일 때 스프라이트가 덮는 반경(월드 유닛). 반경에 맞춰 자동 확대된다")]
    [SerializeField] private float spriteRadiusAtScaleOne = 0.5f;

    [Header("예고 색")]
    [SerializeField] private Color warnColor  = new(1f, 0.25f, 0.15f, 0.30f);
    [SerializeField] private Color burstColor = new(1f, 0.85f, 0.45f, 0.85f);

    [Tooltip("터진 뒤 잔상이 남는 시간")]
    [SerializeField] private float burstHold = 0.16f;

    private ObjectPool _pool;

    /// <param name="windup">예고 시간. 0 이하로 들어와도 최소 0.15초는 준다.</param>
    public void Initialize(float damage, float radius, float windup, ObjectPool pool)
    {
        _pool = pool;
        StopAllCoroutines();
        StartCoroutine(Run(damage, radius, Mathf.Max(0.15f, windup)));
    }

    private IEnumerator Run(float damage, float radius, float windup)
    {
        if (spriteRadiusAtScaleOne > 0f)
            transform.localScale = Vector3.one * (radius / spriteRadiusAtScaleOne);

        // ── 예고 ──
        // 알파를 키우며 "차오르는" 느낌을 준다. 크기를 키우면 실제 피해 반경과
        // 눈에 보이는 반경이 달라져 플레이어가 속는다 — 크기는 처음부터 최종값이다.
        float t = 0f;
        while (t < windup)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / windup);
            if (telegraph != null)
                telegraph.color = new Color(warnColor.r, warnColor.g, warnColor.b,
                                            Mathf.Lerp(warnColor.a * 0.35f, warnColor.a, k));
            yield return null;
        }

        // ── 폭발 ──
        AudioManager.Play(SfxId.Explosion);
        if (telegraph != null) telegraph.color = burstColor;

        // 🔴 지금 이 순간의 거리로 판정한다. 예고를 보고 나갔으면 안 맞는다.
        var player = PlayerStats.Current;
        if (player != null)
        {
            float d = Vector2.Distance(player.transform.position, transform.position);
            if (d <= radius) player.TryTakeHit(damage, transform.position);
        }

        // EnemyBase.PlayDeathImpact 과 같은 경로로 잡는다 (씬 전체 순회 안 한다).
        var cam = Camera.main != null ? Camera.main.GetComponent<CameraController>() : null;
        if (cam != null) cam.Shake(0.25f, 0.18f);

        yield return new WaitForSeconds(burstHold);

        if (_pool != null) _pool.Return(gameObject);
        else               gameObject.SetActive(false);
    }
}
