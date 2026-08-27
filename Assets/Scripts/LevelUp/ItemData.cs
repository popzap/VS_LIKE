using UnityEngine;

public enum ItemCategory { Weapon, Building, Passive }

[CreateAssetMenu(fileName = "ItemData", menuName = "Game/ItemData")]
public class ItemData : ScriptableObject
{
    [Header("공통")]
    public string        ItemName;
    [TextArea] public string Description;
    public Sprite        Icon;
    public ItemCategory  Category;
    public int           MaxLevel = 5;

    [Header("무기 전용")]
    public WeaponData    WeaponRef;

    [Header("건물 전용")]
    public BuildingData  BuildingRef;

    [Header("패시브 전용")]
    public PassiveData   PassiveRef;

    [Header("상점")]
    public int ShopPrice = 5;            // 상점 구매 기본 비용

    // 런타임에서 현재 레벨을 추적
    [System.NonSerialized] public int CurrentLevel = 0;

    public bool IsMaxLevel => CurrentLevel >= MaxLevel;

    public string GetLeveledDescription()
        => $"{Description}\n<size=80%>Lv {CurrentLevel + 1} / {MaxLevel}</size>";
}
