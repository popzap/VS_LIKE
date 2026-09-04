using System.Collections.Generic;
using UnityEngine;

// ────────────────────────────────────────────────────────────────────────────
//  DisplaySettings  —  해상도 · 전체화면 · 그래픽 품질 (저장은 PlayerPrefs)
// ────────────────────────────────────────────────────────────────────────────
/// <summary>
/// 옵션 화면에 볼륨 슬라이더 2개밖에 없었다(`ROADMAP` §3-3).
///
/// <para>🔑 <b>값을 고르는 곳(<see cref="OptionPanel"/>)과 적용하는 곳을 갈랐다.</b>
/// 옵션 패널은 <b>열려야</b> <c>Start()</c> 가 도는데, 저장된 설정은
/// <b>게임이 켜지자마자</b> 적용돼야 하기 때문이다. 그래서 적용은 이 정적 클래스가 맡고
/// <see cref="GameManager"/> 가 <c>Awake</c> 에서 한 번 부른다 —
/// <see cref="AudioManager"/> 가 볼륨을 다루는 방식과 같다.</para>
///
/// <para>⚠️ <b>에디터에서는 해상도·전체화면이 눈에 안 보인다.</b> <c>Screen.SetResolution</c> 은
/// 게임 뷰를 바꾸지 않는다 — 빌드에서만 실제로 먹는다. 그래서 이 클래스의 검증은
/// "화면이 바뀌었나"가 아니라 <b>"저장·조회가 맞나"</b> 로 한다. 품질은 에디터에서도 바뀐다.</para>
/// </summary>
public static class DisplaySettings
{
    // 🔴 컴파일 반영 확인용 (D27).
    public const int Version = 1;

    private const string KeyWidth      = "screen_w";
    private const string KeyHeight     = "screen_h";
    private const string KeyFullscreen = "screen_full";
    private const string KeyQuality    = "gfx_quality";

    private static List<Vector2Int> _resolutions;

    /// <summary>
    /// 고를 수 있는 해상도. <c>Screen.resolutions</c> 는 <b>같은 크기를 주사율만 달리해 여러 번</b>
    /// 돌려주므로 크기 기준으로 중복을 없앤다 (20개 → 실제로는 훨씬 적다).
    /// </summary>
    public static IReadOnlyList<Vector2Int> Resolutions
    {
        get
        {
            if (_resolutions != null) return _resolutions;

            _resolutions = new List<Vector2Int>();
            var seen = new HashSet<long>();
            foreach (var r in Screen.resolutions)
            {
                long key = ((long)r.width << 32) | (uint)r.height;
                if (!seen.Add(key)) continue;
                _resolutions.Add(new Vector2Int(r.width, r.height));
            }

            // 목록이 비는 환경(일부 에디터/헤드리스)이 있다. 그때도 화살표가 죽지 않게 지금 값을 넣는다.
            if (_resolutions.Count == 0)
                _resolutions.Add(new Vector2Int(Screen.width, Screen.height));

            _resolutions.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            return _resolutions;
        }
    }

    public static bool Fullscreen => PlayerPrefs.GetInt(KeyFullscreen, Screen.fullScreen ? 1 : 0) == 1;

    public static int QualityLevel =>
        Mathf.Clamp(PlayerPrefs.GetInt(KeyQuality, QualitySettings.GetQualityLevel()),
                    0, Mathf.Max(0, QualitySettings.names.Length - 1));

    /// <summary>저장된 해상도의 목록상 인덱스. 저장값이 없거나 목록에 없으면 지금 화면과 가장 가까운 것.</summary>
    public static int ResolutionIndex
    {
        get
        {
            int w = PlayerPrefs.GetInt(KeyWidth,  Screen.width);
            int h = PlayerPrefs.GetInt(KeyHeight, Screen.height);

            var list = Resolutions;
            for (int i = 0; i < list.Count; i++)
                if (list[i].x == w && list[i].y == h) return i;

            // 못 찾으면 가장 가까운 것으로 — 인덱스 0 으로 떨어뜨리면
            // "옵션을 열었을 뿐인데 최저 해상도가 선택돼 보인다".
            int best = 0;
            long bestDiff = long.MaxValue;
            for (int i = 0; i < list.Count; i++)
            {
                long d = System.Math.Abs((long)list[i].x - w) + System.Math.Abs((long)list[i].y - h);
                if (d < bestDiff) { bestDiff = d; best = i; }
            }
            return best;
        }
    }

    /// <summary>게임 시작 시 한 번. <see cref="GameManager"/> 의 <c>Awake</c> 가 부른다.</summary>
    public static void ApplySaved()
    {
        QualitySettings.SetQualityLevel(QualityLevel, true);
        ApplyScreen(ResolutionIndex, Fullscreen);
    }

    public static void SetResolution(int index)
    {
        var list = Resolutions;
        index = Mathf.Clamp(index, 0, list.Count - 1);
        PlayerPrefs.SetInt(KeyWidth,  list[index].x);
        PlayerPrefs.SetInt(KeyHeight, list[index].y);
        PlayerPrefs.Save();
        ApplyScreen(index, Fullscreen);
    }

    public static void SetFullscreen(bool on)
    {
        PlayerPrefs.SetInt(KeyFullscreen, on ? 1 : 0);
        PlayerPrefs.Save();
        ApplyScreen(ResolutionIndex, on);
    }

    public static void SetQuality(int level)
    {
        level = Mathf.Clamp(level, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
        PlayerPrefs.SetInt(KeyQuality, level);
        PlayerPrefs.Save();
        // 두 번째 인자 true = 지금 프레임에 즉시 반영. false 면 다음 프레임까지 밀린다.
        QualitySettings.SetQualityLevel(level, true);
    }

    private static void ApplyScreen(int index, bool fullscreen)
    {
        var list = Resolutions;
        index = Mathf.Clamp(index, 0, list.Count - 1);
        // ⚠️ 에디터에서는 아무 일도 안 일어난다 — 빌드 전용이다.
        Screen.SetResolution(list[index].x, list[index].y, fullscreen);
    }

    public static string ResolutionLabel(int index)
    {
        var list = Resolutions;
        index = Mathf.Clamp(index, 0, list.Count - 1);
        return $"{list[index].x} x {list[index].y}";
    }

    public static string QualityLabel(int level)
    {
        var names = QualitySettings.names;
        if (names == null || names.Length == 0) return "-";
        return names[Mathf.Clamp(level, 0, names.Length - 1)];
    }
}
