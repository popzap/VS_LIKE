using System.Collections.Generic;
using UnityEngine;

public enum StageType { Normal, Elite, Shop, Event, Boss }

[System.Serializable]
public class StageNode
{
    public int       Layer;           // 몇 번째 층인지 (0 = 시작)
    public int       IndexInLayer;    // 같은 층 내 인덱스
    public StageType StageType;
    public List<int> NextNodeIndices = new(); // 다음 층 연결 인덱스
    public bool      IsVisited;
    public bool      IsAvailable;     // 현재 선택 가능한 노드인지
    public Vector2   MapPosition;     // UI 배치용 좌표
}

/// <summary>
/// 슬레이 더 스파이어 스타일의 분기형 스테이지 맵을 생성하고 관리한다.
/// 구조: 레이어(층) × 노드, 각 노드는 다음 층의 1~2개 노드와 연결.
/// </summary>
public class StageMapManager : MonoBehaviour
{
    [Header("맵 설정")]
    [SerializeField] private int   totalLayers     = 10;  // 총 층 수 (마지막은 항상 보스)
    [SerializeField] private int   maxNodesPerLayer = 3;
    [SerializeField] private float xSpacing = 200f;
    [SerializeField] private float ySpacing = 150f;

    [Header("스테이지 타입 가중치 (노말/엘리트/상점/이벤트)")]
    [SerializeField] private float weightNormal = 0.50f;
    [SerializeField] private float weightElite  = 0.20f;
    [SerializeField] private float weightShop   = 0.15f;
    [SerializeField] private float weightEvent  = 0.15f;

    public List<List<StageNode>> Layers { get; private set; } = new();
    public StageNode              CurrentNode { get; private set; }

    // ── 맵 생성 ─────────────────────────────────────────────────

    public void GenerateMap()
    {
        Layers.Clear();

        for (int layer = 0; layer < totalLayers; layer++)
        {
            bool isBossLayer = (layer == totalLayers - 1);
            int  nodeCount   = isBossLayer ? 1 : Random.Range(2, maxNodesPerLayer + 1);

            var layerList = new List<StageNode>();
            for (int i = 0; i < nodeCount; i++)
            {
                var node = new StageNode
                {
                    Layer        = layer,
                    IndexInLayer = i,
                    StageType    = isBossLayer ? StageType.Boss : RollStageType(layer),
                    MapPosition  = new Vector2(layer * xSpacing, (i - nodeCount * 0.5f) * ySpacing)
                };
                layerList.Add(node);
            }
            Layers.Add(layerList);
        }

        ConnectLayers();
        SetAvailableNodes(Layers[0]);   // 첫 층 전부 선택 가능
    }

    private void ConnectLayers()
    {
        for (int layer = 0; layer < Layers.Count - 1; layer++)
        {
            var current = Layers[layer];
            var next    = Layers[layer + 1];

            // 각 노드에서 다음 층 1~2개 연결 (중복 허용)
            foreach (var node in current)
            {
                int connections = Random.Range(1, Mathf.Min(3, next.Count + 1));
                var indices = new HashSet<int>();
                while (indices.Count < connections)
                    indices.Add(Random.Range(0, next.Count));

                node.NextNodeIndices.AddRange(indices);
            }

            // 다음 층에 고아 노드가 없도록 보정
            var reachable = new HashSet<int>();
            foreach (var node in current)
                foreach (int idx in node.NextNodeIndices)
                    reachable.Add(idx);

            for (int j = 0; j < next.Count; j++)
            {
                if (!reachable.Contains(j))
                {
                    // 랜덤 이전 노드에서 연결 추가
                    current[Random.Range(0, current.Count)].NextNodeIndices.Add(j);
                }
            }
        }
    }

    private StageType RollStageType(int layer)
    {
        // 첫 층은 항상 노말
        if (layer == 0) return StageType.Normal;
        // 두 층마다 상점이 최소 1개 보장 (단순 보정)
        float r = Random.value;
        float cumulative = 0;

        cumulative += weightNormal; if (r < cumulative) return StageType.Normal;
        cumulative += weightElite;  if (r < cumulative) return StageType.Elite;
        cumulative += weightShop;   if (r < cumulative) return StageType.Shop;
        cumulative += weightEvent;  if (r < cumulative) return StageType.Event;
        return StageType.Normal;    // 가중치 합이 1 미만일 때의 폴백
    }

    // ── 진행 ────────────────────────────────────────────────────

    /// <summary>플레이어가 노드를 선택했을 때 UI에서 호출.</summary>
    public void SelectNode(StageNode node)
    {
        if (!node.IsAvailable) return;
        node.IsVisited = true;
        CurrentNode = node;
        GameManager.Instance.OnStageNodeSelected(node);
    }

    /// <summary>웨이브 클리어 후 다음 층 노드들을 활성화.</summary>
    public void AdvanceToNext(StageNode completedNode)
    {
        // 현재 층 전체 비활성화
        foreach (var n in Layers[completedNode.Layer])
            n.IsAvailable = false;

        if (completedNode.Layer + 1 >= Layers.Count) return;

        // 클리어한 노드에서 연결된 노드만 활성화
        var nextLayer = Layers[completedNode.Layer + 1];
        foreach (int idx in completedNode.NextNodeIndices)
            nextLayer[idx].IsAvailable = true;
    }

    private void SetAvailableNodes(List<StageNode> nodes)
    {
        foreach (var n in nodes) n.IsAvailable = true;
    }

    /// <summary>현재 진행 상황 요약 (디버그용).</summary>
    public string GetMapSummary()
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < Layers.Count; i++)
        {
            sb.Append($"Layer {i}: ");
            foreach (var n in Layers[i])
                sb.Append($"[{n.StageType}{(n.IsAvailable ? "*" : "")}] ");
            sb.AppendLine();
        }
        return sb.ToString();
    }
}
