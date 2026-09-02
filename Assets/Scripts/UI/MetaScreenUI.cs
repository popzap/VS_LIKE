using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 메타 강화 화면 (D30) — <b>재화 고리의 마지막 칸</b>이다.
///
/// <para>여기가 없어서 지금까지 메타 골드는 <b>벌기만 하고 쓸 데가 없었다.</b>
/// 고리는 이렇게 닫힌다:
/// 스테이지 클리어 → <c>GameManager.SettleRun</c> → <c>MetaProgression.Currency</c> →
/// <b>이 화면에서 소비</b> → <c>MetaProgressionManager.GetStatBonus()</c> →
/// <c>PlayerStats.cs:228</c> 에서 다음 런에 반영.</para>
///
/// <para>⚠️ <see cref="GameStatePanel"/> 규칙대로 이 스크립트가 붙은 오브젝트는 <b>항상 활성</b>이어야 한다.
/// 실제로 켜고 끄는 건 <c>panel</c> 이다 — 꺼진 오브젝트는 <c>Awake</c> 가 안 돌아 상태 구독을 놓친다.</para>
/// </summary>
public class MetaScreenUI : GameStatePanel
{
    [Header("카드")]
    [SerializeField] private Transform     cardContent;   // 카드 부모 (Grid/Vertical Layout)
    [SerializeField] private UpgradeCardUI cardPrefab;

    [Header("텍스트")]
    [SerializeField] private TextMeshProUGUI currencyText;
    [SerializeField] private TextMeshProUGUI emptyHintText;   // 항목이 하나도 없을 때

    [Header("버튼")]
    [SerializeField] private Button backButton;

    private readonly List<UpgradeCardUI> _cards = new();

    protected override bool IsVisibleIn(GameState state) => state == GameState.MetaScreen;

    protected override void Awake()
    {
        base.Awake();

        // MainMenuUI 와 같은 이유로 Awake 에서 건다 — GameManager.Start 가 곧바로 상태를 방송한다.
        if (backButton != null) backButton.onClick.AddListener(OnBackClicked);
    }

    protected override void OnShown(GameState state)
    {
        Time.timeScale = 1f;
        Rebuild();
    }

    /// <summary>
    /// 카드를 처음 한 번만 만들고, 그 뒤에는 <see cref="Refresh"/> 로 값만 갱신한다.
    ///
    /// <para>매번 <c>Destroy</c> → <c>Instantiate</c> 하면 구매할 때마다 레이아웃이 튀고
    /// 버튼 포커스가 날아간다. 항목 수는 런 중에 바뀌지 않으므로 다시 만들 이유가 없다.</para>
    /// </summary>
    private void Rebuild()
    {
        var meta = GameManager.Instance != null ? GameManager.Instance.MetaProgression : null;
        var defs = meta != null ? meta.Upgrades : null;

        bool empty = defs == null || defs.Length == 0;
        if (emptyHintText != null)
        {
            emptyHintText.gameObject.SetActive(empty);
            if (empty) emptyHintText.text = "No upgrades available.";
        }
        if (empty) { RefreshCurrency(0); return; }

        if (_cards.Count == 0 && cardPrefab != null && cardContent != null)
        {
            foreach (var def in defs)
            {
                if (def == null) continue;
                var card = Instantiate(cardPrefab, cardContent);
                _cards.Add(card);
            }
        }

        Refresh();
    }

    /// <summary>보유 골드와 각 항목의 레벨을 다시 읽어 카드에 반영한다.</summary>
    private void Refresh()
    {
        var meta = GameManager.Instance != null ? GameManager.Instance.MetaProgression : null;
        if (meta == null) return;

        var defs = meta.Upgrades;
        int gold = meta.Currency;

        for (int i = 0; i < _cards.Count && i < defs.Length; i++)
        {
            var def = defs[i];
            if (def == null) continue;
            _cards[i].Setup(def, meta.GetUpgradeLevel(def.UpgradeId), gold, OnBuyClicked);
        }

        RefreshCurrency(gold);
    }

    private void RefreshCurrency(int gold)
    {
        if (currencyText != null) currencyText.text = $"{gold} G";
    }

    // ── 버튼 핸들러 ──────────────────────────────────────────

    private void OnBuyClicked(UpgradeDefinition def)
    {
        var meta = GameManager.Instance != null ? GameManager.Instance.MetaProgression : null;
        if (meta == null) return;

        // PurchaseUpgrade 는 상한·잔액을 스스로 검사하고 Save() 까지 한다.
        // 실패해도 조용히 false 다 — 버튼이 이미 비활성이라 정상 경로에서는 안 나온다.
        if (!meta.PurchaseUpgrade(def))
        {
            AudioManager.Play(SfxId.UiCancel);
            return;
        }

        Refresh();
    }

    private void OnBackClicked()
    {
        AudioManager.Play(SfxId.UiCancel);
        GameManager.Instance.ChangeState(GameState.MainMenu);
    }
}
