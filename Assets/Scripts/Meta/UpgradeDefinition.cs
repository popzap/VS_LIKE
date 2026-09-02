using UnityEngine;

// ────────────────────────────────────────────────────────────────────────────
//  UpgradeDefinition  —  영구 업그레이드 항목 정의
// ────────────────────────────────────────────────────────────────────────────
/// <summary>
/// 메타 화면에서 골드로 사는 영구 강화 한 줄. 런을 넘어 유지된다.
///
/// <para>🔴 <b>이 클래스는 반드시 자기 파일에 단독으로 있어야 한다</b> (I-19).
/// 예전에는 <see cref="MetaProgressionManager"/> 와 같은 파일에 있었는데, 그 상태로
/// <c>.asset</c> 을 만들면 <c>m_Script</c> 가 <c>0</c> 으로 기록되고
/// <b>재임포트로도 복구되지 않는다.</b> 애셋이 0개였던 덕에 아무도 안 다쳤을 뿐이다
/// (`REQ/DEV.md` 요청-20 §3-가 에서 CONTENT 가 짚었다).</para>
///
/// <para>레벨은 <c>SaveData.UpgradeLevels</c> 에 <see cref="UpgradeId"/> 를 키로 저장된다.
/// <b>애셋 이름이 아니라 이 문자열이 키다</b> — 한 번 배포한 뒤에는 바꾸면 저장이 끊긴다.</para>
/// </summary>
[CreateAssetMenu(fileName = "UpgradeDef", menuName = "Game/UpgradeDefinition")]
public class UpgradeDefinition : ScriptableObject
{
    [Tooltip("저장 키. 🔴 애셋 이름이 아니라 이 값이 SaveData 의 키다 — 바꾸면 기존 저장이 끊긴다")]
    public string UpgradeId;

    [Tooltip("메타 화면 카드에 뜨는 이름. UI 문자열이므로 영문으로 쓴다")]
    public string DisplayName;

    [TextArea] public string Description;
    public Sprite Icon;
    public int    MaxLevel = 5;

    [Tooltip("레벨별 재화 비용. 0번 원소가 Lv0→1 비용이다")]
    public int[]  Costs;

    [Header("효과 (StatBlock 에 더해짐)")]
    [Tooltip("MetaProgressionManager.ApplyStatKey 가 아는 문자열이어야 한다. 오타는 조용히 무시된다")]
    public string StatKey;        // "MaxHp", "Damage", "MoveSpeed" 등

    [Tooltip("레벨별 보너스. 0번 원소가 Lv1 의 값이다 (비용과 기준이 하나 어긋나 있다)")]
    public float[] Bonus;

    public int   GetCost(int level)  => Costs[Mathf.Clamp(level, 0, Costs.Length - 1)];
    public float GetBonus(int level) => Bonus[Mathf.Clamp(level - 1, 0, Bonus.Length - 1)];
}
