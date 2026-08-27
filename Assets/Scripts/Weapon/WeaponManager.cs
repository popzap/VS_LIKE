using System.Collections.Generic;
using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    public static WeaponManager Instance { get; private set; }

    [SerializeField] private ObjectPool weaponPool;
    [SerializeField] private int        maxWeaponSlots = 6;

    private PlayerStats _stats;
    private readonly Dictionary<WeaponData, WeaponBase> _weapons = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _stats = GetComponentInParent<PlayerStats>();
    }

    public void AddOrUpgradeWeapon(WeaponData data, int level)
    {
        if (_weapons.TryGetValue(data, out var existing))
        {
            existing.SetLevel(level);
            return;
        }

        if (_weapons.Count >= maxWeaponSlots)
        {
            Debug.LogWarning("[WeaponManager] 무기 슬롯 꽉 참!");
            return;
        }

        var go = weaponPool.Get(data.WeaponPrefab, transform.position, Quaternion.identity);
        go.transform.SetParent(transform);
        var weapon = go.GetComponent<WeaponBase>();
        weapon.Initialize(data, level, _stats, weaponPool);
        _weapons[data] = weapon;
    }

    public void RemoveWeapon(WeaponData data)
    {
        if (!_weapons.TryGetValue(data, out var w)) return;
        weaponPool.Return(w.gameObject);
        _weapons.Remove(data);
    }

    public List<WeaponData> GetEquippedWeapons() => new(_weapons.Keys);
}
