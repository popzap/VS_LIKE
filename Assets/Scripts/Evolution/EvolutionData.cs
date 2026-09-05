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

    [Tooltip("진화를 끝냈을 때 런 내내 남는 특전 (D68). None 이면 결과 무기의 수치만 바뀐다.")]
    public EvolutionPerk Perk = EvolutionPerk.None;

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

/// <summary>
/// 진화를 끝냈을 때 <b>런 내내</b> 남는 특전 (D68 · 사용자 요구 12).
///
/// <para>사용자 요구: *"진화 능력 너가 적당히 분배해 (투사체 두배,
/// 필드 드랍아이템 지속시간 두배, 이속증가 추가, +@)"*.</para>
///
/// <para>🔑 <b>결과 무기의 수치와 따로 둔다.</b> 수치는 <c>Weapons.csv</c> 가 이미 올려 주고 있고,
/// 그건 *"세졌다"* 로만 읽힌다. 특전은 <b>빌드가 달라지는</b> 쪽이라
/// 어느 진화를 골랐는지가 판 전체에 남는다.</para>
///
/// <para>🔴 <c>enum</c> 은 정수로 직렬화된다 — <b>뒤에만 붙일 것</b> (<c>D51</c> 에서 같은 이유로
/// <c>EnemyAI</c> 를 뒤에만 늘렸다). 중간에 끼우면 기존 <c>.asset</c> 의 값이 조용히 밀린다.</para>
/// </summary>
public enum EvolutionPerk
{
    /// <summary>특전 없음.</summary>
    None = 0,

    /// <summary>모든 무기의 투사체(근접은 연타) 수가 <b>2배</b>가 된다.</summary>
    DoubleProjectiles = 1,

    /// <summary>필드 드랍 버프(무적·공속·이속)의 <b>지속시간이 2배</b>가 된다.</summary>
    DoubleBuffDuration = 2,

    /// <summary>골드 획득이 <b>2배</b>가 된다.</summary>
    DoubleGold = 3,
}
