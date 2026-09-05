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
/// </summary>
public class BossArrowGraphic : Graphic
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        var r = GetPixelAdjustedRect();
        float hx = r.width  * 0.5f;
        float hy = r.height * 0.5f;
        var c = color;

        // 꼭짓점이 +x 를 향한다. 뒤쪽 두 점은 살짝 안으로 넣어 뾰족함을 남긴다.
        var v = UIVertex.simpleVert;
        v.color = c;

        v.position = new Vector3(r.center.x + hx,        r.center.y,      0f); vh.AddVert(v);
        v.position = new Vector3(r.center.x - hx * 0.7f, r.center.y + hy, 0f); vh.AddVert(v);
        v.position = new Vector3(r.center.x - hx * 0.2f, r.center.y,      0f); vh.AddVert(v);
        v.position = new Vector3(r.center.x - hx * 0.7f, r.center.y - hy, 0f); vh.AddVert(v);

        vh.AddTriangle(0, 1, 2);
        vh.AddTriangle(0, 2, 3);
    }
}
