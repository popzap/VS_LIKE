using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임의 모든 오디오 클립을 한 곳에 모아 둔 표. 키(<see cref="SfxId"/>/<see cref="BgmId"/>)로 꺼낸다.
///
/// <para>왜 한 곳에 모으나 — 클립 참조를 각 <c>WeaponData</c>/<c>EnemyData</c> 에 흩뿌리면
/// "지금 어떤 소리가 비어 있나"를 알려면 애셋을 전부 열어 봐야 한다.
/// 여기 한 장만 보면 빈 칸이 바로 보이고, 나중에 종류별로 소리를 다르게 하고 싶어지면
/// 그때 개별 데이터 애셋에 <b>덮어쓰기용</b> 필드를 더하면 된다.</para>
///
/// <para>볼륨과 피치 흔들기를 클립마다 여기서 잡아 둔다. 호출부는 키만 넘기므로
/// 밸런싱(어떤 소리가 너무 크다 같은)은 코드를 건드리지 않고 이 애셋에서 끝난다.</para>
/// </summary>
[CreateAssetMenu(fileName = "AudioLibrary", menuName = "Game/Audio Library")]
public class AudioLibrary : ScriptableObject
{
    [Serializable]
    public class SfxEntry
    {
        public SfxId     Id;
        public AudioClip Clip;

        [Tooltip("이 클립 고유의 음량. 사용자 SFX 볼륨이 여기에 곱해진다.")]
        [Range(0f, 1f)] public float Volume = 1f;

        [Tooltip("±피치 흔들기. 초당 여러 번 나는 소리일수록 크게 준다.")]
        [Range(0f, 0.3f)] public float PitchJitter = 0.06f;
    }

    [Serializable]
    public class BgmEntry
    {
        public BgmId     Id;
        public AudioClip Clip;
    }

    [SerializeField] private List<SfxEntry> sfx = new();
    [SerializeField] private List<BgmEntry> bgm = new();

    // 리스트 선형 탐색은 초당 수십 번 울리는 효과음에는 낭비다. 첫 조회 때 한 번 만든다.
    // OnEnable 이 아니라 지연 생성인 이유 — 도메인 리로드 순서에 기대지 않기 위해서다.
    private Dictionary<SfxId, SfxEntry>  _sfxMap;
    private Dictionary<BgmId, AudioClip> _bgmMap;

    public SfxEntry GetSfx(SfxId id)
    {
        if (id == SfxId.None) return null;
        if (_sfxMap == null) BuildMaps();
        return _sfxMap.TryGetValue(id, out var e) ? e : null;
    }

    public AudioClip GetBgm(BgmId id)
    {
        if (id == BgmId.None) return null;
        if (_bgmMap == null) BuildMaps();
        return _bgmMap.TryGetValue(id, out var c) ? c : null;
    }

    private void BuildMaps()
    {
        _sfxMap = new Dictionary<SfxId, SfxEntry>();
        _bgmMap = new Dictionary<BgmId, AudioClip>();

        for (int i = 0; i < sfx.Count; i++)
        {
            var e = sfx[i];
            if (e == null || e.Id == SfxId.None) continue;
            _sfxMap[e.Id] = e;   // 중복 키는 뒤엣것이 이긴다
        }

        for (int i = 0; i < bgm.Count; i++)
        {
            var e = bgm[i];
            if (e == null || e.Id == BgmId.None) continue;
            _bgmMap[e.Id] = e.Clip;
        }
    }

    /// <summary>인스펙터에서 표를 고쳤을 때 캐시를 버린다. 안 그러면 플레이 중 수정이 안 먹는다.</summary>
    private void OnValidate() => _sfxMap = null;

    /// <summary>클립이 비어 있는 키 목록. 진행 상황을 콘솔에서 확인할 때 쓴다.</summary>
    public string DescribeMissing()
    {
        if (_sfxMap == null) BuildMaps();

        var missing = new List<string>();
        foreach (SfxId id in Enum.GetValues(typeof(SfxId)))
        {
            if (id == SfxId.None) continue;
            var e = GetSfx(id);
            if (e == null || e.Clip == null) missing.Add(id.ToString());
        }
        foreach (BgmId id in Enum.GetValues(typeof(BgmId)))
        {
            if (id == BgmId.None) continue;
            if (GetBgm(id) == null) missing.Add("BGM." + id);
        }

        return missing.Count == 0 ? "(none)" : string.Join(", ", missing);
    }
}
