using System.Collections;
using UnityEngine;

// ────────────────────────────────────────────────────────────────────────────
//  PulseFx  —  퍼져 나가는 파동 연출 (승급 / 레벨업)
// ────────────────────────────────────────────────────────────────────────────
/// <summary>
/// 링 여러 겹이 시차를 두고 퍼지면서 사라지고, 가운데에서 별 모양이 한 번 터진다.
///
/// <para>🔑 <b>승급과 레벨업이 같은 스크립트·같은 그림을 쓴다</b> — 다른 건 색과 크기뿐이다(C34).
/// 흰색 스프라이트 2장(<c>Fx_ShockRing</c> · <c>Fx_Burst</c>)을 프리팹이 틴트한다.
/// UI 프레임·예고 원과 같은 방식이다.</para>
///
/// <para>🔴 <b>스스로 사라져야 한다.</b> <see cref="HUDManager"/> 의 레벨업 경로가
/// 풀이 아니라 <c>Instantiate</c> 로 부르기 때문이다(<c>HUDManager.cs:290</c>).
/// 풀에서 꺼내 쓰는 경우를 위해 <see cref="SetPool"/> 도 둔다.</para>
///
/// <para>스프라이트 규약은 <c>BossSlamRing</c> 과 같다 — <b>scale 1 에서 반경 0.5 유닛.</b>
/// 그래서 반경 <c>r</c> 로 그리려면 <c>localScale = r / 0.5</c> 다.</para>
///
/// <para>🔴 <b><c>Time.unscaledDeltaTime</c> 을 쓴다.</b> 이 연출이 뜨는 두 순간이
/// <b>둘 다 게임이 멈춰 있는 때</b>이기 때문이다 — 레벨업은 <c>GameState.LevelUp</c> 이
/// <c>timeScale = 0</c> 으로 만들고, 승급도 레벨업 카드 경로면 같은 상태다.
/// <c>Time.deltaTime</c> 을 쓰면 <b>얼어붙은 링이 패널 뒤에 그대로 붙어 있다</b>
/// (D41 검증 중 실제로 그렇게 나왔다 — scale 0.30 · alpha 1.00 에서 정지).</para>
///
/// <para>⚠️ 다만 <b>화면 흔들림은 일시정지 중에는 안 난다</b> —
/// <see cref="CameraController"/> 가 <c>Time.deltaTime &lt;= 0</c> 이면 통째로 조기 반환한다
/// (<c>CameraController.cs:84</c>). 흔들림이 실제로 보이는 건 <b>제단(E 키) 승급</b>처럼
/// 게임이 도는 중에 일어나는 경로뿐이다. 그건 카메라 쪽 설계라 여기서 안 건드린다.</para>
/// </summary>
public class PulseFx : MonoBehaviour
{
    // 🔴 컴파일 반영 확인용 (D27).
    public const int Version = 2;   // 2 = unscaledDeltaTime (D41 검증 중 발견)

    [Header("그림")]
    [Tooltip("퍼져 나가는 링. 배열 순서대로 시차를 두고 출발한다.")]
    [SerializeField] private SpriteRenderer[] rings;

    [Tooltip("가운데에서 한 번 터지는 별. 없어도 된다.")]
    [SerializeField] private SpriteRenderer burst;

    [Header("색 — 승급은 금색, 레벨업은 청록")]
    [SerializeField] private Color tint = new(1f, 0.82f, 0.35f, 1f);

    [Header("크기 · 시간")]
    [Tooltip("scale 1 일 때 스프라이트가 덮는 반경(월드 유닛). BossSlamRing 과 같은 0.5 규약.")]
    [SerializeField] private float spriteRadiusAtScaleOne = 0.5f;

    [Tooltip("링이 끝까지 퍼졌을 때의 반경(월드 유닛).")]
    [SerializeField] private float maxRadius = 2.6f;

    [Tooltip("링 하나가 퍼지는 데 걸리는 시간.")]
    [SerializeField] private float ringDuration = 0.55f;

    [Tooltip("링 사이의 출발 간격. 0 이면 세 겹이 겹쳐 보여 한 겹처럼 된다.")]
    [SerializeField] private float ringDelay = 0.11f;

    [Tooltip("별이 커지는 최종 배율. 링과 달리 반경 규약을 안 따른다 — 그냥 장식이다.")]
    [SerializeField] private float burstScale = 2.2f;

    [Tooltip("별이 뜨고 사라지는 시간.")]
    [SerializeField] private float burstDuration = 0.30f;

    [Header("화면 흔들림 — 0 이면 안 흔든다")]
    [Tooltip("보스 등장이 0.55/0.7, 엘리트 처치가 0.20/0.25 다. 그 사이 어디쯤인지는 플레이 판정 → TUNING.md")]
    [SerializeField] private float shakeAmplitude;
    [SerializeField] private float shakeDuration;

    private ObjectPool _pool;

    /// <summary>풀에서 꺼내 쓸 때만 부른다. 안 부르면 끝나고 스스로 <c>Destroy</c> 한다.</summary>
    public void SetPool(ObjectPool pool) => _pool = pool;

    private void OnEnable()
    {
        StopAllCoroutines();
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        if (shakeAmplitude > 0f && shakeDuration > 0f)
        {
            // EnemyBase.PlayDeathImpact · BossSlam 과 같은 경로로 잡는다 (씬 전체 순회 안 한다).
            var cam = Camera.main != null ? Camera.main.GetComponent<CameraController>() : null;
            if (cam != null) cam.Shake(shakeAmplitude, shakeDuration);
        }

        int    n     = rings != null ? rings.Length : 0;
        float  total = n > 0 ? ringDuration + ringDelay * (n - 1) : 0f;
        if (burst != null) total = Mathf.Max(total, burstDuration);

        // 시작 상태 — 여기서 안 지우면 풀에서 재사용될 때 지난번 끝 상태가 한 프레임 보인다.
        for (int i = 0; i < n; i++)
        {
            if (rings[i] == null) continue;
            rings[i].color            = new Color(tint.r, tint.g, tint.b, 0f);
            rings[i].transform.localScale = Vector3.zero;
        }
        if (burst != null)
        {
            burst.color            = new Color(tint.r, tint.g, tint.b, 0f);
            burst.transform.localScale = Vector3.zero;
        }

        float t = 0f;
        while (t < total)
        {
            t += Time.unscaledDeltaTime;

            for (int i = 0; i < n; i++)
            {
                var sr = rings[i];
                if (sr == null) continue;

                float k = (t - ringDelay * i) / ringDuration;
                if (k < 0f) { sr.color = new Color(tint.r, tint.g, tint.b, 0f); continue; }
                k = Mathf.Clamp01(k);

                // 처음이 빠르고 끝이 느리다 — 충격파는 감속하는 게 자연스럽다.
                float eased  = 1f - (1f - k) * (1f - k);
                float radius = Mathf.Lerp(0.15f, maxRadius, eased);

                sr.transform.localScale = Vector3.one * (radius / Mathf.Max(0.01f, spriteRadiusAtScaleOne));
                sr.color = new Color(tint.r, tint.g, tint.b, tint.a * (1f - k));
            }

            if (burst != null)
            {
                float k = Mathf.Clamp01(t / Mathf.Max(0.01f, burstDuration));
                burst.transform.localScale = Vector3.one * Mathf.Lerp(0.2f, burstScale, k);
                // 앞쪽 30% 에 확 떴다가 나머지에서 사라진다.
                float a = k < 0.3f ? k / 0.3f : 1f - (k - 0.3f) / 0.7f;
                burst.color = new Color(tint.r, tint.g, tint.b, tint.a * Mathf.Clamp01(a));
            }

            yield return null;
        }

        if (_pool != null) _pool.Return(gameObject);
        else               Destroy(gameObject);
    }
}
