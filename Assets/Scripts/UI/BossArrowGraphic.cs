using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 삼각형 하나를 그리는 UI 그래픽 (D66). <see cref="BossOffscreenArrowUI"/> 만 쓴다.
///
/// <para>🔑 <b>스프라이트도 폰트 글리프도 안 쓴다.</b> 화살표 그림을 새로 만들면 애셋이 늘고,
/// 폰트 기호(<c>▲</c>)를 쓰면 <b>Static 115자 아틀라스에 없어 빈칸으로 나온다</b> (<c>I-60</c>·<c>B4</c>).
/// 정점 3개면 되는 모양이라 <see cref="Graphic"/> 로 직접 그린다 — 어느 해상도에서도 안 뭉갠다.</para>
///
/// <para>🔴 <b>파일 이름과 클래스 이름을 맞춰 두었다.</b> 다른 파일에 얹으면
/// <c>MonoScript</c> 가 안 붙어서 <c>AddComponent</c> 가 실패한다 (<c>I-19</c> 와 같은 함정).</para>
///
/// <para>모양은 <b>+x(오른쪽)를 가리키는</b> 삼각형이다. 방향은 부모가
/// <c>rectTransform.rotation</c> 으로 돌린다 — 여기서 각도를 알 필요가 없다.</para>
/// <para>🔴 <b><c>CanvasRenderer</c> 를 직접 요구한다</b> (D104 · 사용자가 세 번 지적한 건의 진짜 원인).
/// <c>Graphic</c> 이 이미 같은 속성을 달고 있지만, <b>파생 클래스를 <c>AddComponent</c> 할 때는 그게 안 따라왔다.</b>
/// 그 결과 화살표는 <b>켜지고 · 위치도 각도도 정확한데 한 픽셀도 안 그려졌다</b> —
/// <c>activeSelf</c> 만 본 검증은 전부 통과했다. 버텍스를 담을 <c>CanvasRenderer</c> 가 없으면
/// <c>Graphic</c> 은 <b>조용히 아무것도 안 한다.</b></para>
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public class BossArrowGraphic : Graphic
{
    /// <summary>
    /// 외곽선 두께(캔버스px). 0 이면 안 그린다 (<c>B21</c>).
    ///
    /// <para>🔴 <b>화살표가 보스 체력 바에 묻혔다</b> — 둘 다 빨강이라 윗부분이 섞였다.
    /// 색을 바꾸는 건 CONTENT 판단이라(<c>요청-61</c>) 건드리지 않고,
    /// <b>어떤 배경 위에서도 윤곽이 남도록</b> 어두운 테두리를 깐다.
    /// 붉은색이 지고 있는 "위험" 이라는 뜻도 그대로 남는다.</para>
    /// </summary>
    public float OutlineWidth { get; set; }

    /// <summary>외곽선 색. 알파는 <see cref="Graphic.color"/> 의 맥동을 따라간다.</summary>
    public Color OutlineColor { get; set; } = new(0f, 0f, 0f, 0.85f);

    /// <summary>
    /// 외곽선까지 포함한 배율. <see cref="BossOffscreenArrowUI"/> 가 <b>테두리 여유를 계산할 때</b>
    /// 쓴다 — 외곽선은 <c>rect</c> 밖으로 나가므로 이걸 빼먹으면 <c>B20</c> 이 다시 열린다.
    /// </summary>
    public float OutlineScale(Rect r) =>
        OutlineWidth <= 0f ? 1f : 1f + 2f * OutlineWidth / Mathf.Min(r.width, r.height);

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        var r = GetPixelAdjustedRect();

        // 🔑 외곽선을 <b>먼저</b> 넣는다. 같은 메시 안에서는 나중 삼각형이 위에 그려지므로
        //    이 순서 하나로 "뒤에 깔린 테두리"가 된다 (오브젝트를 하나 더 만들 필요가 없다).
        if (OutlineWidth > 0f)
        {
            var oc = OutlineColor;
            oc.a *= color.a;                       // 맥동을 같이 탄다
            Emit(vh, r, OutlineScale(r), oc);
        }
        Emit(vh, r, 1f, color);
    }

    /// <summary>꼭짓점이 +x 를 향하는 삼각형 둘. 방향은 부모가 회전으로 준다.</summary>
    private static void Emit(VertexHelper vh, Rect r, float s, Color c)
    {
        float hx = r.width  * 0.5f * s;
        float hy = r.height * 0.5f * s;

        int i0 = vh.currentVertCount;
        var v = UIVertex.simpleVert;
        v.color = c;

        // 뒤쪽 두 점은 살짝 안으로 넣어 뾰족함을 남긴다.
        v.position = new Vector3(r.center.x + hx,        r.center.y,      0f); vh.AddVert(v);
        v.position = new Vector3(r.center.x - hx * 0.7f, r.center.y + hy, 0f); vh.AddVert(v);
        v.position = new Vector3(r.center.x - hx * 0.2f, r.center.y,      0f); vh.AddVert(v);
        v.position = new Vector3(r.center.x - hx * 0.7f, r.center.y - hy, 0f); vh.AddVert(v);

        vh.AddTriangle(i0 + 0, i0 + 1, i0 + 2);
        vh.AddTriangle(i0 + 0, i0 + 2, i0 + 3);
    }
}
