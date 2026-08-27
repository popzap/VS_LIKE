using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Game/EnemyData")]
public class EnemyData : ScriptableObject
{
    [Header("기본")]
    public string     EnemyName;
    public GameObject Prefab;
    public Sprite     Sprite;

    [Header("외형 (프리팹을 공유하고 색/크기로만 구분)")]
    public Color Tint      = Color.white;
    public float SizeScale = 1f;

    [Header("스탯")]
    public float MaxHp        = 30f;
    public float MoveSpeed    = 2f;
    public float ContactDamage= 10f;
    public float Armor        = 0f;

    [Header("경험치 / 보상")]
    public int   XpDrop       = 3;
    public int   CurrencyDrop = 0; // 상점용 재화

    [Header("엘리트 배율")]
    public float EliteHpMult      = 2.5f;
    public float EliteDamageMult  = 1.5f;
    public float EliteSpeedMult   = 1.2f;
    public int   EliteXpMult      = 3;

    [Header("보스 배율")]
    public float BossHpMult       = 10f;
    public float BossDamageMult   = 2.5f;
    public float BossSpeedMult    = 0.8f;
    public int   BossXpMult       = 10;
}
