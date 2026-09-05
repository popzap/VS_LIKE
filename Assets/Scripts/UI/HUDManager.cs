using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 인게임 HUD 전체를 관리한다.
/// ┌──────────────────────────────────────────────────────────┐
/// │ [포트레이트+HP바]      [Lv.N / XP바]      [톱니바퀴]  │
/// └──────────────────────────────────────────────────────────┘
/// </summary>
public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; private set; }

    // 🔴 컴파일 반영 확인용 (D27).
    public const int Version = 3;   // 3 = 직업 초상화 + 보유 아이템 줄 (D82) · 2 = 레벨업 파동 (D43)

    // ── 포트레이트 (좌측 상단) ───────────────────────────────
    [Header("포트레이트")]
    [SerializeField] private Image   portraitImage;        // 원형 마스크 안의 얼굴 이미지
    [SerializeField] private Sprite  faceHealthy;          // HP 75% 이상
    [SerializeField] private Sprite  faceNormal;           // HP 50~75%
    [SerializeField] private Sprite  faceWorried;          // HP 25~50%
    [SerializeField] private Sprite  faceCritical;         // HP 25% 이하
    [SerializeField] private Image   statusDot;            // 포트레이트 우하단 상태 점

    // ── 보유 아이템 줄 (D82 · 사용자 요구 2) ──────────────────
    [Header("보유 아이템 줄 — HP 바 아래")]
    [Tooltip("칩이 담길 자리. 가로 레이아웃 그룹을 붙여 둔다.")]
    [SerializeField] private Transform  itemSlotRow;
    [Tooltip("Prefab_ItemChip. StatsPanel(TAB)이 쓰는 것과 같은 프리팹이다.")]
    [SerializeField] private GameObject itemChipPrefab;

    private readonly System.Collections.Generic.List<ItemChipUI> _slotChips = new();
    private int _slotSignature = -1;   // 재구축 여부만 가른다 (매 프레임 다시 만들지 않기 위해)

    // ── HP 바 ────────────────────────────────────────────────
    [Header("HP 바")]
    [SerializeField] private Slider           hpSlider;
    [SerializeField] private Image            hpFill;
    [SerializeField] private TextMeshProUGUI  hpText;

    private static readonly Color ColorHpHigh    = new(0.13f, 0.77f, 0.37f); // 초록
    private static readonly Color ColorHpMid     = new(0.92f, 0.70f, 0.00f); // 노랑
    private static readonly Color ColorHpLow     = new(0.96f, 0.60f, 0.09f); // 주황
    private static readonly Color ColorHpCrit    = new(0.93f, 0.30f, 0.29f); // 빨강

    // ── 경험치 / 레벨 (상단 중앙) ───────────────────────────
    [Header("레벨 & 경험치")]
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Slider          xpSlider;
    [SerializeField] private TextMeshProUGUI xpText;

    // ── 웨이브 정보 (상단 중앙 / 우측 상단) ─────────────────
    [Header("웨이브 정보")]
    [SerializeField] private TextMeshProUGUI timerText;      // 클리어까지 남은 시간
    [SerializeField] private TextMeshProUGUI killText;       // 런 누적 처치 수
    [SerializeField] private TextMeshProUGUI currencyText;   // 보유 골드

    // 타이머가 이 시간 아래로 내려가면 붉게 물들여 "곧 끝난다"를 알린다.
    private const float TimerUrgentThreshold = 10f;
    private static readonly Color ColorTimerNormal = new(0.92f, 0.92f, 0.88f);
    private static readonly Color ColorTimerUrgent = new(0.96f, 0.35f, 0.30f);

    // ── 설치 대기 건물 (B3) ─────────────────────────────────
    //
    // `Z` 를 누르면 대기열 맨 앞이 나가는데, 그게 뭔지 화면에 안 나와서
    // "Village 를 얻었는데 Turret 이 나온다"로 보였다. 대기열은 정상 동작 중이고
    // (건물은 레벨이 오르면 MaxCount 가 늘어 대기열에 조용히 더 쌓인다)
    // 진짜 문제는 **무엇이 설치될지 모른다**는 것이라 표시만 한다.
    [Header("설치 대기 건물")]
    [SerializeField] private TextMeshProUGUI buildPromptText;

    // ── 옵션 버튼 (우측 상단) ───────────────────────────────
    [Header("옵션 버튼")]
    [SerializeField] private Button optionsButton;

    // ── 레퍼런스 ─────────────────────────────────────────────
    private PlayerStats        _playerStats;
    private ExperienceManager  _expManager;

    // ── 레벨업 연출 ──────────────────────────────────────────
    [Header("레벨업 연출")]
    [SerializeField] private Animator levelUpAnimator;    // "LevelUp" 트리거가 있는 Animator
    [Tooltip("레벨업 파동(Fx_LevelUp). SceneWiring.csv 의 HUDManager,levelUpEffect 로 배선한다. "
           + "🔴 여기서 바로 띄우지 않는다 — PlayLevelUpEffect() 를 LevelUpManager 가 부른다.")]
    [SerializeField] private GameObject levelUpEffect;

    // ────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // 🔴 [Z] Build 안내를 <b>가로 가운데 · 바닥에서 1/3</b> 높이로 (D82 · 사용자 요구).
        //    `D65` 는 좌하단으로 뺐고 사용자 판정은 *"읽힌다"* 였다. 그런데 이번 요구가
        //    *"중앙 아래로 — 맨 아래 말고 중앙에서 아래 2:1 느낌으로"* 다.
        //    🔑 `D65` 가 피하려던 건 "하단 중앙"이 아니라 **플레이어 바로 아래**였는데,
        //    바닥에서 1/3 이면 플레이어(화면 중앙)보다 충분히 아래라 겹치지 않는다.
        PromptCorner.PlaceCenterLower(buildPromptText);

        _playerStats = FindFirstObjectByType<PlayerStats>();
        _expManager  = ExperienceManager.Instance;

        optionsButton.onClick.AddListener(PauseMenuUI.Instance.Open);

        // 이벤트 구독
        _expManager.OnXpChanged  += OnXpChanged;
        _expManager.OnLevelUp    += OnLevelUp;

        // 초기값 세팅
        RefreshHP();
        RefreshXP();
    }

    private void Update()
    {
        RefreshWaveInfo();   // 타이머 / 킬 / 골드

        if (_playerStats == null) return;
        RefreshHP();   // HP는 매 프레임 체크 (공격 받을 때마다 변함)
        RefreshItemSlots();
    }

    /// <summary>
    /// HP 바 아래 <b>보유 아이템 줄</b> (D82 · 사용자 요구 2).
    ///
    /// <para>요구는 *"현재 소유한 아이템들을 박스 안에 아이콘으로 — 그러면 상한까지 몇 개
    /// 남았는지 확인하기 쉽다"* 였다. 🔑 <b>남은 자리가 보여야 한다</b>는 게 핵심이라
    /// 가진 것만이 아니라 <b>상한만큼</b> 칸을 그린다. 분류 순서는 무기 → 패시브 → 건물.</para>
    ///
    /// <para>🔑 <b>새 부품이 없다</b> — 칩은 TAB 스탯 창이 쓰는 <c>Prefab_ItemChip</c> 그대로고
    /// 상한은 <see cref="PlayerStats.SlotLimit"/>, 보유 수는 <see cref="LevelUpManager.CountOwned"/> 다.
    /// 🟢 그래서 이 줄이 <b>소지 상한이 실제로 도는지 보여 주는 화면</b>이기도 하다.</para>
    ///
    /// <para>🔴 <b>매 프레임 다시 만들지 않는다.</b> 보유 구성이 바뀌었을 때만 재구축한다 —
    /// 그 판정을 위해 (종류 수 · 레벨 합 · 상한 합)으로 만든 <b>서명</b>을 비교한다.
    /// 이벤트를 안 쓴 이유는 상점 구매·레벨업·승급 <b>세 경로</b>가 모두 인벤토리를 바꾸는데
    /// 그중 하나만 놓쳐도 줄이 조용히 낡기 때문이다.</para>
    /// </summary>
    private void RefreshItemSlots()
    {
        if (itemSlotRow == null || itemChipPrefab == null) return;

        var lm = GameManager.Instance != null ? GameManager.Instance.LevelUpManager : null;
        var ps = _playerStats;
        if (lm == null || ps == null) return;

        // 🔴 <b>직업이 없으면 아무것도 그리지 않는다</b> (D83 — 이걸 빠뜨려 에디터를 두 번 멈췄다).
        //    <see cref="PlayerStats.SlotLimit"/> 는 <c>_classChain</c> 이 비면 <b>int.MaxValue</b> 를 돌려준다
        //    ("직업이 없으면 제한하지 않는다" — 카드가 안 뜨는 걸 막으려고 그렇게 만든 값이다).
        //    그 값을 아래 루프의 상한으로 쓰면 <b>한 프레임에 21억 개를 Instantiate</b> 하려 든다.
        //    메인 메뉴에서 바로 그 상태다.
        if (ps.ClassChain.Count == 0)
        {
            for (int i = 0; i < _slotChips.Count; i++) _slotChips[i].gameObject.SetActive(false);
            _slotSignature = -1;
            return;
        }

        var inv = lm.Inventory;

        // ── 서명 ─────────────────────────────────────────────
        int sig = 17;
        foreach (var cat in Categories)
            sig = sig * 31 + ps.SlotLimit(cat);
        if (inv != null)
            foreach (var kv in inv)
            {
                if (kv.Key == null) continue;
                sig = sig * 31 + kv.Key.GetInstanceID();
                sig = sig * 31 + kv.Value;
            }
        if (sig == _slotSignature) return;
        _slotSignature = sig;

        // ── 다시 그린다 ──────────────────────────────────────
        int used = 0;
        foreach (var cat in Categories)
        {
            // 🔴 <b>상한을 한 번 더 자른다.</b> 위에서 "직업 없음"은 걸렀지만,
            //    이 값이 <c>int.MaxValue</c> 가 될 수 있는 함수라는 사실 자체가 위험하다 —
            //    <b>루프의 상한은 그 값을 믿지 않고 내가 정한다.</b>
            //    지금 가장 큰 직업이 한 분류에 8칸(Mage 패시브)이라 12 면 넉넉하다.
            int limit = Mathf.Clamp(ps.SlotLimit(cat), 0, MaxSlotsPerCategory);
            int filled = 0;

            if (inv != null)
                foreach (var kv in inv)
                {
                    if (kv.Key == null || kv.Key.Category != cat) continue;
                    if (filled >= limit) break;          // 상한을 넘겨 그리지 않는다
                    var chip = GetSlotChip(used++);
                    chip.gameObject.SetActive(true);
                    chip.Bind(kv.Key, kv.Value);
                    filled++;
                }

            for (int i = filled; i < limit; i++)
            {
                var chip = GetSlotChip(used++);
                chip.gameObject.SetActive(true);
                chip.BindEmpty();
            }
        }

        for (int i = used; i < _slotChips.Count; i++)
            _slotChips[i].gameObject.SetActive(false);
    }

    private static readonly ItemCategory[] Categories =
        { ItemCategory.Weapon, ItemCategory.Passive, ItemCategory.Building };

    /// <summary>HUD 줄에 쓸 칩 한 칸의 크기(px). 아래 주석의 계산이 이 값을 정한다.</summary>
    private const float SlotChipSize = 44f;

    /// <summary>
    /// 한 분류에 그릴 수 있는 최대 칸 수 — <b>안전망이다</b> (D83).
    /// 지금 가장 큰 값이 Mage 의 패시브 8칸이라 12 면 넉넉하다.
    /// </summary>
    private const int MaxSlotsPerCategory = 12;

    private ItemChipUI GetSlotChip(int index)
    {
        while (_slotChips.Count <= index)
        {
            var go   = Instantiate(itemChipPrefab, itemSlotRow);
            var chip = go.GetComponent<ItemChipUI>();
            if (chip == null) chip = go.AddComponent<ItemChipUI>();

            // 🔴 프리팹은 TAB 스탯 창용이라 76x96 이다. 상한 합이 최대 14칸(Mage)이라
            //    그대로 쓰면 1116px 이 필요한데 HUD 에 난 자리는 660px 뿐이다.
            //    44px 로 줄이고(14x44 + 13x3 = 655) 글자는 끈다 — 그 크기에선 안 읽힌다.
            var le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            le.preferredWidth  = SlotChipSize;
            le.preferredHeight = SlotChipSize;
            le.minWidth        = SlotChipSize;
            le.minHeight       = SlotChipSize;

            chip.SetCompact(true);
            _slotChips.Add(chip);
        }
        return _slotChips[index];
    }

    private void OnDestroy()
    {
        if (_expManager == null) return;
        _expManager.OnXpChanged -= OnXpChanged;
        _expManager.OnLevelUp   -= OnLevelUp;
    }

    // ── 웨이브 정보 갱신 (타이머 / 킬 / 골드) ────────────────
    //
    // ⚠️ WaveManager 를 이벤트로 구독하지 않고 폴링한다.
    //    GameManager.Instance.WaveManager 는 GameManager.Start() 에서 채워지는데
    //    모든 Awake 가 모든 Start 보다 먼저 돌고, Start 끼리의 순서는 보장되지 않는다.
    //    Start 에서 구독하면 조용히 null 을 잡고 기능만 죽는다 (I-8 · I-38 과 같은 함정).

    private int _lastKills    = -1;
    private int _lastCurrency = -1;
    private int _lastTimerSec = -1;

    private void RefreshWaveInfo()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        // 남은 시간 ─ 초 단위가 바뀔 때만 문자열을 다시 만든다 (매 프레임 할당 방지)
        if (timerText != null)
        {
            var wave = gm.WaveManager;
            bool show = wave != null && wave.IsWaveActive && wave.WaveRemainingTime >= 0f;

            if (timerText.gameObject.activeSelf != show)
                timerText.gameObject.SetActive(show);

            if (show)
            {
                int sec = Mathf.CeilToInt(wave.WaveRemainingTime);
                if (sec != _lastTimerSec)
                {
                    _lastTimerSec  = sec;
                    timerText.text  = $"{sec / 60:0}:{sec % 60:00}";
                    timerText.color = sec <= TimerUrgentThreshold ? ColorTimerUrgent : ColorTimerNormal;
                }
            }
            else _lastTimerSec = -1;
        }

        // 처치 수 ─ 런 누적
        if (killText != null)
        {
            var wave  = gm.WaveManager;
            int kills = wave != null ? wave.TotalKillCount : 0;
            if (kills != _lastKills)
            {
                _lastKills   = kills;
                killText.text = $"{kills} Kills";
            }
        }

        // 골드
        if (currencyText != null)
        {
            // HUD 는 **런 골드**를 보여 준다 — 지금 상점에서 쓸 수 있는 돈이 이쪽이다 (TODO §2-B).
            int gold = gm.RunGold;
            if (gold != _lastCurrency)
            {
                _lastCurrency     = gold;
                currencyText.text = $"{gold} G";
            }
        }

        RefreshBuildPrompt(gm);
    }

    // ── 설치 대기 건물 (B3) ──────────────────────────────────

    private BuildingData _lastPending;
    private int          _lastPendingCount = -1;

    /// <summary>
    /// 다음에 `Z` 로 나갈 건물을 알린다.
    ///
    /// <para>원래 대기열이 순수 FIFO 라, 레벨업으로 조용히 쌓인 예전 건물이
    /// 새로 얻은 건물보다 먼저 나왔다(B3). 이 표시는 그 <b>B안</b>이었다 —
    /// 순서는 그대로 두고 무엇이 나올지 보이게만 했다.
    /// 순서 자체는 나중에 <b>C안</b>으로 고쳤다(<see cref="BuildingManager"/> 참고):
    /// 처음 해금된 것이 증설분보다 먼저 나온다.</para>
    ///
    /// <para>그래도 이 표시는 남긴다. 대기열이 여러 개일 때 <b>다음에 무엇이 나오는지</b>는
    /// 여전히 알 수 없기 때문이다.</para>
    /// </summary>
    private void RefreshBuildPrompt(GameManager gm)
    {
        if (buildPromptText == null) return;

        var bm      = gm.BuildingMgr;
        var pending = bm != null ? bm.NextPending : null;
        int count   = bm != null ? bm.PendingCount : 0;

        if (pending == null || count <= 0)
        {
            if (buildPromptText.gameObject.activeSelf)
                buildPromptText.gameObject.SetActive(false);
            _lastPending      = null;
            _lastPendingCount = -1;
            return;
        }

        if (!buildPromptText.gameObject.activeSelf)
            buildPromptText.gameObject.SetActive(true);

        // 문자열은 바뀔 때만 만든다 — 이 메서드는 매 프레임 돈다.
        if (pending == _lastPending && count == _lastPendingCount) return;
        _lastPending      = pending;
        _lastPendingCount = count;

        string name = string.IsNullOrEmpty(pending.BuildingName) ? pending.name : pending.BuildingName;
        // 남은 수를 같이 보여야 "왜 새로 얻은 게 안 나오나"가 설명된다.
        buildPromptText.text = count > 1 ? $"[Z] Build: {name}  (+{count - 1} queued)"
                                         : $"[Z] Build: {name}";
    }

    // ── HP 갱신 ──────────────────────────────────────────────

    private float _lastHp = -1;

    private void RefreshHP()
    {
        if (_playerStats == null) return;
        float hp    = _playerStats.CurrentHp;
        float maxHp = _playerStats.Final.MaxHp;
        if (Mathf.Approximately(hp, _lastHp)) return;
        _lastHp = hp;

        float ratio = Mathf.Clamp01(hp / maxHp);
        hpSlider.value = ratio;
        hpText.text    = $"{Mathf.CeilToInt(hp)} / {Mathf.RoundToInt(maxHp)}";

        // 색상 + 표정
        UpdateHpColor(ratio);
        UpdatePortrait(ratio);
    }

    private void UpdateHpColor(float ratio)
    {
        if      (ratio > 0.75f) hpFill.color = ColorHpHigh;
        else if (ratio > 0.50f) hpFill.color = ColorHpMid;
        else if (ratio > 0.25f) hpFill.color = ColorHpLow;
        else                    hpFill.color = ColorHpCrit;
    }

    /// <summary>
    /// 초상화 칸을 채운다 (D82 · 사용자 요구 3: *"캐릭터 얼굴 박스가 아직 비어있는 상태"*).
    ///
    /// <para>🔴 <b>비어 있던 이유는 그림이 없어서다.</b> 이 함수는 HP 4단계에 따라
    /// <see cref="faceHealthy"/>~<see cref="faceCritical"/> 를 갈아 끼우게 돼 있는데
    /// <b>넷 다 미할당</b>이라 매번 <c>sprite = null</c> 을 넣고 있었다.</para>
    ///
    /// <para>🔑 <b>새 그림을 만들지 않았다</b> — 직업 10종이 이미 `Portrait` 를 갖고 있다
    /// (직업 선택 화면이 쓰는 그 그림이다). 지금 직업의 것을 그대로 쓴다.</para>
    ///
    /// <para>🔴 <b>매번 다시 읽는다.</b> 캐시하면 승급으로 직업이 바뀌었을 때 옛 얼굴이 남는다.
    /// HP 가 바뀔 때만 불리는 함수라 비용도 문제되지 않는다.</para>
    /// </summary>
    private void UpdatePortrait(float ratio)
    {
        if (portraitImage == null) return;

        Sprite face = null;

        // ① 얼굴 그림이 실제로 있으면 예전대로 HP 4단계를 쓴다.
        if      (ratio > 0.75f) face = faceHealthy;
        else if (ratio > 0.50f) face = faceNormal;
        else if (ratio > 0.25f) face = faceWorried;
        else                    face = faceCritical;

        // ② 없으면 지금 직업의 초상화로 대신한다.
        if (face == null)
        {
            var ps = PlayerStats.Current;
            if (ps != null && ps.ClassChain.Count > 0)
            {
                var cur = ps.ClassChain[ps.ClassChain.Count - 1];   // 승급했으면 마지막이 지금 직업
                if (cur != null) face = cur.Portrait;
            }
        }

        portraitImage.sprite  = face;
        portraitImage.enabled = face != null;   // 없으면 빈 사각형을 그리지 않는다

        // 상태 점 색상
        if (statusDot != null)
            statusDot.color = ratio > 0.25f ? ColorHpHigh : ColorHpCrit;
    }

    // ── XP 갱신 ──────────────────────────────────────────────

    private void OnXpChanged(int current, int toNext)
    {
        xpSlider.value = toNext > 0 ? (float)current / toNext : 0f;
        xpText.text    = $"{current} / {toNext}";
    }

    private void OnLevelUp(int newLevel)
    {
        levelText.text = $"Lv. {newLevel}";

        // ⚠️ C# 의 ?. 은 Unity 의 "가짜 null"(미할당 직렬화 필드)을 걸러내지 못한다.
        // 반드시 != null 로 검사할 것. ?. 를 쓰면 UnassignedReferenceException 이 터지고,
        // 그 예외가 CollectXp 밖으로 전파되어 레벨업 패널이 아예 안 뜬다.
        if (levelUpAnimator != null) levelUpAnimator.SetTrigger("LevelUp");

        // 🔴 파동은 여기서 안 띄운다 (D43).
        //    이 함수가 도는 바로 그 프레임에 레벨업 패널이 열리는데, 캔버스가
        //    ScreenSpaceOverlay 라 월드 스프라이트는 무조건 그 아래다 — sortingOrder 로도 못 이긴다.
        //    D41 에서 파동이 "생기기는 하는데 한 번도 안 보이는" 상태였다.
        //    ⇒ 패널이 실제로 닫히는 시점에 LevelUpManager 가 PlayLevelUpEffect() 를 부른다.
    }

    /// <summary>
    /// 레벨업 파동을 플레이어 자리에 띄운다. <see cref="LevelUpManager.HidePanel"/> 이 부른다.
    ///
    /// <para>🔴 <c>?.</c> 를 쓰지 않는다 — 미할당 직렬화 필드는 "가짜 null" 이라
    /// <c>?.</c> 가 통과시키고 예외가 호출 사슬 밖으로 샌다 (I-24).
    /// 여기서 새면 <b>레벨업 패널이 닫히다 만다.</b></para>
    /// </summary>
    public void PlayLevelUpEffect()
    {
        if (levelUpEffect == null) return;
        var player = FindFirstObjectByType<PlayerController>();
        if (player == null) return;
        Instantiate(levelUpEffect, player.transform.position, Quaternion.identity);
    }

    private void RefreshXP()
    {
        if (_expManager == null) return;
        levelText.text = $"Lv. {_expManager.CurrentLevel}";
        OnXpChanged(_expManager.CurrentXp, _expManager.XpToNext);
    }
}
