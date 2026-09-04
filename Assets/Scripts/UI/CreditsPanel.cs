using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 크레딧 — 메인 메뉴에서 연다. (`ROADMAP` §3-3)
///
/// <para>🔑 <b>본문을 코드가 들고 있다.</b> 씬에 글자를 박아 두면 <c>B11</c> 처럼
/// <b>씬이 코드를 덮어</b> 나중에 고쳐도 화면이 안 바뀐다. 여기서는 반대로
/// <c>Start</c> 마다 코드 값을 씌워서 <b>코드가 항상 이긴다.</b></para>
///
/// <para>🔴 문자열은 영문이다 — 폰트가 Static 115자라 한글 글리프가 없다 (I-60).</para>
/// </summary>
public class CreditsPanel : MonoBehaviour
{
    // 🔴 컴파일 반영 확인용 (D27).
    public const int Version = 1;

    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private Button          closeButton;

    /// <summary>
    /// 🔴 씬에 남은 옛 문자열을 덮기 위해 <b>코드가 원본</b>이다 (B11 의 교훈).
    /// 고칠 때는 여기만 고치면 된다.
    /// </summary>
    private const string Body =
        "VS_LIKE\n" +
        "A 2D survivors-like built in Unity 6.\n" +
        "\n" +
        "DESIGN & CODE\n" +
        "popzap\n" +
        "\n" +
        "ART\n" +
        "Procedural tools and generated pixel art,\n" +
        "assembled in-project.\n" +
        "\n" +
        "AUDIO\n" +
        "Generated sound effects and music.\n" +
        "\n" +
        "FONT\n" +
        "Pretendard\n" +
        "\n" +
        "BUILT WITH\n" +
        "Unity 6.3 LTS - Universal Render Pipeline\n" +
        "TextMeshPro - Input System\n" +
        "\n" +
        "Thanks for playing.";

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
