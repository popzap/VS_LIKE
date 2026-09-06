using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 화면 상단 보스 체력 바 (D31).
///
/// <para>보스가 큰 잡몹으로 보이던 이유의 절반은 <b>얼마나 남았는지 알 수 없다</b>는 것이었다.
/// HP 2200 을 깎는 동안 화면에는 아무 표시도 없었다.</para>
///
/// <para>🔴 보스를 <c>FindObjectsByType</c> 으로 찾아다니지 않는다 —
/// <see cref="WaveManager.OnBossSpawned"/> 를 구독해 <b>등장하는 쪽이 알려 주게</b> 했다.
/// 보스는 한 판에 한 번 나오므로 그걸로 충분하고, 매 프레임 씬을 훑을 이유가 없다
/// (<c>Docs/PERF.md</c> 의 방침).</para>
///
/// <para>페이즈 칸은 <b>몇 페이즈인지가 아니라 남은 페이즈 수</b>를 보여 준다 —
/// "아직 두 번 더 남았다"가 플레이어에게 필요한 정보다.</para>
/// </summary>
public class BossHealthBarUI : MonoBehaviour
{
    [Header("연동 패널 (이 스크립트가 붙은 오브젝트는 항상 활성이어야 한다)")]
    [SerializeField] private GameObject panel;

    [Header("UI")]
    [SerializeField] private Slider          hpSlider;
    [SerializeField] private Image           hpFill;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI phaseText;

    [Header("페이즈별 색")]
    [SerializeField] private Color phase1 = new(0.85f, 0.25f, 0.25f);
    [SerializeField] private Color phase2 = new(0.95f, 0.55f, 0.15f);
    [SerializeField] private Color phase3 = new(0.95f, 0.90f, 0.25f);

    private EnemyBase _boss;
    private BossBrain _brain;
    private WaveManager _wave;

    private void Start()
    {
        // 🔴 Awake 가 아니라 Start 다. GameManager.Instance.WaveManager 는
        //    GameManager.Start 에서 채워지는데 모든 Awake 는 모든 Start 보다 먼저 돈다 (I-8/I-38).
        _wave = GameManager.Instance != null ? GameManager.Instance.WaveManager : null;
        if (_wave != null) _wave.OnBossSpawned += HandleBossSpawned;

        Hide();
    }

    private void OnDestroy()
    {
        if (_wave != null) _wave.OnBossSpawned -= HandleBossSpawned;
    }

    private void HandleBossSpawned(EnemyBase boss, BossBrain brain)
    {
        _boss  = boss;
        _brain = brain;

        if (panel != null) panel.SetActive(true);
        if (nameText != null) nameText.text = boss != null ? boss.DisplayName.ToUpperInvariant() : "BOSS";

        Refresh();
    }

    /// <summary>컴파일 반영 확인용 (D27). 값을 바꿨으면 이 숫자를 올린다.</summary>
    public const int Version = 1;

    private void Update()
    {
        if (_boss == null) return;

        // 죽었거나 풀로 돌아가 비활성이면 내린다.
        if (_boss.Dead || !_boss.gameObject.activeInHierarchy) { Hide(); return; }

        // 🔴 <b>전투 화면이 아닐 때는 내린다</b> (D104 · 사용자: "ESC 눌렀을때 맨위에 보이는데").
        //    씬 형제 순서가 <c>BossHealthBar</c>[11] > <c>PausePanel</c>[8] > <c>LevelUpPanel</c>[7] 이라
        //    체력 바가 <b>두 화면 위로 뚫고 올라온다.</b> 사용자는 ESC 에서 봤지만
        //    <b>보스전 중 레벨업에서도 같은 일이 난다</b> — 원인이 하나다.
        //
        //    🔑 <b>형제 순서를 바꾸지 않았다.</b> <c>PausePanel</c> 을 위로 올리면 그 위에 있어야 하는
        //    <c>OptionSubPanel</c>[9] 이 뒤로 밀려 옵션 창이 가려진다. 순서를 만지면 다른 짝이 깨진다.
        //    ⇒ <see cref="BossOffscreenArrowUI"/> 와 <b>같은 방식</b>으로 상태를 본다.
        //
        //    🔴 <see cref="Hide"/> 를 쓰면 안 된다 — 거긴 <c>_boss</c> 를 지워서 <b>영구히</b> 내린다.
        //    일시정지를 풀면 다시 떠야 하므로 <b>보이기만</b> 끈다.
        var gm = GameManager.Instance;
        bool inCombat = gm != null && gm.CurrentState == GameState.Wave;
        if (panel != null && panel.activeSelf != inCombat) panel.SetActive(inCombat);
        if (!inCombat) return;

        Refresh();
    }

    private void Refresh()
    {
        if (_boss == null) return;

        float f = _boss.HpFraction;
        if (hpSlider != null) hpSlider.value = f;

        int phase  = _brain != null ? _brain.Phase      : 0;
        int total  = _brain != null ? _brain.PhaseCount : 1;

        if (hpFill != null)
            hpFill.color = phase switch { 0 => phase1, 1 => phase2, _ => phase3 };

        if (phaseText != null)
        {
            // 🔴 폰트가 Static 115자다 (I-60). 문자표에 없는 글자는 빈칸으로 나온다 —
            //    그게 B4 였다. '★'(U+2605)는 문자표에 있고 '◆' 는 없다.
            //    쓸 수 있는 기호 목록은 BALANCE.md § 폰트 문자표에 있다.
            int left = Mathf.Max(0, total - phase - 1);
            phaseText.text = left > 0 ? new string('★', left) : "FINAL";
        }
    }

    private void Hide()
    {
        _boss  = null;
        _brain = null;
        if (panel != null) panel.SetActive(false);
    }
}
