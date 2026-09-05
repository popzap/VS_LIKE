#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// <b>개발용 치트 패널.</b> 백틱(<c>`</c> — 숫자 1 왼쪽) 키로 열고 닫는다.
/// 아이템/무기/패시브/건물 레벨을 손으로 올리고 내려서 <b>진화·승급을 즉시 시험</b>하기 위한 것이다.
/// 정상 플레이로 Lv5 무기 + Lv3 건물을 모으려면 오래 걸려서 검증이 사실상 불가능했다.
///
/// <para>🔴 <b>파일 전체가 <c>#if UNITY_EDITOR || DEVELOPMENT_BUILD</c> 안에 있다.</b>
/// 릴리즈 빌드에는 한 줄도 들어가지 않는다.</para>
///
/// <para><b>왜 캔버스 UI 가 아니라 IMGUI(<c>OnGUI</c>) 인가</b> — 셋 다 실익이다.
/// ① 아이템이 30개가 넘어 스크롤뷰 + 행 프리팹을 씬에 만들어야 하는데, 그 배선이 본 게임보다 커진다.
/// ② <b>폰트 문제를 통째로 피한다</b> — TMP 는 Static 115자라 문자표에 없는 글자가 빈칸이 된다(I-60 · B4).
///    IMGUI 는 유니티 내장 폰트를 써서 그 제약을 받지 않는다.
/// ③ 캔버스 정렬 순서 싸움(I-50)에 끼어들지 않는다. 못생긴 건 개발 도구라 감수한다.</para>
/// </summary>
public class DevPanel : MonoBehaviour
{
    /// <summary>
    /// 패널을 여는 키 — 백틱(<c>`</c>), 숫자 1 왼쪽.
    ///
    /// <para>게임 조작(WASD · Z · E · ESC)과 겹치지 않아야 하고, <b>에디터 단축키와도 겹치면 안 된다.</b>
    /// <c>F1</c> 은 유니티가 "Unity 매뉴얼 열기"로 물고 있어서 누르면 브라우저가 뜬다.</para>
    /// </summary>
    private const Key ToggleKey = Key.Backquote;

    /// <summary>Gold 버튼 한 번에 더할 금액.</summary>
    private const int GoldStep = 100;

    /// <summary>
    /// <b>씬에 넣지 않는다.</b> 게임이 시작되면 스스로 붙는다.
    ///
    /// <para>씬에 오브젝트로 두면 이 클래스가 <c>#if</c> 로 잘려 나가는 릴리즈 빌드에서
    /// <b>"Missing script" 로 남는다.</b> 런타임 생성이면 씬 파일에 흔적이 0 이고,
    /// 배선할 것도 없어 병렬 세션에서 씬 충돌을 만들지 않는다.</para>
    ///
    /// <para><c>DontDestroyOnLoad</c> 인 이유 — 이 콜백은 <b>게임 시작 때 한 번만</b> 돈다.
    /// Retry 는 씬을 다시 로드하므로(I-17) 그냥 두면 재시작 후 패널이 사라진다.</para>
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        var go = new GameObject("[DevPanel]");
        go.AddComponent<DevPanel>();
        DontDestroyOnLoad(go);
    }

    private bool    _open;
    private Vector2 _scroll;

    // 패널을 열 때의 timeScale 을 기억했다가 닫을 때 되돌린다.
    // 무조건 1 로 되돌리면 ESC 일시정지 위에서 열었다 닫았을 때
    // 일시정지 화면 뒤에서 게임만 다시 흐른다.
    private float _savedTimeScale = 1f;

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || !kb[ToggleKey].wasPressedThisFrame) return;

        _open = !_open;
        if (_open) { _savedTimeScale = Time.timeScale; Time.timeScale = 0f; }
        else       { Time.timeScale = _savedTimeScale; }
    }

    private void OnDisable()
    {
        // 패널을 연 채로 씬이 바뀌면 timeScale 이 0 인 채 남는다 = 게임이 멈춘 것처럼 보인다.
        if (!_open) return;
        _open = false;
        Time.timeScale = _savedTimeScale;
    }

    // ── 그리기 ───────────────────────────────────────────────────

    private const float PanelWidth  = 560f;
    private const float PanelMargin = 12f;

    private void OnGUI()
    {
        if (!_open) return;

        float h = Screen.height - PanelMargin * 2f;
        GUILayout.BeginArea(new Rect(PanelMargin, PanelMargin, PanelWidth, h), GUI.skin.box);

        // 여는 키를 하드코딩하지 않는다 — ToggleKey 를 바꾸면 안내도 같이 바뀐다.
        GUILayout.Label($"<b>DEV PANEL</b>   [{ToggleKey}] to close   (game is frozen)", RichLabel);

        var gm = GameManager.Instance;
        if (gm == null)
        {
            GUILayout.Label("GameManager not ready.");
            GUILayout.EndArea();
            return;
        }

        DrawPlayerRow(gm);
        GUILayout.Space(6f);

        _scroll = GUILayout.BeginScrollView(_scroll);
        DrawSpawn(gm);
        DrawItems(gm);
        DrawEvolutions(gm);
        GUILayout.EndScrollView();

        GUILayout.EndArea();
    }

    private static GUIStyle _richLabel;
    private static GUIStyle RichLabel
    {
        get
        {
            if (_richLabel == null) _richLabel = new GUIStyle(GUI.skin.label) { richText = true };
            return _richLabel;
        }
    }

    // ── 적 소환 (부하 시험 · 촬영용) ─────────────────────────────
    //
    // 웨이브를 거치지 않고 직접 세운다. WaveManager 는 MaxAlive(60~130)로 상한을 걸고
    // 시간창에 맞춰 나눠 소환하므로 "적 800마리"라는 조건 자체를 만들 수 없다.
    //
    // 🔴 소환된 적은 WaveManager 의 _alive 목록에 안 들어간다. 웨이브가 도는 중에
    //    쓰면 웨이브가 자기 적을 그 위에 얹는다. 부하 시험은 웨이브 밖에서 하는 게 깨끗하다.

    private EnemyData[] _spawnTypes;
    private ObjectPool  _spawnPool;
    private readonly List<GameObject> _devSpawned = new();
    private float _spawnRadius = 12f;

    private void DrawSpawn(GameManager gm)
    {
        GUILayout.Space(4f);
        GUILayout.Label("<b>Enemy Spawn</b>  (load test / capture)", RichLabel);

        EnsureSpawnRefs();

        if (_spawnTypes == null || _spawnTypes.Length == 0)
        {
            GUILayout.Label("EnemyData not found.");
            return;
        }

        // 살아 있는 수는 씬 전체에서 센다 — 웨이브가 소환한 것도 화면에 보이므로
        // "지금 몇 마리가 돌고 있나"는 그쪽이 맞는 답이다.
        int alive = 0;
        foreach (var e in FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
            if (e.gameObject.activeInHierarchy) alive++;

        GUILayout.BeginHorizontal();
        GUILayout.Label($"alive <b>{alive}</b>", RichLabel, GUILayout.Width(90f));
        foreach (int n in new[] { 100, 200, 400, 800 })
            if (GUILayout.Button($"+{n}", GUILayout.Width(52f))) SpawnEnemies(n);
        if (GUILayout.Button("Clear", GUILayout.Width(60f))) ClearSpawned();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label($"radius {_spawnRadius:0}", GUILayout.Width(90f));
        _spawnRadius = GUILayout.HorizontalSlider(_spawnRadius, 4f, 25f, GUILayout.Width(160f));
        if (GUILayout.Button("Invincible 60s", GUILayout.Width(120f)))
            PlayerStats.Current?.GrantInvincibility(60f);
        GUILayout.EndHorizontal();

        GUILayout.Space(6f);
    }

    private void EnsureSpawnRefs()
    {
        if (_spawnPool == null) _spawnPool = FindFirstObjectByType<ObjectPool>();
        if (_spawnTypes != null && _spawnTypes.Length > 0) return;

#if UNITY_EDITOR
        // 에디터에서만 애셋을 훑는다. 개발 빌드에는 AssetDatabase 가 없다.
        var guids = UnityEditor.AssetDatabase.FindAssets("t:EnemyData");
        var list  = new List<EnemyData>();
        foreach (var g in guids)
        {
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
            var d    = UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            if (d != null && d.Prefab != null) list.Add(d);
        }
        _spawnTypes = list.ToArray();
#endif
    }

    private void SpawnEnemies(int count)
    {
        if (_spawnPool == null || _spawnTypes == null || _spawnTypes.Length == 0) return;

        var player = GameObject.FindGameObjectWithTag("Player");
        Vector2 c  = player != null ? (Vector2)player.transform.position : Vector2.zero;

        for (int i = 0; i < count; i++)
        {
            var data = _spawnTypes[i % _spawnTypes.Length];
            Vector2 p = c + Random.insideUnitCircle * _spawnRadius;

            var go = _spawnPool.Get(data.Prefab, p, Quaternion.identity);
            go.GetComponent<EnemyBase>().Initialize(data);
            _devSpawned.Add(go);
        }
    }

    private void ClearSpawned()
    {
        // 내가 세운 것만이 아니라 씬 전체를 치운다 — 웨이브가 얹은 것까지 같이 지워야
        // "지금 화면에 N마리" 라는 조건을 깨끗하게 만들 수 있다.
        foreach (var e in FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
            if (e.gameObject.activeInHierarchy) e.ForceDespawn();
        _devSpawned.Clear();
    }

    // ── 플레이어 한 줄 (레벨 / 골드) ─────────────────────────────

    private void DrawPlayerRow(GameManager gm)
    {
        var exp    = gm.ExpManager;
        var meta   = gm.MetaProgression;
        var player = PlayerStats.Current;

        string cls = "-";
        if (player != null && player.ClassChain.Count > 0)
            cls = player.ClassChain[player.ClassChain.Count - 1].ClassName;

        // 지갑이 둘이라 둘 다 보여 준다 — Run 은 상점이 쓰는 돈, Meta 는 영구 강화용 (TODO §2-B).
        GUILayout.Label($"State {gm.CurrentState}   Class {cls}"
                      + (exp  != null ? $"   Lv {exp.CurrentLevel} ({exp.CurrentXp}/{exp.XpToNext})" : "")
                      + $"   Run {gm.RunGold} G"
                      + (meta != null ? $"   Meta {meta.Currency} G (+{gm.PendingMetaGold})" : ""));

        GUILayout.BeginHorizontal();

        // XpToNext 만큼 넣으면 정확히 한 번 레벨업한다 → 레벨업 카드가 뜬다.
        if (exp != null && GUILayout.Button("Level +1"))
            exp.CollectXp(exp.XpToNext);

        if (GUILayout.Button($"Run Gold +{GoldStep}"))
            gm.AddRunGold(GoldStep);

        if (meta != null && GUILayout.Button($"Meta Gold +{GoldStep}"))
            meta.AddCurrency(GoldStep);

        if (player != null && GUILayout.Button("Full Heal"))
            player.Heal(player.Final.MaxHp);

        GUILayout.EndHorizontal();

        DrawStageRow(gm);
    }

    /// <summary>
    /// 스테이지 진행 줄 (D63) — <b>지금 노드를 즉시 클리어</b>한다.
    ///
    /// <para>사용자 요구: *"9라운드까지 빠르게"*. 병목은 난이도가 아니라
    /// <b>웨이브 타이머(60~100초)</b>라서, 그걸 건너뛰는 버튼 하나면 된다.</para>
    ///
    /// <para>🔴 <b>웨이브일 때만 누를 수 있다.</b> 상점·이벤트 노드에는 각자의 닫기 경로가
    /// 따로 있어서 여기서 흉내 내면 그쪽 상태가 어긋난다 — 버튼을 막고 지금 상태를 보여 준다.</para>
    /// </summary>
    private void DrawStageRow(GameManager gm)
    {
        var wm = gm.WaveManager;
        if (wm == null) return;

        GUILayout.BeginHorizontal();

        // 층은 LayerScaling 이 정본이다 (StartWave 가 채운다).
        GUILayout.Label($"Layer <b>{LayerScaling.Layer}</b>   HP x{LayerScaling.HpMult:0.00}"
                      + $"   Spawn x{LayerScaling.SpawnMult:0.00}", RichLabel, GUILayout.Width(240f));

        bool inWave = gm.CurrentState == GameState.Wave && wm.IsWaveActive;

        // 🔴 GUI.enabled 를 반드시 되돌린다. 안 그러면 이 아래 모든 줄이 회색으로 죽는다.
        bool prev = GUI.enabled;
        GUI.enabled = inWave;
        if (GUILayout.Button("Clear Node", GUILayout.Width(110f)))
            wm.DevForceClearWave();
        GUI.enabled = prev;

        GUILayout.Label(inWave
            ? $"  remaining {wm.WaveRemainingTime:0}s"
            : $"  (not in a wave - state {gm.CurrentState})");

        GUILayout.EndHorizontal();
    }

    // ── 아이템 목록 ──────────────────────────────────────────────

    private static readonly ItemCategory[] Categories =
        { ItemCategory.Weapon, ItemCategory.Passive, ItemCategory.Building };

    private void DrawItems(GameManager gm)
    {
        var lum = gm.LevelUpManager;
        if (lum == null) { GUILayout.Label("LevelUpManager not ready."); return; }

        var all = lum.AllItems;
        if (all == null) { GUILayout.Label("LevelUpManager.allItems is empty."); return; }

        var player = PlayerStats.Current;

        foreach (var cat in Categories)
        {
            string slots = player != null
                ? $"  ({lum.CountOwned(cat)}/{player.SlotLimit(cat)} slots)"
                : "";
            GUILayout.Space(6f);
            GUILayout.Label($"<b>{cat}</b>{slots}", RichLabel);

            foreach (var item in all)
            {
                if (item == null || item.Category != cat) continue;
                DrawItemRow(lum, item);
            }
        }
    }

    private void DrawItemRow(LevelUpManager lum, ItemData item)
    {
        int  level = lum.GetItemLevel(item);
        bool owned = level > 0;

        GUILayout.BeginHorizontal();

        string name = string.IsNullOrEmpty(item.ItemName) ? item.name : item.ItemName;
        GUILayout.Label($"{name}  <b>{level}</b>/{item.MaxLevel}", RichLabel, GUILayout.Width(240f));

        // 슬롯이 꽉 찬 카테고리의 신규 아이템은 게임과 똑같이 막는다.
        // 여기서만 뚫으면 인벤토리에는 있는데 무기는 없는 상태가 만들어진다
        // (LevelUpManager.ApplyItem 주석 참조) — 치트로 만든 유령 상태는 버그처럼 보인다.
        bool canAdd = !item.IsMaxLevel && lum.CanAcquire(item);

        GUI.enabled = level > 0;
        if (GUILayout.Button("-", GUILayout.Width(28f))) SetLevel(lum, item, level - 1);

        GUI.enabled = canAdd;
        if (GUILayout.Button("+", GUILayout.Width(28f))) lum.ApplyItemFromShop(item);
        if (GUILayout.Button("Max", GUILayout.Width(44f))) SetLevel(lum, item, item.MaxLevel);

        GUI.enabled = owned;
        if (GUILayout.Button("X", GUILayout.Width(28f))) lum.RemoveItemFull(item);

        GUI.enabled = true;

        if (!canAdd && !item.IsMaxLevel && !owned)
            GUILayout.Label("slot full");

        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// 레벨을 <paramref name="target"/> 으로 맞춘다.
    ///
    /// <para>내리는 API 가 없어서 <b>완전히 제거한 뒤 목표 횟수만큼 다시 올린다.</b>
    /// 게임에 "레벨을 내린다"는 개념 자체가 없으므로 그 경로를 새로 만드는 대신
    /// 이미 검증된 공개 API(<c>RemoveItemFull</c> + <c>ApplyItemFromShop</c>)만 조합한다 —
    /// 치트가 게임 로직을 우회하면 여기서 나온 상태를 신뢰할 수 없다.</para>
    /// </summary>
    private static void SetLevel(LevelUpManager lum, ItemData item, int target)
    {
        target = Mathf.Clamp(target, 0, item.MaxLevel);

        lum.RemoveItemFull(item);
        for (int i = 0; i < target; i++) lum.ApplyItemFromShop(item);
    }

    // ── 진화 / 승급 ──────────────────────────────────────────────

    private void DrawEvolutions(GameManager gm)
    {
        var em = gm.EvolutionMgr;
        GUILayout.Space(10f);
        GUILayout.Label("<b>Ready Evolutions</b>", RichLabel);

        if (em == null) { GUILayout.Label("EvolutionManager not ready."); return; }

        List<EvolutionData> weapons = em.GetReadyEvolutions();
        List<ClassEvolutionData> classes = em.GetReadyClassEvolutions();

        if (weapons.Count == 0 && classes.Count == 0)
        {
            // 무엇이 모자란지는 안 보여준다 — 레시피 조건은 EvolutionManager 가 판정한다.
            GUILayout.Label("none  (raise materials above)");
            return;
        }

        // ⚠️ 아래 두 버튼은 제단 근접 판정을 건너뛴다. 조건이 맞으면 그 자리에서 진화한다.
        //    "건물 앞에서 E 를 누르는 경로"는 이걸로 검증되지 않는다 (TODO.md §1).
        foreach (var evo in weapons)
        {
            if (evo == null) continue;
            string tag = evo.IsFinalEvolution ? " [altar]" : "";
            if (GUILayout.Button($"Evolve  ->  {Name(evo.EvolutionName, evo.name)}{tag}")) em.Evolve(evo);
        }

        foreach (var evo in classes)
        {
            if (evo == null) continue;
            if (GUILayout.Button($"Promote  ->  {Name(evo.EvolutionName, evo.name)} [altar]")) em.EvolveClass(evo);
        }
    }

    private static string Name(string display, string assetName)
        => string.IsNullOrEmpty(display) ? assetName : display;
}
#endif
