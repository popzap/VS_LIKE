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

    private void Update()
    {
        if (_boss == null) return;

        // 죽었거나 풀로 돌아가 비활성이면 내린다.
        if (_boss.Dead || !_boss.gameObject.activeInHierarchy) { Hide(); return; }

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
