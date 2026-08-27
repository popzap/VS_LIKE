using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 직업 선택 화면. 메인 메뉴에서 Start 를 누르면 열리고, 여기서 고른 직업으로 런이 시작된다.
///
/// <para>레이아웃 (초안)
/// <list type="bullet">
/// <item>좌상단 — 직업 칸 그리드 (<c>cardParent</c> 에 <c>GridLayoutGroup</c>)</item>
/// <item>우측 — 선택한 직업의 일러스트 패널</item>
/// <item>좌하단 — 간단 설명 바</item>
/// </list></para>
///
/// <para><b>Retry 는 여기를 거치지 않는다.</b> <see cref="GameManager.ReloadScene"/> 이
/// 곧바로 <c>StartRun()</c> 을 부르므로 직전에 고른 직업이 그대로 유지된다.</para>
/// </summary>
public class ClassSelectUI : GameStatePanel
{
    [Header("직업 칸")]
    [Tooltip("GridLayoutGroup 이 붙은 부모. 여기에 카드가 복제된다.")]
    [SerializeField] private Transform    cardParent;
    [SerializeField] private ClassCardUI  cardPrefab;

    [Header("일러스트 패널 (우측)")]
    [SerializeField] private Image            portraitImage;
    [SerializeField] private TextMeshProUGUI  classNameText;
    [Tooltip("일러스트가 없을 때 대신 보여줄 자리표시 오브젝트")]
    [SerializeField] private GameObject       portraitPlaceholder;

    [Header("설명 바 (하단)")]
    [SerializeField] private TextMeshProUGUI descriptionText;
    [Tooltip("시작 무기 / 스탯 보너스 요약. 없으면 설명 바에 합쳐 쓴다.")]
    [SerializeField] private TextMeshProUGUI statSummaryText;

    [Header("버튼")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button backButton;

    private readonly List<ClassCardUI> _cards = new();
    private int _selected = -1;

    protected override bool IsVisibleIn(GameState state) => state == GameState.ClassSelect;

    protected override void Awake()
    {
        base.Awake();

        if (startButton) startButton.onClick.AddListener(OnStartClicked);
        if (backButton)  backButton .onClick.AddListener(OnBackClicked);
    }

    protected override void OnShown(GameState state)
    {
        Time.timeScale = 1f;
        BuildCards();
        Select(Mathf.Max(0, GameManager.Instance.SelectedClassIndex));
    }

    // ── 카드 목록 ────────────────────────────────────────────

    private void BuildCards()
    {
        var classes = GameManager.Instance?.Classes;

        // 매번 다시 만든다. 목록이 바뀌는 일은 거의 없지만 상태가 남는 것보다 낫다.
        foreach (var c in _cards) if (c != null) Destroy(c.gameObject);
        _cards.Clear();

        if (classes == null || classes.Length == 0)
        {
            Debug.LogWarning("[ClassSelectUI] GameManager.classes 가 비어 있다. " +
                             "SceneWiring.csv 의 GameManager,classes 를 확인할 것.");
            return;
        }
        if (cardPrefab == null || cardParent == null)
        {
            Debug.LogWarning("[ClassSelectUI] cardPrefab / cardParent 가 연결되지 않았다.");
            return;
        }

        for (int i = 0; i < classes.Length; i++)
        {
            if (classes[i] == null) continue;

            var card = Instantiate(cardPrefab, cardParent);
            card.Setup(classes[i], i, IsUnlocked(classes[i]), Select);
            _cards.Add(card);
        }
    }

    /// <summary>
    /// 해금 여부. 해금 UI·구매 흐름이 아직 없어서 지금은 <c>UnlockedByDefault</c> 만 본다.
    /// (<c>MetaProgressionManager.UnlockCharacter</c> 와 이어붙일 자리 → TODO.md §2)
    /// </summary>
    private static bool IsUnlocked(CharacterClassData cls) => cls.UnlockedByDefault;

    // ── 선택 ─────────────────────────────────────────────────

    private void Select(int index)
    {
        var classes = GameManager.Instance?.Classes;
        if (classes == null || index < 0 || index >= classes.Length) return;

        _selected = index;
        GameManager.Instance.SelectClass(index);

        foreach (var c in _cards) c.SetSelected(c.Index == index);

        ShowDetail(classes[index]);
    }

    private void ShowDetail(CharacterClassData cls)
    {
        if (classNameText != null)
            classNameText.text = string.IsNullOrEmpty(cls.ClassName) ? cls.name : cls.ClassName;

        if (portraitImage != null)
        {
            portraitImage.sprite  = cls.Portrait;
            portraitImage.enabled = cls.Portrait != null;
        }
        // 일러스트가 준비되기 전까지는 자리표시가 대신 뜬다.
        if (portraitPlaceholder != null) portraitPlaceholder.SetActive(cls.Portrait == null);

        string summary = BuildSummary(cls);

        if (statSummaryText != null)
        {
            if (descriptionText != null) descriptionText.text = cls.Description;
            statSummaryText.text = summary;
        }
        else if (descriptionText != null)
        {
            // 요약 칸이 따로 없으면 설명 바에 두 줄로 합친다.
            descriptionText.text = string.IsNullOrEmpty(summary)
                ? cls.Description
                : $"{cls.Description}\n{summary}";
        }

        if (startButton != null) startButton.interactable = IsUnlocked(cls);
    }

    /// <summary>시작 무기 + 0이 아닌 스탯 보너스만 한 줄로 요약한다.</summary>
    private static string BuildSummary(CharacterClassData cls)
    {
        var parts = new List<string>();

        if (cls.StartingWeapon != null)
        {
            string wp = string.IsNullOrEmpty(cls.StartingWeapon.WeaponName)
                ? cls.StartingWeapon.name
                : cls.StartingWeapon.WeaponName;
            parts.Add($"Weapon: {wp}");
        }

        Add(parts, "HP",      cls.BonusMaxHp);
        Add(parts, "Speed",   cls.BonusMoveSpeed);
        Add(parts, "Damage",  cls.BonusDamage);
        // AttackSpeed 는 쿨다운 배율이라 음수가 "더 빠름"이다. 표시도 뒤집어 준다.
        Add(parts, "Attack Speed", -cls.BonusAttackSpeed);
        Add(parts, "Size",    cls.BonusProjectileSize);
        Add(parts, "Pickup",  cls.BonusPickupRadius);
        Add(parts, "Crit",    cls.BonusCritChance);
        Add(parts, "Armor",   cls.BonusArmor);
        Add(parts, "XP",      cls.BonusXpGain);
        Add(parts, "Gold",    cls.BonusGoldGain);

        return string.Join("   ", parts);
    }

    private static void Add(List<string> parts, string label, float value)
    {
        if (Mathf.Approximately(value, 0f)) return;
        string color = value > 0 ? "#7FD87F" : "#E88";
        parts.Add($"<color={color}>{label} {value:+0.##;-0.##}</color>");
    }

    // ── 버튼 ─────────────────────────────────────────────────

    private void OnStartClicked()
    {
        if (_selected < 0) return;
        Time.timeScale = 1f;
        GameManager.Instance.StartRun();
    }

    private void OnBackClicked() => GameManager.Instance.ChangeState(GameState.MainMenu);
}
