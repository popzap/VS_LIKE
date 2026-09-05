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
    public const int Version = 2;   // 2 = 층별 테마 (D49)

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
        _builtLayer = layer;
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

        Vector3Int camCell = _map.WorldToCell(_cam.transform.position);
        Vector3Int want    = new Vector3Int(camCell.x - _size.x / 2, camCell.y - _size.y / 2, 0);

        // 카메라가 한 칸을 넘어갔을 때만 다시 채운다. 그 사이 프레임은 아무 일도 안 한다.
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

        Vector2Int need = new Vector2Int(
            Mathf.CeilToInt(halfW * 2f / cx) + margin * 2,
            Mathf.CeilToInt(halfH * 2f / cy) + margin * 2);

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
