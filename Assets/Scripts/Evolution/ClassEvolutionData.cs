using UnityEngine;

/// <summary>
/// 직업 진화 레시피 한 줄. <c>Assets/Game/Balance/ClassEvolutions.csv</c> 가 원본이다.
///
/// <para><b>무기 진화(<see cref="EvolutionData"/>)와 무엇이 다른가</b> —
/// 결과가 아이템이 아니라 <b>직업</b>이고, 재료를 <b>소모하지 않는다.</b>
/// 무기 진화는 재료 무기를 먹고 더 센 무기를 돌려주니 교환이 성립하지만,
/// 직업 진화는 돌려주는 게 직업이라 무기까지 가져가면 순수한 손해가 된다.</para>
///
/// <para><b>제단은 필수다.</b> 무기 진화는 재료에 건물이 없으면 보물상자로 새지만,
/// 직업 진화는 "그 건물 앞에서 승급한다"가 규칙이라 <see cref="AltarBuilding"/> 이
/// 반드시 있어야 한다. 임포터가 없으면 경고한다.</para>
/// </summary>
[CreateAssetMenu(fileName = "ClassEvolutionData", menuName = "Game/ClassEvolutionData")]
public class ClassEvolutionData : ScriptableObject
{
    [Header("표시")]
    public string EvolutionName;
    [TextArea] public string Description;

    [Header("조건")]
    [Tooltip("이 직업일 때만 승급할 수 있다. 비어 있으면 아무 직업에서나 — T1→T2 는 보통 비워 둔다.")]
    public CharacterClassData FromClass;

    [Tooltip("필요한 아이템들. 건물이 하나는 있어야 제단이 정해진다.")]
    public ItemData[] Ingredients;

    [Tooltip("각 재료의 최소 레벨. Ingredients 와 같은 순서·같은 길이여야 한다.")]
    public int[] RequiredLevels;

    [Header("결과")]
    [Tooltip("사슬 끝에 덧붙일 상위 직업. SceneWiring 의 GameManager.classes 에는 넣지 말 것 — 선택 화면에 뜬다.")]
    public CharacterClassData ResultClass;

    /// <summary>재료 중 첫 번째 건물. 이 건물이 승급 제단이다. 없으면 null(= 잘못된 레시피).</summary>
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

    /// <summary>i 번째 재료의 요구 레벨. 배열이 짧으면 1 로 본다.</summary>
    public int GetRequiredLevel(int index)
        => RequiredLevels != null && index < RequiredLevels.Length
           ? Mathf.Max(1, RequiredLevels[index])
           : 1;
}
