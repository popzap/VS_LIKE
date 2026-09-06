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
/// 가운뎃점(· U+00B7)은 문자표에 있으므로 써도 된다.</para>
/// </summary>
public class HelpPanel : MonoBehaviour
{
    // 🔴 컴파일 반영 확인용 (D27).
    public const int Version = 5;   // 5 = TAB 폐지 (D92) · 4 = 넓힌 카드 + 글자 27 (D86) · 3 = 2단 배치 · 2 = 여백

    [Tooltip("왼쪽 단 — CONTROLS")]
    [SerializeField] private TextMeshProUGUI bodyText;

    [Tooltip("오른쪽 단 — SCREEN + GOOD TO KNOW. 🔴 한 단으로 두면 넘친다(실측 필요 1004 / 자리 708).")]
    [SerializeField] private TextMeshProUGUI bodyRightText;

    [SerializeField] private Button closeButton;

    /// <summary>
    /// 왼쪽 단 — 조작. 🔴 <b>코드가 원본</b>이다 (B11 의 교훈).
    ///
    /// <para>순서는 <b>배우는 순서</b>다 — 움직이고, 고르고, 그 다음이 편의다.</para>
    ///
    /// <para>🔴 <b>가독성이 요구였다</b> (D85 · *"너무 글씨가 몰려있어"*).
    /// 키와 설명을 <b>같은 줄에 표로 붙이지 않는다</b> — 예전 배치는 공백으로 칸을 맞춰서
    /// 글자가 조금만 길어져도 붙어 보였다. 키를 한 줄, 설명을 들여쓰기로 아래에 둔다.</para>
    ///
    /// <para>🔴 <b>한 단으로는 안 들어간다</b> — 실측 필요 높이 <b>1004</b>, 자리 <b>708</b> 이었다.
    /// 글자를 줄이면 요구와 반대가 되므로 <b>카드의 남는 가로를 쓴다.</b></para>
    /// </summary>
    private const string BodyLeft =
        "<size=118%><color=#F0C040>CONTROLS</color></size>\n" +
        "\n" +
        "WASD  ·  Arrow keys\n" +
        "<indent=8%>Move.</indent>\n" +
        "<indent=8%>Weapons fire automatically.</indent>\n" +
        "\n" +
        "1   2   3\n" +
        "<indent=8%>Pick a level-up card.</indent>\n" +
        "\n" +
        "Z\n" +
        "<indent=8%>Place a building.</indent>\n" +
        "\n" +
        "ESC\n" +
        "<indent=8%>Pause. The game fully stops.</indent>\n" +
        "<indent=8%>Shows your stats and items.</indent>";

    /// <summary>오른쪽 단 — 화면 설정과 알아 둘 것.</summary>
    private const string BodyRight =
        "<size=118%><color=#F0C040>SCREEN</color></size>\n" +
        "\n" +
        "F7   ·   F8\n" +
        "<indent=8%>Zoom in  ·  Zoom out.</indent>\n" +
        "<indent=8%>Remembered between runs.</indent>\n" +
        "\n" +
        "\n" +
        "<size=118%><color=#F0C040>GOOD TO KNOW</color></size>\n" +
        "\n" +
        "Each class has its own slot limits.\n" +
        "\n" +
        "The rows under your health bar\n" +
        "show what you carry and how\n" +
        "many slots are left.\n" +
        "\n" +
        "A shop lets you buy, remove,\n" +
        "or reroll. When there is nothing\n" +
        "left to buy it offers rest and\n" +
        "gold exchange instead.";

    private void Start()
    {
        Apply();
        if (closeButton != null) closeButton.onClick.AddListener(() => gameObject.SetActive(false));
    }

    // 패널을 다시 열 때도 코드 값이 이기게 한다.
    private void OnEnable() => Apply();

    private void Apply()
    {
        Style(bodyText,      BodyLeft);
        Style(bodyRightText, BodyRight);
    }

    /// <summary>
    /// 🔴 <b>글자 배치도 코드가 정한다</b> (D85). 씬 값이면 다음에 캔버스를 만질 때 조용히 돌아간다 (B11).
    ///
    /// <para>🔴 <b>자동 축소를 끈다.</b> 켜져 있으면 본문이 길어질수록 글자가 스스로 작아져
    /// *"몰려 보인다"* 가 다시 생긴다 — 넘치면 <b>글자가 아니라 배치를 고쳐야</b> 한다.</para>
    /// </summary>
    private static void Style(TextMeshProUGUI t, string body)
    {
        if (t == null) return;
        t.text             = body;
        t.alignment        = TextAlignmentOptions.TopLeft;
        t.enableAutoSizing = false;
        // 🔴 <b>단이 520 으로 넓어졌으므로 글자를 키운다</b> (D86 · 사용자: *"글자 크기 늘리고 좌우로 박스 더 늘려서"*).
        //    폭이 340 일 때 27 로 두면 줄이 자주 접혀 오히려 읽기 나빠진다 — 폭을 먼저 넓히고 키웠다.
        t.fontSize         = 27f;
        t.lineSpacing      = 12f;
        t.paragraphSpacing = 6f;
        t.overflowMode     = TextOverflowModes.Overflow;
        t.richText         = true;
    }
}
