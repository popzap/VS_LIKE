using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 조작 안내 — 메인 메뉴에서 연다 (D83 · 사용자 요구).
///
/// <para>사용자 요구는 F7/F8 화면 배율을 만들면서 나왔다 —
/// *"해당 배율 조절 조작에 대해서는 메인화면에서 도움말 페이지에서 안내해줘"*.
/// 🔑 새 조작을 만들었으면 <b>그걸 알 방법도 같이 만들어야</b> 한다.
/// 지금까지 조작은 어디에도 적혀 있지 않았다 — <c>Z</c>·<c>TAB</c>·<c>ESC</c> 도 마찬가지다.</para>
///
/// <para>🔑 <b>본문을 코드가 들고 있다</b> — <see cref="CreditsPanel"/> 과 같은 방식이다.
/// 씬에 글자를 박아 두면 <c>B11</c> 처럼 <b>씬이 코드를 덮어</b> 나중에 고쳐도 화면이 안 바뀐다.
/// <c>OnEnable</c> 마다 코드 값을 씌워 <b>코드가 항상 이긴다.</b></para>
///
/// <para>🔴 문자열은 영문이다 — 폰트가 Static 115자라 한글 글리프가 없다 (I-60).
/// 화살표(→ U+2192)와 가운뎃점(· U+00B7)은 문자표에 있으므로 써도 된다.</para>
/// </summary>
public class HelpPanel : MonoBehaviour
{
    // 🔴 컴파일 반영 확인용 (D27).
    public const int Version = 1;

    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private Button          closeButton;

    /// <summary>
    /// 🔴 <b>코드가 원본</b>이다 (B11 의 교훈). 고칠 때는 여기만 고친다.
    ///
    /// <para>순서는 <b>배우는 순서</b>다 — 움직이고, 싸우고(자동), 고르고, 그 다음이 편의다.
    /// 배율은 사용자가 물어본 항목이라 <b>따로 한 줄을 준다.</b></para>
    /// </summary>
    private const string Body =
        "CONTROLS\n" +
        "\n" +
        "WASD  ·  Arrow keys      Move\n" +
        "Weapons fire automatically.\n" +
        "\n" +
        "1  2  3                  Pick a level-up card\n" +
        "Z                        Place a building\n" +
        "TAB                      Stats and items (slows time)\n" +
        "ESC                      Pause\n" +
        "\n" +
        "SCREEN\n" +
        "\n" +
        "F7                       Zoom in   (see less, bigger)\n" +
        "F8                       Zoom out  (see more, smaller)\n" +
        "Your choice is remembered between runs.\n" +
        "\n" +
        "TIPS\n" +
        "\n" +
        "Each class has its own slot limits.\n" +
        "The row under your health bar shows what you\n" +
        "carry and how many slots are left.\n" +
        "\n" +
        "A shop node lets you buy, remove, or reroll.\n" +
        "When there is nothing left to buy it offers\n" +
        "rest and gold exchange instead.";

    private void Start()
    {
        if (bodyText != null) bodyText.text = Body;
        if (closeButton != null) closeButton.onClick.AddListener(() => gameObject.SetActive(false));
    }

    // 패널을 다시 열 때도 코드 값이 이기게 한다.
    private void OnEnable()
    {
        if (bodyText != null) bodyText.text = Body;
    }
}
