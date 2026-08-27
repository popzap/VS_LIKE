using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 전체의 소리를 담당한다. BGM 한 줄기 + 효과음 보이스 풀.
///
/// <para>볼륨 값의 저장은 <see cref="OptionPanel"/> 이 PlayerPrefs 로 처리하고,
/// 여기서는 "적용"만 담당한다.</para>
///
/// <para>⚠️ 예전에는 SFX 볼륨을 <c>AudioListener.volume</c>(전역 마스터)로 대신했다.
/// 그래서 SFX 슬라이더를 0 으로 내리면 <b>BGM 까지 같이 꺼졌다.</b>
/// 지금은 두 계통이 완전히 분리되어 있다 —
/// BGM 은 <c>bgmSource.volume</c>, SFX 는 보이스별 volume 에 곱해진다.
/// <c>AudioListener.volume</c> 은 1 로 고정하고 아무도 건드리지 않는다.</para>
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("BGM")]
    [SerializeField] private AudioSource bgmSource;

    [Header("SFX")]
    [Tooltip("동시에 울릴 수 있는 효과음 개수. 다 차면 새 소리는 버린다.")]
    [SerializeField] private int sfxVoices = 16;

    public float BgmVolume { get; private set; } = 0.8f;
    public float SfxVolume { get; private set; } = 1f;

    // ── SFX 보이스 풀 ────────────────────────────────────────────
    // 뱀서라이크는 초당 수십 발이 터진다. PlayClipAtPoint 는 매번 GameObject 를
    // 만들고 버리므로(GC) 쓰지 않는다. 고정 개수의 AudioSource 를 돌려 쓴다.
    private AudioSource[] _sfxPool;
    private float[]       _sfxBaseVolume;   // SfxVolume 을 곱하기 전의 값
    private int           _cursor;

    // 같은 소리가 같은 순간에 겹치면 음량만 커지고 위상이 뭉개져 "찢어지는" 소리가 난다.
    // 아주 짧은 창 안의 같은 클립은 한 번만 낸다.
    private const float DedupeWindow = 0.04f;
    private readonly Dictionary<AudioClip, float> _lastPlayedAt = new();

    private Coroutine _bgmFade;

    // ────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        BuildSfxPool();

        // 마스터는 항상 1. 볼륨 조절은 계통별로만 한다 (위 주석 참고).
        AudioListener.volume = 1f;

        SetBGMVolume(PlayerPrefs.GetFloat("bgm_vol", 0.8f));
        SetSFXVolume(PlayerPrefs.GetFloat("sfx_vol", 1.0f));
    }

    private void BuildSfxPool()
    {
        int n = Mathf.Max(1, sfxVoices);
        _sfxPool       = new AudioSource[n];
        _sfxBaseVolume = new float[n];

        for (int i = 0; i < n; i++)
        {
            var go = new GameObject($"SfxVoice{i}");
            go.transform.SetParent(transform, false);

            var src = go.AddComponent<AudioSource>();
            src.playOnAwake  = false;
            src.loop         = false;
            src.spatialBlend = 0f;   // 2D. 탑다운이라 좌우 패닝이 오히려 방해된다.
            _sfxPool[i]      = src;
        }
    }

    // ── 볼륨 ─────────────────────────────────────────────────────

    public void SetBGMVolume(float v)
    {
        BgmVolume = Mathf.Clamp01(v);
        if (bgmSource != null) bgmSource.volume = BgmVolume;
    }

    public void SetSFXVolume(float v)
    {
        SfxVolume = Mathf.Clamp01(v);

        // 이미 울리고 있는 소리에도 즉시 반영한다 — 슬라이더를 끌 때 바로 들려야
        // 사용자가 지금 무엇을 조절하는지 안다.
        if (_sfxPool == null) return;
        for (int i = 0; i < _sfxPool.Length; i++)
            _sfxPool[i].volume = _sfxBaseVolume[i] * SfxVolume;
    }

    // ── 효과음 ───────────────────────────────────────────────────

    /// <summary>효과음을 한 번 재생한다. 클립이 없으면 조용히 무시한다.</summary>
    /// <param name="volume">클립 고유 음량(0~1). SfxVolume 이 여기에 곱해진다.</param>
    /// <param name="pitchJitter">±피치 흔들기. 같은 소리가 반복될 때 기계음처럼 들리는 걸 막는다.</param>
    public void PlaySfx(AudioClip clip, float volume = 1f, float pitchJitter = 0.06f)
    {
        if (clip == null || SfxVolume <= 0f) return;

        // A-4 중복 컷
        float now = Time.unscaledTime;
        if (_lastPlayedAt.TryGetValue(clip, out float last) && now - last < DedupeWindow) return;
        _lastPlayedAt[clip] = now;

        var src = TakeFreeVoice(out int idx);
        if (src == null) return;   // 보이스가 다 찼다 — 버린다 (끊어 먹는 것보다 낫다)

        _sfxBaseVolume[idx] = Mathf.Clamp01(volume);
        src.clip   = clip;
        src.volume = _sfxBaseVolume[idx] * SfxVolume;
        src.pitch  = 1f + Random.Range(-pitchJitter, pitchJitter);
        src.Play();
    }

    private AudioSource TakeFreeVoice(out int index)
    {
        // 커서부터 한 바퀴 돌며 놀고 있는 보이스를 찾는다.
        for (int i = 0; i < _sfxPool.Length; i++)
        {
            int idx = (_cursor + i) % _sfxPool.Length;
            if (_sfxPool[idx].isPlaying) continue;

            _cursor = (idx + 1) % _sfxPool.Length;
            index   = idx;
            return _sfxPool[idx];
        }

        index = -1;
        return null;
    }

    // ── BGM ──────────────────────────────────────────────────────

    /// <summary>BGM 을 바꾼다. 같은 곡이 이미 돌고 있으면 아무것도 하지 않는다.</summary>
    /// <param name="fade">교체에 쓸 페이드 시간(초). 0 이면 즉시 전환.</param>
    public void PlayBgm(AudioClip clip, float fade = 1f)
    {
        if (bgmSource == null || clip == null) return;
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        if (_bgmFade != null) StopCoroutine(_bgmFade);

        if (fade <= 0f)
        {
            bgmSource.clip   = clip;
            bgmSource.volume = BgmVolume;
            bgmSource.Play();
            return;
        }

        _bgmFade = StartCoroutine(SwapBgmRoutine(clip, fade));
    }

    /// <summary>BGM 을 멈춘다.</summary>
    public void StopBgm(float fade = 0.5f)
    {
        if (bgmSource == null) return;
        if (_bgmFade != null) StopCoroutine(_bgmFade);
        _bgmFade = StartCoroutine(SwapBgmRoutine(null, fade));
    }

    // 소스가 하나뿐이라 진짜 크로스페이드가 아니라 "내렸다 올리기"다.
    // ⚠️ 반드시 unscaledDeltaTime — 일시정지·레벨업·히트스톱이 timeScale 을 0 으로 만들면
    //    scaled 시간으로는 페이드가 그 자리에서 얼어붙는다.
    private IEnumerator SwapBgmRoutine(AudioClip next, float fade)
    {
        float half = Mathf.Max(0.01f, fade * 0.5f);

        if (bgmSource.isPlaying)
        {
            float from = bgmSource.volume;
            for (float t = 0f; t < half; t += Time.unscaledDeltaTime)
            {
                bgmSource.volume = Mathf.Lerp(from, 0f, t / half);
                yield return null;
            }
        }

        bgmSource.Stop();
        bgmSource.clip = next;

        if (next == null) { bgmSource.volume = BgmVolume; _bgmFade = null; yield break; }

        bgmSource.volume = 0f;
        bgmSource.Play();

        for (float t = 0f; t < half; t += Time.unscaledDeltaTime)
        {
            bgmSource.volume = Mathf.Lerp(0f, BgmVolume, t / half);
            yield return null;
        }

        bgmSource.volume = BgmVolume;
        _bgmFade = null;
    }
}
