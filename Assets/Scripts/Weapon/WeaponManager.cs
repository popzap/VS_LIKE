using System.Collections.Generic;
using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    public static WeaponManager Instance { get; private set; }

    [SerializeField] private ObjectPool weaponPool;

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

        // 상한은 직업 사슬이 정한다 (CharacterClassData.BonusWeaponSlots 의 합).
        // 여기까지 온 것 자체가 이미 이상하다 — LevelUpManager.CanAcquire 가 꽉 찬 카테고리의
        // 신규 아이템을 후보에서 빼기 때문이다. 그래도 남겨 두는 이유는, 예전에 이 자리에서
        // 조용히 return 하는 동안 인벤토리에는 아이템이 기록되어 "보유 중인데 무기는 없는"
        // 상태가 만들어졌기 때문이다. 다시 그런 경로가 생기면 로그로 드러나야 한다.
        int limit = _stats != null ? _stats.SlotLimit(ItemCategory.Weapon) : int.MaxValue;
        if (_weapons.Count >= limit)
        {
            Debug.LogWarning($"[WeaponManager] 무기 슬롯 꽉 참 ({_weapons.Count}/{limit}) — {data.WeaponName}");
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
