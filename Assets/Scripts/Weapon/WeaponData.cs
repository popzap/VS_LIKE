using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "Game/WeaponData")]
public class WeaponData : ScriptableObject
{
    [Header("기본")]
    public string     WeaponName;
    public GameObject WeaponPrefab;   // WeaponBase 컴포넌트 포함
    public Sprite     Icon;

    [Header("스탯 (레벨별)")]
    public float[]    Damage        = { 10, 15, 22, 30, 40 };
    public float[]    Cooldown      = { 1.5f, 1.3f, 1.1f, 0.9f, 0.7f };
    public float[]    ProjectileSize= { 1f, 1.1f, 1.2f, 1.3f, 1.5f };
    public int[]      ProjectileCount= { 1, 1, 2, 2, 3 };
    public float[]    Range         = { 10f, 10f, 12f, 12f, 15f };
    public float      ProjectileSpeed = 8f;
    public GameObject ProjectilePrefab;

    public float GetDamage(int level)         => Damage        [Mathf.Clamp(level - 1, 0, Damage.Length - 1)];
    public float GetCooldown(int level)       => Cooldown      [Mathf.Clamp(level - 1, 0, Cooldown.Length - 1)];
    public float GetProjectileSize(int level) => ProjectileSize[Mathf.Clamp(level - 1, 0, ProjectileSize.Length - 1)];
    public int   GetProjectileCount(int level)=> ProjectileCount[Mathf.Clamp(level - 1, 0, ProjectileCount.Length - 1)];
    public float GetRange(int level)          => Range         [Mathf.Clamp(level - 1, 0, Range.Length - 1)];
}
