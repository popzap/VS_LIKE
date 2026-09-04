using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 옵션 — 볼륨 2종 + 화면 3종 (해상도 · 전체화면 · 그래픽 품질).
///
/// <para>🔑 <b>드롭다운을 안 쓰고 <c>&lt; 값 &gt;</c> 화살표로 만들었다.</b>
/// <c>TMP_Dropdown</c> 은 템플릿·뷰포트·아이템까지 부품이 여럿이라 코드로 짓기 나쁘고,
/// 화살표 방식은 기존 9-slice 버튼을 그대로 쓰며 나중에 게임패드로 옮기기도 쉽다.</para>
///
/// <para>🔴 <b>값을 저장·적용하는 것은 <see cref="DisplaySettings"/> 다.</b>
/// 이 패널은 <b>열려야</b> <c>Start()</c> 가 도는데 저장된 설정은 게임이 켜지자마자
/// 적용돼야 하기 때문이다.</para>
///
/// <para>🔴 직렬화 필드에 <c>?.</c> 를 쓰지 않는다 (I-24). 미할당 필드는 "가짜 null" 이라
/// <c>?.</c> 가 통과시키고 예외가 <c>Start</c> 밖으로 새면 <b>옵션 화면이 통째로 죽는다.</b></para>
/// </summary>
public class OptionPanel : MonoBehaviour
{
    // 🔴 컴파일 반영 확인용 (D27).
    public const int Version = 2;   // 2 = 화면 설정 3종 추가 (D47)

    [Header("볼륨")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("화면 — 해상도")]
    [SerializeField] private TextMeshProUGUI resolutionValueText;
    [SerializeField] private Button          resolutionPrevButton;
    [SerializeField] private Button          resolutionNextButton;

    [Header("화면 — 전체화면")]
    [SerializeField] private TextMeshProUGUI fullscreenValueText;
    [SerializeField] private Button          fullscreenButton;

    [Header("화면 — 그래픽 품질")]
    [SerializeField] private TextMeshProUGUI qualityValueText;
    [SerializeField] private Button          qualityPrevButton;
    [SerializeField] private Button          qualityNextButton;

    [Header("닫기")]
    [SerializeField] private Button closeButton;

    private const string BGM_KEY = "bgm_vol";
    private const string SFX_KEY = "sfx_vol";

    private int _resIndex;
    private int _quality;

    private void Start()
    {
        // ── 볼륨 ──
        if (bgmSlider != null)
        {
            bgmSlider.value = PlayerPrefs.GetFloat(BGM_KEY, 0.8f);
            bgmSlider.onValueChanged.AddListener(v =>
            {
                PlayerPrefs.SetFloat(BGM_KEY, v);
                AudioManager.Instance?.SetBGMVolume(v);
            });
        }
        if (sfxSlider != null)
        {
            sfxSlider.value = PlayerPrefs.GetFloat(SFX_KEY, 1.0f);
            sfxSlider.onValueChanged.AddListener(v =>
            {
                PlayerPrefs.SetFloat(SFX_KEY, v);
                AudioManager.Instance?.SetSFXVolume(v);
            });
        }

        // ── 화면 ──
        _resIndex = DisplaySettings.ResolutionIndex;
        _quality  = DisplaySettings.QualityLevel;

        if (resolutionPrevButton != null) resolutionPrevButton.onClick.AddListener(() => StepResolution(-1));
        if (resolutionNextButton != null) resolutionNextButton.onClick.AddListener(() => StepResolution(+1));
        if (fullscreenButton     != null) fullscreenButton    .onClick.AddListener(ToggleFullscreen);
        if (qualityPrevButton    != null) qualityPrevButton   .onClick.AddListener(() => StepQuality(-1));
        if (qualityNextButton    != null) qualityNextButton   .onClick.AddListener(() => StepQuality(+1));

        if (closeButton != null) closeButton.onClick.AddListener(() => gameObject.SetActive(false));

        RefreshLabels();
    }

    /// <summary>패널을 다시 열 때마다 최신값을 보여 준다 — 다른 곳에서 바꿨을 수도 있다.</summary>
    private void OnEnable()
    {
        _resIndex = DisplaySettings.ResolutionIndex;
        _quality  = DisplaySettings.QualityLevel;
        RefreshLabels();
    }

    // ─────────────────────────────────────────────────────────────

    private void StepResolution(int delta)
    {
        int count = DisplaySettings.Resolutions.Count;
        if (count <= 0) return;
        // 순환시킨다 — 끝에서 버튼이 죽어 있으면 "고장난 것"처럼 보인다.
        _resIndex = ((_resIndex + delta) % count + count) % count;
        DisplaySettings.SetResolution(_resIndex);
        RefreshLabels();
    }

    private void StepQuality(int delta)
    {
        int count = QualitySettings.names.Length;
        if (count <= 0) return;
        _quality = ((_quality + delta) % count + count) % count;
        DisplaySettings.SetQuality(_quality);
        RefreshLabels();
    }

    private void ToggleFullscreen()
    {
        DisplaySettings.SetFullscreen(!DisplaySettings.Fullscreen);
        RefreshLabels();
    }

    private void RefreshLabels()
    {
        if (resolutionValueText != null) resolutionValueText.text = DisplaySettings.ResolutionLabel(_resIndex);
        if (qualityValueText    != null) qualityValueText   .text = DisplaySettings.QualityLabel(_quality);
        if (fullscreenValueText != null) fullscreenValueText.text = DisplaySettings.Fullscreen ? "On" : "Off";
    }
}
