using UnityEngine;

/// <summary>
/// <b>아레나 경계</b> — 맵을 사각형으로 막는다 (D111 · 사용자 결정 "A + ①": 넓은 아레나 + 단단한 벽).
///
/// <para>🔑 <b>왜 막는가 — 카이팅의 모양을 바꾸려는 것이다.</b>
/// 무한 맵에서 도망은 <b>직선</b>이다. 한 방향으로 계속 달리면 다시는 그 자리로 안 돌아오므로
/// <b>못 걷는 것(건물)은 지어 놓고 3초면 무의미해진다.</b>
/// 막으면 도망이 <b>고리</b>가 된다 — 벽에 닿으면 꺾어야 하고, 꺾으면 언젠가 지나온 자리로 돌아온다.
/// ⇒ 건물이 <b>일할 기회</b>를 얻는다.</para>
///
/// <para>🔴 <b>원이 아니라 사각형이다.</b> <see cref="CameraController"/> 가 이미
/// <c>Bounds</c>(사각형)로 카메라를 가두는 코드를 갖고 있어서, 원으로 잡으면
/// <b>플레이어가 갈 수 있는 곳과 카메라가 갈 수 있는 곳의 모양이 달라진다.</b></para>
///
/// <para>🔑 <b>막는 자리는 두 곳뿐이다</b> — 플레이어의 위치와 <b>적이 소환되는 지점</b>.
/// 적은 플레이어를 쫓으므로 저절로 안쪽으로 들어온다. 소환만 안 막으면
/// <c>SpawnRadius</c>(기본 20)가 플레이어를 기준으로 재므로 <b>벽 근처에서 밖에 생긴다.</b></para>
///
/// <para>🔵 크기는 <c>Economy.csv</c> 의 <c>halfExtent</c> 다. 값 하나로 조절한다 —
/// <b>"직선으로 몇 초나 도망칠 수 있나"</b> 로 읽으면 감이 온다:
/// 가로지르는 거리 = <c>halfExtent × 2</c>, 기본 이동 속도는 <c>4</c> 다.
/// (24 면 48유닛 = 약 12초. 이동 속도 패시브가 붙으면 더 짧아진다.)</para>
/// </summary>
public class ArenaBounds : MonoBehaviour
{
    /// <summary>컴파일 반영 확인용 (D27).</summary>
    public const int Version = 1;

    [Tooltip("아레나 중심에서 벽까지의 거리(유닛). 가로지르는 데 걸리는 시간 = 2 x 이 값 / 이동속도.")]
    [SerializeField] private float halfExtent = 24f;

    [Tooltip("끄면 예전처럼 무한 맵이 된다. 되돌릴 수 있게 남겨 둔다.")]
    [SerializeField] private bool enabledBounds = true;

    [Header("보이는 벽")]
    [Tooltip("경계선 두께(유닛).")]
    [SerializeField] private float lineWidth = 0.35f;

    [SerializeField] private Color lineColor = new(0.85f, 0.30f, 0.25f, 0.85f);

    [Tooltip("경계선의 정렬 레이어 순서. 바닥보다 위, 적보다 아래에 둔다.")]
    [SerializeField] private int sortingOrder = 1;

    // ── 밖에서 쓰는 창구 ─────────────────────────────────────────
    //
    // 🔴 static 으로 여는 이유: 플레이어(PlayerController)와 소환(WaveManager)이 둘 다 봐야 하는데,
    //    서로 참조를 물리면 씬 배선이 하나 더 늘고 그 칸은 언젠가 빈다 (I-24).
    private static ArenaBounds _instance;

    /// <summary>경계가 켜져 있나. 없거나 꺼져 있으면 false — 그때는 예전처럼 무한 맵이다.</summary>
    public static bool Enabled => _instance != null && _instance.enabledBounds && _instance.halfExtent > 0f;

    /// <summary>중심에서 벽까지의 거리. 꺼져 있으면 0.</summary>
    public static float HalfExtent => Enabled ? _instance.halfExtent : 0f;

    /// <summary>아레나 중심 (이 오브젝트의 위치).</summary>
    public static Vector2 Center => _instance != null ? (Vector2)_instance.transform.position : Vector2.zero;

    /// <summary>
    /// 점을 아레나 안으로 밀어 넣는다. 경계가 꺼져 있으면 그대로 돌려준다.
    ///
    /// <para><paramref name="margin"/> 은 벽에서 더 떨어뜨릴 여유다 —
    /// 적을 벽에 딱 붙여 소환하면 벽에 낀 것처럼 보인다.</para>
    /// </summary>
    public static Vector2 Clamp(Vector2 p, float margin = 0f)
    {
        if (!Enabled) return p;
        float h = Mathf.Max(0.5f, _instance.halfExtent - Mathf.Max(0f, margin));
        Vector2 c = Center;
        return new Vector2(Mathf.Clamp(p.x, c.x - h, c.x + h),
                           Mathf.Clamp(p.y, c.y - h, c.y + h));
    }

    /// <summary>이 점이 아레나 밖인가 (검증용).</summary>
    public static bool IsOutside(Vector2 p)
    {
        if (!Enabled) return false;
        Vector2 c = Center; float h = _instance.halfExtent;
        return p.x < c.x - h || p.x > c.x + h || p.y < c.y - h || p.y > c.y + h;
    }

    // ── 수명 ─────────────────────────────────────────────────────

    private LineRenderer _line;

    private void Awake()
    {
        _instance = this;
        BuildLine();
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    /// <summary>
    /// 벽을 <b>보이게</b> 그린다.
    ///
    /// <para>🔴 <b>안 보이는 벽은 벽이 아니라 버그로 읽힌다.</b> 플레이어는 왜 안 움직이는지 모른다.</para>
    ///
    /// <para>🔑 <b>새 애셋이 없다</b> — <see cref="LineRenderer"/> 로 사각형 하나를 그린다.
    /// (<c>BossArrowGraphic</c> 이 삼각형을 코드로 그린 것과 같은 판단이다.)</para>
    /// </summary>
    private void BuildLine()
    {
        _line = GetComponent<LineRenderer>();
        if (_line == null) _line = gameObject.AddComponent<LineRenderer>();

        _line.useWorldSpace   = false;      // 이 오브젝트를 옮기면 벽도 따라온다
        _line.loop            = true;
        _line.positionCount   = 4;
        _line.widthMultiplier = lineWidth;
        _line.numCornerVertices = 0;
        _line.numCapVertices    = 0;
        _line.alignment       = LineAlignment.TransformZ;
        _line.textureMode     = LineTextureMode.Tile;
        _line.sortingOrder    = sortingOrder;

        // 🔴 <b>이 프로젝트는 URP 다.</b> 빌트인 <c>Sprites/Default</c> 를 물리면
        //    <c>isSupported</c> 는 <b>true 를 돌려주는데 URP 2D 렌더러가 그리지 않는다</b> —
        //    오류도 분홍색도 없이 <b>그냥 안 보인다.</b> 실제로 한 번 당했다 (D111).
        //    🔑 확실히 보이는 것들(적·타일맵)이 쓰는 셰이더가 정답이다:
        //       Universal Render Pipeline/2D/Sprite-Unlit-Default
        //    Lit 이 아니라 Unlit 을 쓰는 이유는 <b>경계선이 2D 조명에 어두워지면 안 되기 때문</b>이다 —
        //    이건 배경이 아니라 <b>알림</b>이다.
        if (_line.sharedMaterial == null || _line.sharedMaterial.shader == null
            || _line.sharedMaterial.shader.name == "Sprites/Default")
        {
            var sh = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                  ?? Shader.Find("Sprites/Default");          // 빌트인 프로젝트로 되돌아가도 동작하게
            _line.material = new Material(sh);
        }
        _line.startColor = _line.endColor = lineColor;

        float h = halfExtent;
        _line.SetPosition(0, new Vector3(-h, -h, 0f));
        _line.SetPosition(1, new Vector3( h, -h, 0f));
        _line.SetPosition(2, new Vector3( h,  h, 0f));
        _line.SetPosition(3, new Vector3(-h,  h, 0f));
        _line.enabled = enabledBounds && halfExtent > 0f;
    }

    /// <summary>
    /// 카메라도 같은 사각형에 가둔다 (D111).
    ///
    /// <para>🔴 <b>Start 다.</b> <c>CameraController</c> 를 <c>Awake</c> 에서 찾으면
    /// 아직 없을 수 있다 — 모든 <c>Awake</c> 가 모든 <c>Start</c> 보다 먼저 돈다는 규칙은
    /// <b>같은 단계 안의 순서까지 보장하지는 않는다</b> (I-8 · I-38 과 같은 함정).</para>
    /// </summary>
    private void Start()
    {
        if (!enabledBounds) return;
        var cam = Camera.main != null ? Camera.main.GetComponent<CameraController>() : null;
        if (cam != null) cam.SetBounds(new Bounds(transform.position,
                                                  new Vector3(halfExtent * 2f, halfExtent * 2f, 1f)));
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying) return;
        if (halfExtent < 1f) halfExtent = 1f;
    }
#endif

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0.3f, 0.6f);
        Gizmos.DrawWireCube(transform.position, new Vector3(halfExtent * 2f, halfExtent * 2f, 0f));
    }
}
