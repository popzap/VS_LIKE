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

    private Tilemap    _map;
    private Camera     _cam;
    private TileBase[] _buffer;
    private float[]    _cumulative;
    private float      _totalWeight;
    private Vector2Int _size;
    private Vector3Int _origin;
    private bool       _hasOrigin;

    private void Awake()
    {
        _map = GetComponent<Tilemap>();
        BuildWeightTable();
    }

    private void BuildWeightTable()
    {
        if (tiles == null || tiles.Length == 0) return;

        _cumulative  = new float[tiles.Length];
        _totalWeight = 0f;
        for (int i = 0; i < tiles.Length; i++)
        {
            // 비중이 0 이하로 들어와도 뽑히지 않게만 하고 넘어간다.
            _totalWeight += Mathf.Max(0f, tiles[i].Weight);
            _cumulative[i] = _totalWeight;
        }
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

        Vector2Int need = new Vector2Int(
            Mathf.CeilToInt(halfW * 2f) + margin * 2,
            Mathf.CeilToInt(halfH * 2f) + margin * 2);

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
