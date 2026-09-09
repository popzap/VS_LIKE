using UnityEngine;

/// <summary>
/// <b>석상이 깨어날 때 퍼지는 고리</b> (D124 · 사용자 요구 *"석상 상호작용시 효과 이펙트나 소리 추가"*).
///
/// <para>🔑 <b>눌렀다는 사실이 화면에 남아야 한다.</b> `D123` 은 누르면 엘리트가 <b>조용히</b> 나왔다 —
/// 소리도 빌린 것이었고 화면에는 아무 일도 안 일어나서, 방금 무엇을 산 건지 알기 어려웠다.</para>
///
/// <para>🔵 <b>새 그림을 안 만들었다.</b> <see cref="CampfireBuilding"/> 이 온기 반경을 그리는
/// 방식(<see cref="LineRenderer"/> 로 원을 그린다)을 그대로 쓴다 — 스프라이트시트를 굽는 것보다
/// 싸고, 반경·색·두께를 코드에서 바로 만질 수 있다.</para>
///
/// <para>🔴 셰이더는 <b>URP 2D 것</b>이라야 한다. 빌트인 <c>Sprites/Default</c> 는
/// <c>isSupported</c> 가 true 인데도 URP 2D 렌더러가 <b>한 픽셀도 안 그린다</b> (D111).</para>
///
/// <para>🔴 <b>스스로 지운다.</b> 풀에 넣지 않은 이유는 <b>판당 몇 번 안 나오기 때문</b>이다
/// (석상은 최대 2개이고 각각 한 번만 쓴다). 자주 나오는 것이었다면 풀에 넣어야 한다.</para>
/// </summary>
public class StatueAwakenFx : MonoBehaviour
{
    /// <summary>컴파일 반영 확인용 (D27).</summary>
    public const int Version = 1;

    private const int   Segments = 40;
    private const float Duration = 0.75f;
    private const float StartRadius = 0.35f;
    private const float EndRadius = 4.2f;
    private const float StartWidth = 0.30f;
    private const float EndWidth = 0.04f;

    private LineRenderer _ring;
    private float        _t;

    /// <summary>고리 하나를 그 자리에 띄운다.</summary>
    public static StatueAwakenFx Play(Vector2 at, Color color)
    {
        var go = new GameObject("StatueAwakenFx");
        go.transform.position = at;
        var fx = go.AddComponent<StatueAwakenFx>();
        fx.Build(color);
        return fx;
    }

    private void Build(Color color)
    {
        _ring = gameObject.AddComponent<LineRenderer>();
        _ring.useWorldSpace = false;
        _ring.loop          = true;
        _ring.positionCount = Segments;
        _ring.startWidth    = StartWidth;
        _ring.endWidth      = StartWidth;
        _ring.sortingOrder  = 6;          // 석상(0)·적 위에 그린다 — 알림이므로 가려지면 안 된다
        _ring.textureMode   = LineTextureMode.Stretch;
        _ring.alignment     = LineAlignment.View;
        _ring.startColor    = color;
        _ring.endColor      = color;

        var sh = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
              ?? Shader.Find("Sprites/Default");
        _ring.material = new Material(sh);

        Draw(StartRadius);
    }

    private void Update()
    {
        _t += Time.deltaTime;
        float k = Mathf.Clamp01(_t / Duration);

        // 🔵 빠르게 나갔다 느려진다 — 등속으로 퍼지면 물결이 아니라 원이 커지는 것으로 보인다.
        float ease = 1f - (1f - k) * (1f - k);
        Draw(Mathf.Lerp(StartRadius, EndRadius, ease));

        float w = Mathf.Lerp(StartWidth, EndWidth, ease);
        _ring.startWidth = w;
        _ring.endWidth   = w;

        var c = _ring.startColor;
        c.a = 1f - k;                      // 끝에서 완전히 사라진다
        _ring.startColor = c;
        _ring.endColor   = c;

        if (k >= 1f) Destroy(gameObject);
    }

    private void Draw(float radius)
    {
        for (int i = 0; i < Segments; i++)
        {
            float a = i / (float)Segments * Mathf.PI * 2f;
            // 🔵 세로를 눌러 바닥에 누운 원으로 보이게 한다 — 탑다운이라 완전한 원은 떠 보인다
            //    (공전체 D118 이 같은 이유로 wobble 을 쓴다).
            _ring.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius * 0.72f, 0f));
        }
    }
}
