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

    // ── 직사각형 예고 (D83) ──────────────────────────────────────
    //
    // 🔴 <b>애셋을 만들지 않는다.</b> 1x1 흰 사각형은 `Texture2D.whiteTexture` 로 충분하고,
    //    새 png 을 넣으면 CONTENT 소유 경로를 건드리게 된다.
    private static Sprite _boxSprite;

    // 🔴 풀에서 재사용되므로 원래 원형 스프라이트를 기억해 뒀다가 되돌린다.
    //    안 그러면 사각형으로 한 번 쓴 오브젝트가 다음에 원형 내려찍기로 나올 때
    //    <b>사각형인 채로</b> 뜬다.
    private Sprite _circleSprite;
    private bool   _spriteCached;

    private static Sprite BoxSprite
    {
        get
        {
            if (_boxSprite == null)
                _boxSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f),
                                           new Vector2(0.5f, 0.5f), 1f);
            return _boxSprite;
        }
    }

    private void CacheCircleSprite()
    {
        if (_spriteCached || telegraph == null) return;
        _circleSprite = telegraph.sprite;
        _spriteCached = true;
    }

    /// <param name="windup">예고 시간. 0 이하로 들어와도 최소 0.15초는 준다.</param>
    public void Initialize(float damage, float radius, float windup, ObjectPool pool)
    {
        _pool = pool;
        CacheCircleSprite();
        StopAllCoroutines();
        StartCoroutine(Run(damage, radius, Mathf.Max(0.15f, windup)));
    }

    /// <summary>
    /// <b>직사각형</b> 예고 (D83 · 사용자 요구).
    ///
    /// <para>사용자 판정: *"일직선으로 안 보인다 (그냥 원 여러 개)"* → *"원 여러개를 쓰지 말고
    /// 직사각형의 범위로 새로 만들어"*. <c>D73</c> 은 <see cref="BossSlam"/> 원을 줄 세워
    /// 일직선을 만들었는데, 원 7개는 <b>줄 지어 있어도 하나의 띠로 안 읽힌다</b>.</para>
    ///
    /// <para>🔑 판정도 같이 사각형이 된다 — 예전에는 원 7개 각각이 따로 판정해서
    /// <b>원과 원 사이(오목한 자리)가 안전지대</b>였다. 보이는 모양과 맞는 판정이 된다.</para>
    /// </summary>
    public void InitializeBox(float damage, float length, float width, float angleDeg, float windup, ObjectPool pool)
    {
        _pool = pool;
        CacheCircleSprite();
        StopAllCoroutines();
        StartCoroutine(RunBox(damage, Mathf.Max(0.1f, length), Mathf.Max(0.1f, width),
                              angleDeg, Mathf.Max(0.15f, windup)));
    }

    private IEnumerator RunBox(float damage, float length, float width, float angleDeg, float windup)
    {
        if (telegraph != null) telegraph.sprite = BoxSprite;
        transform.rotation   = Quaternion.Euler(0f, 0f, angleDeg);
        transform.localScale = new Vector3(length, width, 1f);   // 1x1 스프라이트라 배율이 곧 크기다

        yield return Telegraph(windup);

        AudioManager.Play(SfxId.Explosion);
        if (telegraph != null) telegraph.color = burstColor;

        // 🔴 회전을 고려한 사각형 판정. 로컬 좌표로 되돌리면 배율까지 풀리므로
        //    -0.5 ~ +0.5 안에 있는지만 보면 된다.
        var player = PlayerStats.Current;
        if (player != null)
        {
            Vector3 local = transform.InverseTransformPoint(player.transform.position);
            if (Mathf.Abs(local.x) <= 0.5f && Mathf.Abs(local.y) <= 0.5f)
                player.TryTakeHit(damage, transform.position);
        }

        var cam = Camera.main != null ? Camera.main.GetComponent<CameraController>() : null;
        if (cam != null) cam.Shake(0.25f, 0.18f);

        yield return new WaitForSeconds(burstHold);

        // 🔴 다음 사용을 위해 원형으로 되돌린다 (풀 재사용).
        if (telegraph != null && _circleSprite != null) telegraph.sprite = _circleSprite;
        transform.rotation = Quaternion.identity;

        if (_pool != null) _pool.Return(gameObject);
        else               gameObject.SetActive(false);
    }

    /// <summary>알파를 키우며 "차오르는" 예고. 원형·사각형이 같이 쓴다.</summary>
    private IEnumerator Telegraph(float windup)
    {
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
    }

    private IEnumerator Run(float damage, float radius, float windup)
    {
        // 🔴 사각형으로 쓰였던 오브젝트가 풀에서 돌아올 수 있다. 원형으로 되돌린다.
        if (telegraph != null && _circleSprite != null) telegraph.sprite = _circleSprite;
        transform.rotation = Quaternion.identity;

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
