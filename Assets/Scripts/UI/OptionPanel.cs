using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 볼륨, 해상도 등 간단 옵션 (확장 가능)
/// </summary>
public class OptionPanel : MonoBehaviour
{
    [Header("볼륨")]
    [SerializeField] private Slider  bgmSlider;
    [SerializeField] private Slider  sfxSlider;
    [SerializeField] private Button  closeButton;

    private const string BGM_KEY = "bgm_vol";
    private const string SFX_KEY = "sfx_vol";

    private void Start()
    {
        bgmSlider.value = PlayerPrefs.GetFloat(BGM_KEY, 0.8f);
        sfxSlider.value = PlayerPrefs.GetFloat(SFX_KEY, 1.0f);

        bgmSlider.onValueChanged.AddListener(v =>
        {
            PlayerPrefs.SetFloat(BGM_KEY, v);
            AudioManager.Instance?.SetBGMVolume(v);
        });
        sfxSlider.onValueChanged.AddListener(v =>
        {
            PlayerPrefs.SetFloat(SFX_KEY, v);
            AudioManager.Instance?.SetSFXVolume(v);
        });

        closeButton.onClick.AddListener(() => gameObject.SetActive(false));
    }
}
