using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 분기형 스테이지 맵을 버튼 그래프로 그린다.
/// 노드 클릭 → <see cref="StageMapManager.SelectNode"/> → GameManager 가 웨이브/상점/이벤트로 분기.
/// </summary>
public class StageMapUI : GameStatePanel
{
    [Header("노드 배치")]
    [SerializeField] private ScrollRect    scrollRect;            // 선택 사항 (가로 스크롤)
    [SerializeField] private RectTransform nodeContainer;         // ScrollRect.content
    [SerializeField] private GameObject    nodeButtonPrefab;      // Button + Image + TMP 라벨
    [SerializeField] private GameObject    connectionLinePrefab;  // Image (pivot 0, 0.5) — 없으면 선 생략
    [SerializeField] private float         edgePadding = 200f;    // 좌우 여백

    [Header("헤더")]
    [SerializeField] private TextMeshProUGUI headerText;
    [SerializeField] private TextMeshProUGUI currencyText;

    // 스테이지 타입별 색상
    private static readonly Color ColorNormal = new(0.29f, 0.44f, 0.65f);
    private static readonly Color ColorElite  = new(0.65f, 0.33f, 0.75f);
    private static readonly Color ColorShop   = new(0.20f, 0.62f, 0.45f);
    private static readonly Color ColorEvent  = new(0.85f, 0.66f, 0.22f);
    private static readonly Color ColorBoss   = new(0.80f, 0.24f, 0.24f);
    private static readonly Color ColorLocked = new(0.28f, 0.28f, 0.32f);
    private static readonly Color ColorLine   = new(1f, 1f, 1f, 0.18f);

    protected override bool IsVisibleIn(GameState state) => state == GameState.StageMap;

    protected override void OnShown(GameState state)
    {
        Time.timeScale = 1f;
        Rebuild();
    }

    // ── 맵 생성 ─────────────────────────────────────────────

    private void Rebuild()
    {
        if (nodeContainer == null || nodeButtonPrefab == null) return;

        var map = GameManager.Instance?.StageMap;
        if (map == null || map.Layers.Count == 0) return;

        for (int i = nodeContainer.childCount - 1; i >= 0; i--)
            Destroy(nodeContainer.GetChild(i).gameObject);

        float maxX = 0f;
        foreach (var layer in map.Layers)
            foreach (var node in layer)
                maxX = Mathf.Max(maxX, node.MapPosition.x);

        float contentWidth = maxX + edgePadding * 2f;
        nodeContainer.sizeDelta = new Vector2(contentWidth, nodeContainer.sizeDelta.y);

        // 연결선을 먼저 만들어 노드 뒤에 깔리게 한다 (형제 순서 = 렌더 순서)
        if (connectionLinePrefab != null)
        {
            for (int l = 0; l < map.Layers.Count - 1; l++)
                foreach (var node in map.Layers[l])
                    foreach (int idx in node.NextNodeIndices)
                    {
                        if (idx < 0 || idx >= map.Layers[l + 1].Count) continue;
                        DrawConnection(node.MapPosition, map.Layers[l + 1][idx].MapPosition);
                    }
        }

        StageNode focus = null;
        foreach (var layer in map.Layers)
            foreach (var node in layer)
            {
                CreateNodeButton(node);
                if (focus == null && node.IsAvailable) focus = node;
            }

        if (headerText != null)
            headerText.text = focus != null ? $"Choose your path  —  Layer {focus.Layer + 1}/{map.Layers.Count}"
                                            : "Stage Map";

        if (currencyText != null)
            currencyText.text = $"{GameManager.Instance?.RunGold ?? 0} G";   // 런 골드 (상점에서 쓸 돈)

        ScrollTo(focus, contentWidth);
    }

    private void CreateNodeButton(StageNode node)
    {
        var go = Instantiate(nodeButtonPrefab, nodeContainer);
        go.SetActive(true);

        var rt = go.transform as RectTransform;
        if (rt != null)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = node.MapPosition + new Vector2(edgePadding, 0f);
        }

        var label = go.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null) label.text = GetLabel(node.StageType);

        var bg = go.GetComponent<Image>();
        if (bg != null) bg.color = node.IsAvailable ? GetColor(node.StageType) : ColorLocked;

        var button = go.GetComponent<Button>();
        if (button != null)
        {
            button.interactable = node.IsAvailable;
            var captured = node;                     // 클로저 캡처 주의
            button.onClick.AddListener(() =>
            {
                AudioManager.Play(SfxId.UiSelect);
                OnNodeClicked(captured);
            });
        }
    }

    private void DrawConnection(Vector2 from, Vector2 to)
    {
        var go = Instantiate(connectionLinePrefab, nodeContainer);
        go.SetActive(true);

        var rt = go.transform as RectTransform;
        if (rt == null) return;

        Vector2 delta = to - from;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot     = new Vector2(0f, 0.5f);
        rt.anchoredPosition = from + new Vector2(edgePadding, 0f);
        rt.sizeDelta = new Vector2(delta.magnitude, Mathf.Max(2f, rt.sizeDelta.y));
        rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

        var img = go.GetComponent<Image>();
        if (img != null) img.color = ColorLine;
    }

    private void ScrollTo(StageNode focus, float contentWidth)
    {
        if (scrollRect == null || focus == null || contentWidth <= 0f) return;
        scrollRect.horizontalNormalizedPosition =
            Mathf.Clamp01((focus.MapPosition.x + edgePadding) / contentWidth);
    }

    // ── 입력 ────────────────────────────────────────────────

    private void OnNodeClicked(StageNode node)
    {
        GameManager.Instance?.StageMap?.SelectNode(node);
    }

    // ── 표시용 헬퍼 ──────────────────────────────────────────

    private static string GetLabel(StageType type) => type switch
    {
        StageType.Elite => "Elite",
        StageType.Shop  => "Shop",
        StageType.Event => "Event",
        StageType.Boss  => "Boss",
        _               => "Battle"
    };

    private static Color GetColor(StageType type) => type switch
    {
        StageType.Elite => ColorElite,
        StageType.Shop  => ColorShop,
        StageType.Event => ColorEvent,
        StageType.Boss  => ColorBoss,
        _               => ColorNormal
    };
}
