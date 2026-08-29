using UnityEngine;

/// <summary>
/// 진화 레시피 한 줄. <c>Assets/Game/Balance/Evolutions.csv</c> 가 원본이다.
///
/// <para><b>왜 <see cref="ItemData"/> 목록인가</b> — <see cref="ItemData"/> 하나가
/// 무기/건물/패시브 참조를 전부 들고 있는 단일 타입이라, 재료를 카테고리별 필드로
/// 나눌 필요가 없다. 덕분에 <b>패시브+무기 · 무기+무기 · 건물+무기</b> 세 조합이
/// 전부 같은 한 줄로 표현된다.</para>
///
/// <para><b>전달 경로는 재료가 정한다</b> — 별도 플래그를 두지 않는다.
/// 재료에 건물이 섞여 있으면 그 건물이 <b>제단</b>이 되어 필드에서 상호작용해야 하고
/// (<see cref="IsFinalEvolution"/>), 아니면 보물상자에서 나온다.
/// 건물은 이미 필드에 서 있으므로 제단용 오브젝트를 따로 만들 필요가 없다.</para>
/// </summary>
[CreateAssetMenu(fileName = "EvolutionData", menuName = "Game/EvolutionData")]
public class EvolutionData : ScriptableObject
{
    [Header("표시")]
    public string EvolutionName;
    [TextArea] public string Description;

    [Header("재료")]
    [Tooltip("필요한 아이템들. 카테고리는 섞어도 된다.")]
    public ItemData[] Ingredients;

    [Tooltip("각 재료의 최소 레벨. Ingredients 와 같은 순서·같은 길이여야 한다.")]
    public int[] RequiredLevels;

    [Header("결과")]
    [Tooltip("지급할 진화 아이템. 레벨업 후보 풀(SceneWiring 의 allItems)에는 넣지 말 것 — 그냥 뽑히면 진화가 의미를 잃는다.")]
    public ItemData ResultItem;

    /// <summary>
    /// 재료 중 첫 번째 건물의 <see cref="BuildingData"/>. 없으면 null.
    /// 이 건물이 곧 진화 제단이다.
    /// </summary>
    public BuildingData AltarBuilding
    {
        get
        {
            if (Ingredients == null) return null;
            foreach (var item in Ingredients)
            {
                if (item == null) continue;
                if (item.Category != ItemCategory.Building) continue;
                if (item.BuildingRef != null) return item.BuildingRef;
            }
            return null;
        }
    }

    /// <summary>건물이 재료면 최종 진화 — 보물상자가 아니라 제단 앞에서 완성한다.</summary>
    public bool IsFinalEvolution => AltarBuilding != null;

    /// <summary>i 번째 재료의 요구 레벨. 배열이 짧으면 1 로 본다.</summary>
    public int GetRequiredLevel(int index)
        => RequiredLevels != null && index < RequiredLevels.Length
           ? Mathf.Max(1, RequiredLevels[index])
           : 1;
}
