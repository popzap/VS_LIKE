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
