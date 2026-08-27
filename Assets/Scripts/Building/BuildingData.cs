using UnityEngine;

[CreateAssetMenu(fileName = "BuildingData", menuName = "Game/BuildingData")]
public class BuildingData : ScriptableObject
{
    [Header("기본")]
    public string     BuildingName;
    public GameObject Prefab;
    public Sprite     Icon;

    [Header("레벨별 스탯")]
    public float[]    Damage        = { 8, 12, 18, 25, 35 };
    public float[]    AttackRange   = { 4f, 4.5f, 5f, 5.5f, 6f };
    public float[]    AttackCooldown= { 2f, 1.7f, 1.4f, 1.1f, 0.8f };
    public int[]      MaxCount      = { 1, 1, 2, 2, 3 }; // 동시 배치 가능 수

    [Header("비전투 산출량 (쿨다운 1회당)")]
    [Tooltip("Village = 경험치, Farm = 골드, Restaurant = 회복량. 공격 건물은 쓰지 않는다.")]
    public float[]    Output        = { 0, 0, 0, 0, 0 };

    public float GetDamage(int lv)   => Damage        [Mathf.Clamp(lv-1, 0, Damage.Length-1)];
    public float GetRange(int lv)    => AttackRange   [Mathf.Clamp(lv-1, 0, AttackRange.Length-1)];
    public float GetCooldown(int lv) => AttackCooldown[Mathf.Clamp(lv-1, 0, AttackCooldown.Length-1)];
    public int   GetMaxCount(int lv) => MaxCount      [Mathf.Clamp(lv-1, 0, MaxCount.Length-1)];
    public float GetOutput(int lv)   => Output.Length == 0 ? 0f
                                      : Output        [Mathf.Clamp(lv-1, 0, Output.Length-1)];
}
