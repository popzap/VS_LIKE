using UnityEngine;

/// <summary>
/// 볼륨 설정을 실제 오디오에 반영한다.
/// 값 저장은 <see cref="OptionPanel"/> 이 PlayerPrefs 로 처리하고, 여기서는 적용만 담당한다.
///
/// AudioMixer 를 도입하기 전까지 SFX 볼륨은 <see cref="AudioListener.volume"/>(전역 마스터)로
/// 대체하므로 BGM 최종 음량 = bgmSource.volume × SfxVolume 이 된다.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("BGM")]
    [SerializeField] private AudioSource bgmSource;

    public float BgmVolume { get; private set; } = 0.8f;
    public float SfxVolume { get; private set; } = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        SetBGMVolume(PlayerPrefs.GetFloat("bgm_vol", 0.8f));
        SetSFXVolume(PlayerPrefs.GetFloat("sfx_vol", 1.0f));
    }

    public void SetBGMVolume(float v)
    {
        BgmVolume = Mathf.Clamp01(v);
        if (bgmSource != null) bgmSource.volume = BgmVolume;
    }

    public void SetSFXVolume(float v)
    {
        SfxVolume = Mathf.Clamp01(v);
        AudioListener.volume = SfxVolume;
    }
}
