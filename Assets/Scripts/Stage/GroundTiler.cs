using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 카메라를 따라다니는 무한 바닥 타일맵.
///
/// 이 게임에는 아레나 경계가 없다. CameraController.useBounds = false 이고
/// 적은 플레이어 주변 SpawnRadius 에서 나오므로 플레이어는 어디로든 무한히 갈 수 있다.
/// 그래서 유한한 타일맵을 미리 깔아 두면 언젠가 바닥이 끊긴다.
///
/// 대신 화면보다 조금 큰 창(window)만 유지하고 카메라가 타일 한 칸을 넘어갈 때마다
/// 창을 옮겨 다시 채운다. 어떤 타일이 오는지는 좌표 해시로 정하므로
/// 같은 칸은 언제 다시 방문해도 항상 같은 그림이 나온다(깜빡임 없음).
/// </summary>
[RequireComponent(typeof(Tilemap))]
public class GroundTiler : MonoBehaviour
{
    [System.Serializable]
    public struct WeightedTile
    {
        public TileBase Tile;
        [Min(0f)] public float Weight;
    }

    [Tooltip("바닥에 깔릴 타일과 등장 비중. 평범한 타일의 비중을 크게 줘야 바닥이 산만하지 않다.")]
    [SerializeField] private WeightedTile[] tiles;

    [Tooltip("화면 밖으로 더 채워 둘 타일 수. 카메라가 흔들려도 가장자리가 비지 않을 만큼만.")]
    [SerializeField] private int margin = 4;

    [Tooltip("같은 배치를 다시 보고 싶으면 이 값을 고정해 둔다. 바꾸면 지형이 통째로 달라진다.")]
    [SerializeField] private int seed = 1337;

    [Tooltip("카메라가 이 칸 수를 넘어야 바닥을 다시 채운다. 크게 잡을수록 다시 채우는 횟수가 줄지만 "
           + "창이 그만큼 넓어진다(칸 수가 늘어난다). 칸이 0.25유닛이면 8 = 2유닛마다 한 번이다.")]
    [Min(1)] [SerializeField] private int refillStep = 8;

    // ── 층별 테마 (ROADMAP §7 · D49) ────────────────────────────
    [Header("층별 테마 — 값은 Economy.csv 가 덮는다")]
    [Tooltip("층이 깊어질수록 바닥이 달라진다. 🔴 그러려면 위 tiles 배열이 "
           + "\"얕은 층에 어울리는 것 → 깊은 층에 어울리는 것\" 순서여야 한다 — "
           + "깊어질수록 그 순서의 가중치를 뒤집어 가기 때문이다. "
           + "지금은 Grass(40) … Flagstone(1) 로 그렇게 정렬돼 있다.")]
    [SerializeField] private bool depthTheme = true;

    [Tooltip("가중치가 완전히 뒤집히는 층 번호(0부터). 맵이 10층이면 9.")]
    [Min(1)] [SerializeField] private int deepestLayer = 9;

    private Tilemap    _map;
    private Camera     _cam;
    private TileBase[] _buffer;
    private float[]    _cumulative;
    private float      _totalWeight;
    private Vector2Int _size;
    private Vector3Int _origin;
    private bool       _hasOrigin;

    // 지금 가중치 표가 어느 층 기준으로 만들어졌나. -1 = 아직 안 만듦.
    private int _builtLayer = -1;

    // 🔴 컴파일 반영 확인용 (D27).
    public const int Version = 4;   // 4 = 층 색조를 타일맵으로 (D79) · 3 = 기준점 양자화 (D79) · 2 = 층별 테마 (D49)

    /// <summary>
    /// <paramref name="v"/> 를 <paramref name="step"/> 의 배수로 내림한다.
    ///
    /// <para>🔴 <c>v / step * step</c> 으로 쓰면 안 된다 — C# 의 정수 나눗셈은 <b>0 쪽으로</b> 자르므로
    /// 음수에서 결과가 위로 튄다(<c>-1/8*8 = 0</c>). 그러면 원점 부근에서 기준점이 왔다 갔다 하며
    /// <b>매 프레임 다시 채운다.</b> 바닥은 원점 왼쪽/아래에도 깔린다.</para>
    /// </summary>
    private static int Quantize(int v, int step) => Mathf.FloorToInt(v / (float)step) * step;

    private void Awake()
    {
        _map = GetComponent<Tilemap>();
        BuildWeightTable(LayerScaling.Layer);
    }

    /// <summary>
    /// 층 <paramref name="layer"/> 기준의 누적 가중치 표를 만든다.
    ///
    /// <para>🔑 <b>새 데이터를 만들지 않았다.</b> <see cref="tiles"/> 가 이미
    /// <b>얕은 것 → 깊은 것</b> 순서로 정렬돼 있으므로(Grass 40 … Flagstone 1),
    /// 깊어질수록 그 가중치 배열을 <b>제 순서의 역순 쪽으로 보간</b>하면
    /// 풀밭이 돌바닥으로 바뀐다. 타일 10종을 그대로 쓴다.</para>
    ///
    /// <para>🔴 <b>0층에서는 예전과 완전히 같아야 한다</b> — <c>t = 0</c> 이면
    /// 보간이 원래 가중치를 그대로 돌려준다. 이게 이 변경의 대조군이다.</para>
    /// </summary>
    private void BuildWeightTable(int layer)
    {
        if (tiles == null || tiles.Length == 0) return;

        float t = 0f;
        if (depthTheme && deepestLayer > 0)
            t = Mathf.Clamp01((float)Mathf.Max(0, layer) / deepestLayer);

        int n = tiles.Length;
        _cumulative  = new float[n];
        _totalWeight = 0f;
        for (int i = 0; i < n; i++)
        {
            // 비중이 0 이하로 들어와도 뽑히지 않게만 하고 넘어간다.
            float shallow = Mathf.Max(0f, tiles[i].Weight);
            float deep    = Mathf.Max(0f, tiles[n - 1 - i].Weight);
            _totalWeight += Mathf.Lerp(shallow, deep, t);
            _cumulative[i] = _totalWeight;
        }

        ApplyDepthTint(t);
        _builtLayer = layer;
    }

    // ── 층 색조 (D79) ────────────────────────────────────────────

    /// <summary>층0 색조. 타일 애셋의 색을 그대로 쓴다 = 대조군.</summary>
    private static readonly Color ShallowTint = new Color(1.00f, 1.00f, 1.00f, 1f);

    /// <summary>
    /// 가장 깊은 층 색조. 빨강·초록만 내려 <b>차갑고 어둡게</b> 만든다.
    ///
    /// <para>🔑 값은 <b>C37 램프의 층9 밝기(화면 L 0.110)에 맞췄다</b> — 이번 변경의 목적은
    /// *"특정색이 튄다"* 를 없애는 것이지 후반을 어둡게 하는 게 아니다. 아무것도 뒷걸음치지 않는다.
    /// (0.45, 0.50, 1.00) 으로 두면 L 0.054 로 <b>예전의 절반</b>이 되어 분위기가 통째로 바뀐다.</para>
    /// </summary>
    private static readonly Color DeepTint    = new Color(0.62f, 0.68f, 1.00f, 1f);

    /// <summary>
    /// 층 구분을 <b>타일이 아니라 타일맵 전체</b>에 건다 (D79 · 사용자 요구).
    ///
    /// <para>🔴 예전에는 타일 10종의 색을 <b>얕은색 → 깊은색 램프</b>로 칠해 두고
    /// 층마다 가중치를 뒤집었다(<c>C37</c>). 그러면 <b>한 층 안에 서로 다른 색이 섞인다</b> —
    /// 층0 에서도 깊은 타일이 7 % 는 깔리는데 그게 가장 먼 색이라
    /// <b>올리브 바닥에 남색 구멍처럼 박혔다.</b> 사용자가 그걸 짚었다.</para>
    ///
    /// <para>🔑 이제 타일 10종은 <b>전부 같은 한 점</b>이고(<c>Tools/Art/retint_tiles.py</c>),
    /// 층 구분은 여기 색조 하나가 만든다. 모든 타일에 <b>같이</b> 곱해지므로
    /// 아무리 세게 갈라도 한 층 안에서는 여전히 한 색이다 — 두 목표가 더는 서로를 깎지 않는다.</para>
    ///
    /// <para>⚠️ 정점 색은 곱셈이라 <b>1 을 넘겨 밝힐 수 없다.</b> 그래서 얕은 쪽이 흰색(=원본)이고
    /// 깊은 쪽으로만 내려간다. 타일 애셋의 목표색이 초반 밝기 기준이 되는 이유다.</para>
    /// </summary>
    private void ApplyDepthTint(float t)
    {
        if (_map == null) return;
        _map.color = depthTheme ? Color.Lerp(ShallowTint, DeepTint, t) : ShallowTint;
    }

    private void LateUpdate()
    {
        // Camera.main 은 태그 검색이라 매 프레임 부르면 비싸다. 한 번만 잡는다.
        if (_cam == null)
        {
            _cam = Camera.main;
            if (_cam == null) return;
        }
        if (_totalWeight <= 0f) return;

        // 층이 바뀌었으면 가중치를 다시 만들고 화면을 통째로 다시 깐다.
        // _hasOrigin 을 지우지 않으면 카메라가 한 칸 움직일 때까지 옛 바닥이 남는다.
        if (_builtLayer != LayerScaling.Layer)
        {
            BuildWeightTable(LayerScaling.Layer);
            _hasOrigin = false;
        }

        EnsureBuffer();

        // 🔴 <b>카메라 칸을 그대로 쓰면 칸이 작아질수록 비용이 폭증한다</b> (D79).
        //    예전에는 카메라가 <b>한 칸</b>을 넘을 때마다 창 전체(수천 칸)를 다시 썼다.
        //    칸을 절반으로 줄이면 창의 칸 수가 4배가 되고 넘는 횟수도 2배가 되어 <b>비용이 8배</b>다.
        //    ⇒ 기준점을 <see cref="refillStep"/> 칸 단위로 <b>양자화</b>한다. 그러면 다시 채우는 간격이
        //    "칸 몇 개"가 아니라 "<b>월드 거리 얼마</b>"로 고정되어 칸 크기와 무관해진다.
        //    그 대신 창을 그만큼 넓게 잡는다(<see cref="EnsureBuffer"/> 의 <c>+ step * 2</c>).
        int step = Mathf.Max(1, refillStep);
        Vector3Int camCell = _map.WorldToCell(_cam.transform.position);
        Vector3Int want    = new Vector3Int(
            Quantize(camCell.x, step) - _size.x / 2,
            Quantize(camCell.y, step) - _size.y / 2, 0);

        // 기준점이 그대로면 아무 일도 안 한다.
        if (_hasOrigin && want == _origin) return;

        _origin    = want;
        _hasOrigin = true;
        Fill();
    }

    private void EnsureBuffer()
    {
        // 화면을 덮는 데 필요한 칸 수. 창 크기가 바뀌면(에디터 리사이즈) 버퍼도 다시 잡는다.
        float halfH = _cam.orthographicSize;
        float halfW = halfH * _cam.aspect;

        // 🔴 <b>월드 유닛을 칸 수로 그대로 쓰면 안 된다</b> (D78).
        //    예전 식은 <c>CeilToInt(halfW * 2)</c> 였는데, 그건 <b>셀 크기가 1 일 때만</b> 맞는다.
        //    타일이 0.5유닛이 되면서 셀도 0.5 가 됐고, 그대로 두면 <b>화면의 절반만 덮어</b>
        //    가장자리가 검게 빈다. 셀 크기로 나눠야 크기가 무엇이든 화면을 채운다.
        Vector3 cell = _map.layoutGrid != null ? _map.layoutGrid.cellSize : Vector3.one;
        float cx = Mathf.Max(0.01f, cell.x);
        float cy = Mathf.Max(0.01f, cell.y);

        // 🔑 <c>step * 2</c> 가 양자화의 대가다 (D79). 기준점이 <c>step</c> 칸 단위로만 움직이므로
        //    카메라가 창 중심에서 최대 <c>step</c> 칸까지 벗어난다 — 그만큼을 양쪽에 미리 깔아 둔다.
        //    <see cref="margin"/> 은 그 위에 남는 여유다(카메라 흔들림 몫).
        int step = Mathf.Max(1, refillStep);
        Vector2Int need = new Vector2Int(
            Mathf.CeilToInt(halfW * 2f / cx) + (margin + step) * 2,
            Mathf.CeilToInt(halfH * 2f / cy) + (margin + step) * 2);

        if (_buffer != null && need == _size) return;

        _size      = need;
        _buffer    = new TileBase[_size.x * _size.y];
        _hasOrigin = false;   // 크기가 바뀌었으니 무조건 다시 채워야 한다
    }

    private void Fill()
    {
        for (int y = 0; y < _size.y; y++)
        {
            int row = y * _size.x;
            for (int x = 0; x < _size.x; x++)
                _buffer[row + x] = PickTile(_origin.x + x, _origin.y + y);
        }

        // 칸마다 SetTile 을 부르면 이동할 때마다 수천 번 호출된다. 한 번에 밀어 넣는다.
        _map.SetTilesBlock(new BoundsInt(_origin, new Vector3Int(_size.x, _size.y, 1)), _buffer);
    }

    private TileBase PickTile(int x, int y)
    {
        // 해시가 좌표만으로 결정되므로 창이 지나갔다 되돌아와도 같은 타일이 다시 나온다.
        float r = Hash(x, y) * _totalWeight;
        for (int i = 0; i < _cumulative.Length; i++)
            if (r < _cumulative[i]) return tiles[i].Tile;

        return tiles[tiles.Length - 1].Tile;
    }

    /// <summary>좌표 → [0,1). 인접한 좌표끼리 값이 튀도록 섞어야 줄무늬가 생기지 않는다.</summary>
    private float Hash(int x, int y)
    {
        unchecked
        {
            uint h = (uint)(x * 73856093) ^ (uint)(y * 19349663) ^ (uint)(seed * 83492791);
            h ^= h >> 16;
            h *= 0x7feb352d;
            h ^= h >> 15;
            h *= 0x846ca68b;
            h ^= h >> 16;
            return (h & 0xFFFFFF) / 16777216f;
        }
    }
}
