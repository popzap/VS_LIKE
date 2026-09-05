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

    // ── 배치 규칙 (D70 · 사용자 요구 5) ──────────────────────────
    // 🔴 수치는 Economy.csv 의 StageMapManager 행이 들고 있다. 여기 기본값은 자리표시다.
    [Header("배치 규칙 — 값은 Economy.csv 가 덮는다")]
    [Tooltip("엘리트가 나올 수 있는 가장 얕은 층. 2 면 0·1층에는 안 나온다. "
           + "레벨 2~3 짜리가 엘리트를 만나면 그건 갈림길이 아니라 벽이다.")]
    [SerializeField] private int eliteMinLayer = 2;

    [Tooltip("상점을 이만큼 못 만나면 다음 층에 하나를 강제한다. "
           + "🔴 D61 이 9층을 도는 동안 상점을 0회 만났다 — 확률만으로는 이게 막히지 않는다.")]
    [SerializeField] private int shopPityLayers = 4;

    /// <summary>상점이 마지막으로 나온 층. 연속 금지·자비 규칙이 같이 본다.</summary>
    private int _lastShopLayer;

    public List<List<StageNode>> Layers { get; private set; } = new();
    public StageNode              CurrentNode { get; private set; }

    // ── 맵 생성 ─────────────────────────────────────────────────

    public void GenerateMap()
    {
        Layers.Clear();

        // 🔴 <b>0 으로 시작한다 — "0층에 상점이 있었다고 친다".</b>
        //    처음엔 -shopPityLayers 로 두었는데("충분히 오래 못 만났다"), 그러면 1층이
        //    바로 자비 규칙에 걸려 <b>400판 중 400판이 1층 상점</b>이 됐다.
        //    매판 같은 자리에 있으면 그건 갈림길이 아니라 정해진 길이고,
        //    1층은 아직 돈이 거의 없어 상점의 값어치도 낮다.
        //    0 으로 두면 ① 연속 금지가 1층을 막고, 첫 강제는 shopPityLayers 층에서 온다.
        _lastShopLayer = 0;

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
                    StageType    = StageType.Normal,   // 실제 종류는 아래에서 층 단위로 정한다
                    // 🔴 예전 식 `(i - nodeCount * 0.5f)` 은 <b>0 을 중심으로 대칭이 아니다</b> —
                    //    2칸 층은 -1.0·0.0, 3칸 층은 -1.5·-0.5·0.5 로 서로 반 칸씩 어긋나
                    //    화면에서 들쭉날쭉해 보였다. (i - (n-1)/2) 는 항상 0 을 가운데 둔다.
                    MapPosition  = new Vector2(layer * xSpacing,
                                               (i - (nodeCount - 1) * 0.5f) * ySpacing)
                };
                layerList.Add(node);
            }

            if (isBossLayer) foreach (var n in layerList) n.StageType = StageType.Boss;
            else             AssignLayerTypes(layerList, layer, totalLayers);

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

            // 🔴 예전에는 다음 층의 <b>아무 칸이나</b> 1~2개 골랐다 (`Random.Range(0, next.Count)`).
            //    그래서 맨 위 노드가 맨 아래로 가는 선이 예사로 생겼고, 3×3 구간에서는
            //    선 여섯 줄이 서로 엇갈려 <b>어디로 이어지는지 눈으로 못 따라간다.</b>
            //
            // 🔑 <b>자기 자리에 대응하는 칸과 그 이웃</b>에만 잇는다. 위/아래 순서가 뒤집히지
            //    않으므로(단조) 선이 교차하는 경우가 거의 없어진다 —
            //    슬레이 더 스파이어의 맵이 읽히는 이유가 이것이다.
            for (int i = 0; i < current.Count; i++)
            {
                var node = current[i];

                // 이 노드가 다음 층에서 "같은 높이" 로 보이는 자리
                int center = current.Count <= 1
                    ? next.Count / 2
                    : Mathf.RoundToInt(i * (next.Count - 1) / (float)(current.Count - 1));
                center = Mathf.Clamp(center, 0, next.Count - 1);

                var indices = new HashSet<int> { center };

                // 절반쯤은 이웃 한 칸을 더 연다 — 갈림길이 있어야 맵이 의미가 있다.
                if (next.Count > 1 && Random.value < 0.5f)
                {
                    int side = Random.value < 0.5f ? -1 : 1;
                    int alt  = Mathf.Clamp(center + side, 0, next.Count - 1);
                    indices.Add(alt);
                }

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

    /// <summary>
    /// 한 층의 노드 종류를 <b>같이</b> 정한다 (D70 · 사용자 요구 5:
    /// *"shop 2연속으로 안되거나 타당성있게 노드들이 배치되게"*).
    ///
    /// <para>🔑 <b>노드 하나씩 굴리면 규칙을 걸 수 없다.</b> "이 층에 상점이 있나",
    /// "다 같은 종류인가" 는 <b>층을 다 보고서야</b> 답할 수 있는 질문이다.
    /// 그래서 굴림의 단위를 노드에서 <b>층</b>으로 올렸다.</para>
    ///
    /// <para>규칙</para>
    /// <list type="number">
    ///   <item><b>상점은 연속한 두 층에 못 나온다</b> — 사용자 요구 그대로.</item>
    ///   <item><b>자비</b>: <see cref="shopPityLayers"/> 층 동안 못 만났으면 하나를 강제한다.
    ///         🔴 <c>D61</c> 이 9층을 도는 동안 상점을 <b>0회</b> 만났다
    ///         (<c>weightShop 0.15</c> 의 기대는 1.35회다) — 확률만으로는 안 막힌다.</item>
    ///   <item><b>엘리트는 <see cref="eliteMinLayer"/> 층부터.</b> 레벨 2~3 이 엘리트를 만나면
    ///         그건 갈림길이 아니라 벽이다.</item>
    ///   <item><b>한 층이 전부 같은 특수 노드가 되지 않는다.</b> 상점 셋뿐인 층은
    ///         <b>갈림길이 아니다</b> — 고를 게 없으면 맵이 있으나 마나다.</item>
    ///   <item><b>보스 직전 층은 상점을 선호한다</b> — 마지막으로 채비할 자리가 필요하다.
    ///         연속 금지(①)와 부딪히면 <b>①이 이긴다.</b></item>
    /// </list>
    /// </summary>
    private void AssignLayerTypes(List<StageNode> layerList, int layer, int total)
    {
        // 🔴 <b>첫 층은 예외 없이 노말이다.</b> 처음엔 이 줄이 없어서 자비 규칙(②)이
        //    0층을 상점으로 덮어썼다 — 300판 중 <b>300판</b>이 그랬다.
        //    `RollStageType` 이 0층에 Normal 을 돌려주는 것만으로는 부족했다.
        //    그 뒤에 오는 강제 규칙이 결과를 갈아 끼우기 때문이다.
        if (layer == 0)
        {
            foreach (var n in layerList) n.StageType = StageType.Normal;
            return;
        }

        int  sinceShop   = layer - _lastShopLayer;
        bool shopAllowed = sinceShop >= 2;                       // ① 연속 금지
        bool preBoss     = layer == total - 2;                   // ⑤ 보스 직전
        bool shopForced  = shopAllowed && (sinceShop >= Mathf.Max(1, shopPityLayers) || preBoss);

        for (int i = 0; i < layerList.Count; i++)
            layerList[i].StageType = RollStageType(layer, shopAllowed);

        // ② 자비 / ⑤ 보스 직전 — 아직 상점이 없으면 한 칸을 상점으로 바꾼다.
        if (shopForced && !layerList.Exists(n => n.StageType == StageType.Shop))
            layerList[Random.Range(0, layerList.Count)].StageType = StageType.Shop;

        // ④ 전부 같은 특수 노드면 한 칸을 노말로 되돌린다. 노드가 하나뿐인 층은 갈림길이
        //    아니므로 규칙을 적용하지 않는다 — 그 층은 원래 선택지가 없다.
        if (layerList.Count >= 2)
        {
            var first = layerList[0].StageType;
            if (first != StageType.Normal && layerList.TrueForAll(n => n.StageType == first))
                layerList[Random.Range(0, layerList.Count)].StageType = StageType.Normal;
        }

        if (layerList.Exists(n => n.StageType == StageType.Shop))
            _lastShopLayer = layer;
    }

    private StageType RollStageType(int layer, bool shopAllowed)
    {
        // 첫 층은 항상 노말
        if (layer == 0) return StageType.Normal;

        bool eliteAllowed = layer >= eliteMinLayer;               // ③

        // 🔑 막힌 종류의 가중치를 0 으로 만들고 남은 것끼리 다시 정규화한다.
        //    빼기만 하고 정규화를 안 하면 그만큼이 통째로 폴백(노말)으로 흘러
        //    "엘리트를 막았더니 이벤트도 줄었다" 가 된다.
        float wNormal = weightNormal;
        float wElite  = eliteAllowed ? weightElite : 0f;
        float wShop   = shopAllowed  ? weightShop  : 0f;
        float wEvent  = weightEvent;

        float sum = wNormal + wElite + wShop + wEvent;
        if (sum <= 0f) return StageType.Normal;

        float r = Random.value * sum;
        float c = 0f;

        c += wNormal; if (r < c) return StageType.Normal;
        c += wElite;  if (r < c) return StageType.Elite;
        c += wShop;   if (r < c) return StageType.Shop;
        return StageType.Event;
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
