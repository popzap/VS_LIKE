using UnityEngine;

/// <summary>
/// <b>블링크 쿨타임을 발밑에 그린다</b> (D126 · 사용자 요구
/// *"블링크 패시브 (점멸 쿨타임 캐릭터 발 밑에 게이지로 표시)"*).
///
/// <para>🔑 <b>블링크는 이 게임에서 유일하게 "저절로 일어나는" 기술이다.</b>
/// 무기는 눈에 보이는 것을 쏘고 건물은 내가 세우는데, 블링크는 <b>아무 예고 없이</b>
/// 나를 5 유닛 옮긴다(<c>D113</c>). 언제 튈지 모르면 그건 기술이 아니라 사고다 —
/// 이 게이지가 <b>"곧 튄다"</b>를 알려 주는 유일한 신호다.</para>
///
/// <para>🔵 <b>HUD 가 아니라 발밑</b>이다. 화면 구석에 두면 <b>전투 중에는 아무도 안 본다</b> —
/// 뱀서라이크에서 눈은 캐릭터 주변에 붙어 있다(<c>D82</c> 가 건물 안내를 화면 가운데 아래로
/// 옮긴 것과 같은 이유다).</para>
///
/// <para>🔵 <b>스프라이트를 안 만들었다.</b> <see cref="LineRenderer"/> 두 줄(바탕·채움)이면 된다 —
/// <see cref="StatueAwakenFx"/>·<see cref="CampfireBuilding"/> 이 쓰는 그 방식이다.
/// 🔴 셰이더는 <b>URP 2D 것</b>이라야 한다(<c>D111</c>).</para>
///
/// <para>🔴 <b>블링크가 없으면 아예 안 그린다.</b> 패시브를 안 골랐는데 빈 게이지가 붙어 있으면
/// *"내가 뭘 못 채우고 있나"* 로 읽힌다.</para>
/// </summary>
[RequireComponent(typeof(PlayerTeleport))]
public class BlinkGauge : MonoBehaviour
{
    /// <summary>컴파일 반영 확인용 (D27).</summary>
    public const int Version = 1;

    [Header("모양")]
    [Tooltip("게이지 가로 길이(유닛). 플레이어 그림 폭이 약 1 이다.")]
    [SerializeField] private float width = 0.9f;

    [Tooltip("두께(유닛).")]
    [SerializeField] private float thickness = 0.1f;

    [Tooltip("발밑으로 얼마나 내릴지(유닛). 🔴 너무 붙이면 발을 가리고 너무 내리면 딴 것으로 보인다.")]
    [SerializeField] private float yOffset = -0.62f;

    [SerializeField] private Color emptyColor = new(0.08f, 0.10f, 0.14f, 0.75f);
    [SerializeField] private Color fillColor  = new(0.35f, 0.85f, 1f, 0.95f);

    [Tooltip("다 찼을 때의 색 — 🔵 '지금 튄다'를 한 번 더 알려 준다.")]
    [SerializeField] private Color readyColor = new(1f, 0.95f, 0.55f, 1f);

    [SerializeField] private int sortingOrder = 5;

    private PlayerTeleport _blink;
    private LineRenderer   _back;
    private LineRenderer   _fill;

    /// <summary>지금 게이지가 화면에 있나 (검증용).</summary>
    public bool Showing => _back != null && _back.enabled;

    /// <summary>지금 채워진 비율 (검증용).</summary>
    public float Shown01 { get; private set; }

    private void Awake()
    {
        _blink = GetComponent<PlayerTeleport>();
        _back  = Build("BlinkGauge_Back", emptyColor, sortingOrder);
        _fill  = Build("BlinkGauge_Fill", fillColor,  sortingOrder + 1);
    }

    private LineRenderer Build(string name, Color color, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;

        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace  = false;
        lr.positionCount  = 2;
        lr.startWidth     = thickness;
        lr.endWidth       = thickness;
        lr.sortingOrder   = order;
        lr.textureMode    = LineTextureMode.Stretch;
        lr.alignment      = LineAlignment.View;
        lr.startColor     = color;
        lr.endColor       = color;
        lr.numCapVertices = 2;

        var sh = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
              ?? Shader.Find("Sprites/Default");
        lr.material = new Material(sh);
        return lr;
    }

    /// <summary>
    /// 🔵 <c>LateUpdate</c> 다 — 플레이어가 움직인 <b>뒤</b>에 자리를 잡아야 게이지가 뒤늦게 끌려오지 않는다.
    /// (부모에 붙어 있어 위치는 따라오지만, <b>블링크로 순간이동한 프레임</b>에 어긋나 보인다.)
    /// </summary>
    private void LateUpdate()
    {
        if (_blink == null || _back == null || _fill == null) return;

        var gm = GameManager.Instance;
        bool show = _blink.HasBlink
                 && gm != null && gm.CurrentState == GameState.Wave;

        if (!show)
        {
            if (_back.enabled) { _back.enabled = false; _fill.enabled = false; }
            Shown01 = 0f;
            return;
        }
        if (!_back.enabled) { _back.enabled = true; _fill.enabled = true; }

        float half = width * 0.5f;
        float y    = yOffset;

        _back.SetPosition(0, new Vector3(-half, y, 0f));
        _back.SetPosition(1, new Vector3( half, y, 0f));

        float k = Mathf.Clamp01(_blink.Charge01);
        Shown01 = k;

        // 🔴 0 일 때 두 점을 같은 자리에 두면 <c>numCapVertices</c> 가 <b>점 하나를 남긴다.</b>
        //    아주 조금이라도 차야 보이게 한다 — 안 그러면 "다 찬 것처럼 보이는 점"이 남는다.
        float end = -half + Mathf.Max(0.0001f, width * k);
        _fill.enabled = k > 0.01f;
        _fill.SetPosition(0, new Vector3(-half, y, 0f));
        _fill.SetPosition(1, new Vector3(end,   y, 0f));

        var c = k >= 0.999f ? readyColor : fillColor;
        _fill.startColor = c;
        _fill.endColor   = c;
    }
}
