using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 범용 오브젝트 풀. 프리팹별로 Queue를 관리한다.
/// 수백 개의 적/투사체를 생성/삭제 없이 재사용.
/// </summary>
public class ObjectPool : MonoBehaviour
{
    [Header("초기 풀 워밍업 (선택)")]
    [SerializeField] private PoolEntry[] warmUpEntries;

    [System.Serializable]
    private class PoolEntry
    {
        public GameObject Prefab;
        public int        Count;
    }

    private readonly Dictionary<GameObject, Queue<GameObject>> _pools = new();
    private readonly Dictionary<GameObject, GameObject>        _prefabByInstance = new();

    private void Start()
    {
        if (warmUpEntries == null) return;
        foreach (var entry in warmUpEntries)
        {
            for (int i = 0; i < entry.Count; i++)
            {
                var obj = CreateNew(entry.Prefab);
                obj.SetActive(false);
                GetQueue(entry.Prefab).Enqueue(obj);
            }
        }
    }

    /// <summary>오브젝트 꺼내기.</summary>
    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (PerfCounters.On) PerfCounters.PoolGets++;   // 🔬 D27 계측 (임시)

        var queue = GetQueue(prefab);
        GameObject obj;

        if (queue.Count > 0)
        {
            obj = queue.Dequeue();
        }
        else
        {
            obj = CreateNew(prefab);
        }

        obj.transform.SetPositionAndRotation(position, rotation);
        obj.SetActive(true);
        return obj;
    }

    /// <summary>오브젝트 반환.</summary>
    public void Return(GameObject obj)
    {
        if (obj == null) return;
        obj.SetActive(false);
        obj.transform.SetParent(transform);

        if (_prefabByInstance.TryGetValue(obj, out var prefab))
            GetQueue(prefab).Enqueue(obj);
        else
            Destroy(obj); // 풀에 없는 오브젝트는 삭제
    }

    // ── 내부 ────────────────────────────────────────────────────

    private Queue<GameObject> GetQueue(GameObject prefab)
    {
        if (!_pools.ContainsKey(prefab))
            _pools[prefab] = new Queue<GameObject>();
        return _pools[prefab];
    }

    private GameObject CreateNew(GameObject prefab)
    {
        // 🔬 D27 계측 (임시). 풀이 비어 Instantiate 를 부르는 순간 = 스파이크 후보.
        if (PerfCounters.On) PerfCounters.PoolCreates++;

        var obj = Instantiate(prefab, transform);
        obj.SetActive(false);
        _prefabByInstance[obj] = prefab;
        return obj;
    }

    /// <summary>특정 프리팹의 활성화된 인스턴스 수 (디버그용).</summary>
    public int GetActiveCount(GameObject prefab)
    {
        int active = 0;
        foreach (var pair in _prefabByInstance)
            if (pair.Value == prefab && pair.Key.activeInHierarchy)
                active++;
        return active;
    }
}
