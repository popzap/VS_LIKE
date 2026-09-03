using System.Collections.Generic;
using UnityEngine;

// ────────────────────────────────────────────────────────────────────────────
//  BuildingManager  —  건물 해금 / 설치 대기열 / 배치
// ────────────────────────────────────────────────────────────────────────────
/// <summary>
/// 건물은 <b>고르지 않는다</b>. 아이템으로 얻으면 설치 대기열에 쌓이고,
/// Z 키를 누르면 대기열 맨 앞의 것이 플레이어 옆에 바로 세워진다.
///
/// <para><b>대기열은 두 구간이다</b> (B3, C안). 앞쪽은 <b>처음 해금된 건물</b>,
/// 뒤쪽은 <b>레벨업으로 늘어난 증설분</b>. 레벨업 카드가 "Village 획득"이라고 말했으면
/// 다음 <c>Z</c> 는 반드시 Village 를 준다 — 그 약속을 조작이 지키게 하는 게 이 구분의 전부다.
/// 구간 안에서는 <b>얻은 순서(FIFO)</b> 그대로라, 나중에 해금한 것이 먼저 해금한 것을
/// 추월하지 않는다.</para>
///
/// <para>예전에는 마우스 커서로 위치를 고르는 배치 모드였지만
/// <c>SelectBuildingForPlacement()</c> 를 부르는 곳이 아무 데도 없어서
/// <c>_selectedBuildingData</c> 가 늘 null 이었고 배치가 통째로 죽어 있었다.</para>
/// </summary>
public class BuildingManager : MonoBehaviour
{
    // 🔴 컴파일 반영 확인용 (D27). `Assets/Refresh` 는 재컴파일을 보장하지 않으므로
    //    콘솔이 아니라 이 값을 조회해서 새 코드가 로드됐는지 판정한다.
    public const int Version = 2;   // 2 = B3 C안 (신규/증설 두 구간)

    [Header("배치 설정")]
    [Tooltip("여기 걸린 콜라이더가 있으면 그 자리에는 세우지 않는다. 최소한 Building 레이어는 넣어야 겹쳐 쌓이지 않는다.")]
    [SerializeField] private LayerMask  placementBlockLayer;
    [SerializeField] private ObjectPool buildingPool;
    [Tooltip("플레이어에게서 이만큼 떨어뜨려 세운다. 발밑에 세우면 플레이어가 갇힌다.")]
    [SerializeField] private float      placeDistance    = 1.2f;
    [Tooltip("이 반경 안에 막는 콜라이더가 없어야 세울 수 있다.")]
    [SerializeField] private float      placeClearRadius = 0.5f;

    // 해금된 건물 (data → level)
    private readonly Dictionary<BuildingData, int> _unlockedBuildings = new();
    // 배치된 건물 (data → instances)
    private readonly Dictionary<BuildingData, List<BuildingBase>> _placedBuildings = new();
    // 설치 대기열. 앞쪽 _freshCount 개가 "처음 해금된 것", 그 뒤가 "레벨업 증설분".
    // 두 구간 각각은 획득 순서 그대로다.
    private readonly List<BuildingData> _pendingQueue = new();

    // 🔴 _pendingQueue 를 건드리는 모든 경로가 이 값을 같이 맞춰야 한다.
    //    지금은 PlaceNext(맨 앞 제거) · LockBuilding(임의 제거) · ResetRunState 뿐이다.
    private int _freshCount;

    public int          PendingCount => _pendingQueue.Count;
    public BuildingData NextPending  => _pendingQueue.Count > 0 ? _pendingQueue[0] : null;

    // ── 해금 ────────────────────────────────────────────────────

    public void UnlockBuilding(BuildingData data, int level)
    {
        if (data == null) return;

        bool alreadyOwned = _unlockedBuildings.ContainsKey(data);
        _unlockedBuildings[data] = level;
        if (alreadyOwned) UpgradePlacedBuildings(data, level);

        // 레벨이 오르면 동시 배치 가능 수(MaxCount)도 늘어난다.
        // "가질 수 있는 수 - (이미 세운 수 + 대기 중인 수)" 만큼만 대기열에 넣는다.
        int allowed = data.GetMaxCount(level);
        for (int owned = PlacedCount(data) + PendingCountOf(data); owned < allowed; owned++)
        {
            // B3 — 처음 해금된 것은 신규 구간의 끝에, 레벨업 증설분은 대기열 맨 뒤에.
            // 🔑 "맨 앞(index 0)" 이 아니라 "신규 구간의 끝" 인 이유: 맨 앞에 넣으면
            //    나중에 해금한 건물이 아직 못 세운 먼저 해금한 건물을 추월한다.
            if (alreadyOwned) _pendingQueue.Add(data);
            else              _pendingQueue.Insert(_freshCount++, data);
        }
    }

    private void UpgradePlacedBuildings(BuildingData data, int level)
    {
        if (!_placedBuildings.TryGetValue(data, out var list)) return;
        foreach (var b in list) b.Upgrade(level);
    }

    private int PlacedCount(BuildingData data)
        => _placedBuildings.TryGetValue(data, out var list) ? list.Count : 0;

    private int PendingCountOf(BuildingData data)
    {
        int n = 0;
        foreach (var d in _pendingQueue) if (d == data) n++;
        return n;
    }

    // ── 배치 (Z 키) ──────────────────────────────────────────────

    /// <summary>
    /// 대기열 맨 앞의 건물을 <paramref name="origin"/> 근처 빈 자리에 세운다.
    /// 자리가 없으면 대기열을 그대로 두고 <c>false</c> 를 돌려준다.
    /// </summary>
    public bool PlaceNext(Vector2 origin)
    {
        if (_pendingQueue.Count == 0) return false;

        var data = _pendingQueue[0];
        if (data == null || !_unlockedBuildings.TryGetValue(data, out int level) || data.Prefab == null)
        {
            RemoveFront();   // 망가진 항목은 버린다
            return false;
        }

        if (!TryFindSpot(origin, out var pos)) return false;

        var go       = buildingPool.Get(data.Prefab, pos, Quaternion.identity);
        var building = go.GetComponent<BuildingBase>();
        if (building == null)
        {
            buildingPool.Return(go);
            RemoveFront();
            return false;
        }

        building.Initialize(data, level, buildingPool);

        if (!_placedBuildings.ContainsKey(data)) _placedBuildings[data] = new List<BuildingBase>();
        _placedBuildings[data].Add(building);
        RemoveFront();

        // 실제로 설치된 경로에서만 울린다. 위의 실패 반환들은 소리가 나면 안 된다 —
        // 자리를 못 찾아 실패한 것과 설치된 것을 소리로 구분할 수 있어야 한다.
        AudioManager.Play(SfxId.BuildingPlace);
        return true;
    }

    /// <summary>
    /// 대기열 맨 앞을 버리면서 신규 구간 길이도 같이 줄인다.
    /// <c>RemoveAt(0)</c> 를 직접 부르면 <see cref="_freshCount"/> 가 어긋난다.
    /// </summary>
    private void RemoveFront()
    {
        _pendingQueue.RemoveAt(0);
        if (_freshCount > 0) _freshCount--;
    }

    /// <summary>플레이어를 중심으로 동심원을 돌며 빈 자리를 찾는다.</summary>
    private bool TryFindSpot(Vector2 origin, out Vector2 pos)
    {
        for (int ring = 1; ring <= 3; ring++)
        {
            float r = placeDistance * ring;
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI * 0.25f;
                var p = origin + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                if (CanPlaceAt(p)) { pos = p; return true; }
            }
        }
        pos = origin;
        return false;
    }

    private bool CanPlaceAt(Vector2 pos)
        => Physics2D.OverlapCircle(pos, placeClearRadius, placementBlockLayer) == null;

    // ── 조회 ─────────────────────────────────────────────────────

    /// <summary>
    /// <paramref name="origin"/> 에서 <paramref name="radius"/> 안에 있는 가장 가까운 설치 건물.
    /// 없으면 null. 진화 제단(건물 앞 상호작용) 판정이 쓴다.
    /// </summary>
    public BuildingBase FindNearestPlaced(Vector2 origin, float radius)
    {
        BuildingBase nearest = null;
        float minSqr = radius * radius;

        foreach (var pair in _placedBuildings)
        {
            foreach (var b in pair.Value)
            {
                if (b == null || !b.gameObject.activeInHierarchy) continue;

                float sqr = ((Vector2)b.transform.position - origin).sqrMagnitude;
                if (sqr > minSqr) continue;

                minSqr  = sqr;
                nearest = b;
            }
        }
        return nearest;
    }

    // ── 정리 ─────────────────────────────────────────────────────

    /// <summary>새 런 시작 시 <see cref="GameManager.StartRun"/> 가 호출한다.</summary>
    public void ResetRunState()
    {
        ClearAllBuildings();
        _unlockedBuildings.Clear();
        _pendingQueue.Clear();
        _freshCount = 0;
    }

    public void ClearAllBuildings()
    {
        foreach (var pair in _placedBuildings)
            foreach (var b in pair.Value)
                buildingPool.Return(b.gameObject);
        _placedBuildings.Clear();
    }

    /// <summary>
    /// 상점에서 건물 아이템 제거 시 호출.
    /// 배치된 인스턴스와 설치 대기열 항목을 모두 없애고 해금 목록에서 뺀다.
    /// </summary>
    public void LockBuilding(BuildingData data)
    {
        if (data == null) return;

        if (_placedBuildings.TryGetValue(data, out var placed))
        {
            foreach (var b in placed) buildingPool.Return(b.gameObject);
            _placedBuildings.Remove(data);
        }

        // 신규 구간 안에서 몇 개가 지워지는지 먼저 세고 빼야 _freshCount 가 안 어긋난다.
        int removedFresh = 0;
        for (int i = 0; i < _freshCount && i < _pendingQueue.Count; i++)
            if (_pendingQueue[i] == data) removedFresh++;

        _pendingQueue.RemoveAll(d => d == data);
        _freshCount = Mathf.Max(0, _freshCount - removedFresh);
        _unlockedBuildings.Remove(data);
    }
}
