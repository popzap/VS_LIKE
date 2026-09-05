using UnityEngine;
using TMPro;

/// <summary>
/// 화면 하단에 진화 안내 한 줄을 띄운다.
///
/// <para>진화는 <b>조건이 조용히 충족된다</b>. 무기를 Lv5 로 올린 순간 아무 표시도 없으면
/// 플레이어는 자기가 뭘 할 수 있게 됐는지 영영 모른다. 특히 제단 진화는
/// "건물 앞에서 E" 라는 조작이 게임 안 어디에도 안 적혀 있다.</para>
///
/// <para>우선순위: 지금 누를 수 있는 것(제단) → 상자에서 나올 것 → 제단을 찾아가야 할 것.
/// 지금 당장 할 수 있는 행동을 맨 위에 둔다.</para>
/// </summary>
public class EvolutionPromptUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI promptText;

    [Tooltip("판정 주기(초). 매 프레임 돌 필요가 없다 — 레시피 수 × 재료 수만큼 딕셔너리를 뒤진다.")]
    [SerializeField] private float checkInterval = 0.2f;

    private float _timer;

    private void Start()
    {
        // 🔴 [Z] Build 줄(y=26) 바로 위에 얹는다. 둘이 겹치면 둘 다 못 읽는다.
        // 🔑 <b>이제 좌하단 구석을 혼자 쓴다</b> (D82). `D81` 이 건물 안내와 4px 겹쳐서
        //    66 -> 78 로 올렸는데, `D82` 가 건물 안내를 <b>가로 가운데 · 바닥 1/3</b> 로 옮기면서
        //    그 이유가 사라졌다. 구석에 붙는 게 맞으므로 26 으로 내린다.
        PromptCorner.Place(promptText, 26f);
        if (promptText != null) promptText.gameObject.SetActive(false);
    }

    private void Update()
    {
        _timer -= Time.unscaledDeltaTime;
        if (_timer > 0f) return;
        _timer = checkInterval;

        Refresh();
    }

    private void Refresh()
    {
        if (promptText == null) return;

        var text = BuildPrompt();
        if (string.IsNullOrEmpty(text))
        {
            if (promptText.gameObject.activeSelf) promptText.gameObject.SetActive(false);
            return;
        }

        promptText.text = text;
        if (!promptText.gameObject.activeSelf) promptText.gameObject.SetActive(true);
    }

    private string BuildPrompt()
    {
        var gm = GameManager.Instance;
        if (gm == null) return null;

        // 전투 중에만 의미가 있다. HUD 자체가 Wave/LevelUp/Paused 에서만 켜지므로
        // (StateVisibilityBinder) 여기서는 그중 Wave 만 남긴다 — 레벨업 카드를 고르는
        // 동안이나 일시정지 화면 위에 안내가 겹쳐 뜰 이유가 없다.
        if (gm.CurrentState != GameState.Wave) return null;

        var em = EvolutionManager.Instance;
        if (em == null) return null;

        var player = PlayerStats.Current;
        if (player == null) return null;

        // ① 지금 누를 수 있다. 승급을 먼저 본다 — TryEvolveAtAltar 가 그 순서로 처리하므로
        //    안내와 실제 결과가 어긋나지 않아야 한다.
        var classAtAltar = em.FindAltarClassEvolution(player.transform.position);
        if (classAtAltar != null)
            return $"<color=#F0C040>[E]</color>  PROMOTE  —  {classAtAltar.ResultClass.ClassName}";

        var atAltar = em.FindAltarEvolution(player.transform.position);
        if (atAltar != null)
            return $"<color=#F0C040>[E]</color>  EVOLVE  —  {atAltar.ResultItem.ItemName}";

        var ready = em.GetReadyEvolutions();

        // ② 상자에서 나온다 — 가만히 있어도 언젠가 들어오므로 "찾아가야 하는" 것보다 뒤로 밀 이유가 없다
        foreach (var evo in ready)
            if (!evo.IsFinalEvolution)
                return $"<color=#8A8F98>EVOLUTION READY</color>  {evo.ResultItem.ItemName}  —  open a treasure chest";

        // ③ 제단을 찾아가야 한다
        var readyClass = em.GetReadyClassEvolutions();
        if (readyClass.Count > 0)
        {
            var promo = readyClass[0];
            var site  = promo.AltarBuilding;
            return $"<color=#8A8F98>PROMOTION READY</color>  {promo.ResultClass.ClassName}  —  " +
                   $"stand by your {(site != null ? site.BuildingName : "building")} and press E";
        }

        if (ready.Count == 0) return null;

        var final = ready[0];
        var altar = final.AltarBuilding;
        return $"<color=#8A8F98>EVOLUTION READY</color>  {final.ResultItem.ItemName}  —  " +
               $"stand by your {(altar != null ? altar.BuildingName : "building")} and press E";
    }
}

/// <summary>
/// 안내 한 줄을 <b>화면 왼쪽 아래 구석</b>으로 옮긴다 (D65 · 사용자 요구 C-3).
///
/// <para>사용자 판정은 *"묻힌다"* 였고, 요구는 *"더 강조"* 가 아니라
/// <b>*"플레이 화면을 가리니 외곽으로 빼라"*</b> 였다 —
/// 두 안내가 <b>하단 중앙</b>, 즉 플레이어 바로 아래 세로줄에 있었다.
/// 난전에서 적이 제일 두꺼운 자리가 하필 거기다.</para>
///
/// <para>🔑 <b>씬이 아니라 코드가 자리를 정한다.</b> 씬 값이 정본이면 다음에
/// 누가 캔버스를 만질 때 조용히 돌아온다 (<c>B11</c> 에서 실제로 그랬다).</para>
/// </summary>
internal static class PromptCorner
{
    public const float Margin = 26f;
    public const float Width  = 720f;

    /// <summary>
    /// 가로 가운데 · 세로는 <b>바닥에서 1/3</b> 높이에 놓는다 (D82 · 사용자 요구).
    ///
    /// <para>요구는 *"중앙 아래로 — 맨 아래 말고 중앙에서 아래 2:1 느낌으로"* 였다.
    /// 위:아래 = 2:1 로 나누는 자리가 곧 <b>바닥에서 1/3</b> 이다.</para>
    ///
    /// <para>🔴 <b>앵커 자체를 1/3 에 둔다.</b> 바닥 앵커에 <c>y = 높이/3</c> 을 주면
    /// 화면 비율이 바뀔 때 따라가지 못한다 — <c>D81</c> 에서 겹침을 만든 것이 정확히 그 방식이었다.</para>
    /// </summary>
    public static void PlaceCenterLower(TMPro.TextMeshProUGUI text)
    {
        if (text == null) return;
        var rt = text.rectTransform;
        rt.anchorMin        = new Vector2(0.5f, 1f / 3f);
        rt.anchorMax        = new Vector2(0.5f, 1f / 3f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(Width, rt.sizeDelta.y);
        rt.anchoredPosition = Vector2.zero;
        text.alignment      = TMPro.TextAlignmentOptions.Center;
    }

    public static void Place(TMPro.TextMeshProUGUI text, float y)
    {
        if (text == null) return;
        var rt = text.rectTransform;
        rt.anchorMin        = new Vector2(0f, 0f);
        rt.anchorMax        = new Vector2(0f, 0f);
        rt.pivot            = new Vector2(0f, 0f);
        rt.sizeDelta        = new Vector2(Width, rt.sizeDelta.y);
        rt.anchoredPosition = new Vector2(Margin, y);
        text.alignment      = TMPro.TextAlignmentOptions.BottomLeft;
    }
}
