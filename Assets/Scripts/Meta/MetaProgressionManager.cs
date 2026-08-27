using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// ────────────────────────────────────────────────────────────────────────────
//  SaveData  —  JSON 직렬화 대상
// ────────────────────────────────────────────────────────────────────────────
[Serializable]
public class SaveData
{
    public int    Currency             = 0;
    public int    TotalRuns            = 0;
    public int    TotalKills           = 0;

    // 영구 스탯 강화 레벨 (키: 스탯명)
    public SerializableDictionary<string, int> UpgradeLevels = new();

    // 해금된 캐릭터 ID 목록
    public List<string> UnlockedCharacters = new();

    // 해금된 스킨 목록
    public List<string> UnlockedSkins = new();
}

// Unity에서 Dictionary를 직렬화하기 위한 래퍼
[Serializable]
public class SerializableDictionary<TKey, TValue>
{
    public List<TKey>   Keys   = new();
    public List<TValue> Values = new();

    public bool TryGetValue(TKey key, out TValue value)
    {
        int idx = Keys.IndexOf(key);
        if (idx < 0) { value = default; return false; }
        value = Values[idx]; return true;
    }

    public void Set(TKey key, TValue value)
    {
        int idx = Keys.IndexOf(key);
        if (idx >= 0) Values[idx] = value;
        else { Keys.Add(key); Values.Add(value); }
    }
}

// ────────────────────────────────────────────────────────────────────────────
//  UpgradeDefinition  —  영구 업그레이드 항목 정의
// ────────────────────────────────────────────────────────────────────────────
[CreateAssetMenu(fileName = "UpgradeDef", menuName = "Game/UpgradeDefinition")]
public class UpgradeDefinition : ScriptableObject
{
    public string UpgradeId;
    public string DisplayName;
    [TextArea] public string Description;
    public Sprite Icon;
    public int    MaxLevel     = 5;
    public int[]  Costs;          // 레벨별 재화 비용

    [Header("효과 (StatBlock에 더해짐)")]
    public string StatKey;        // "MaxHp", "Damage", "MoveSpeed" 등
    public float[]Bonus;          // 레벨별 보너스

    public int  GetCost(int level) => Costs[Mathf.Clamp(level, 0, Costs.Length - 1)];
    public float GetBonus(int level) => Bonus[Mathf.Clamp(level - 1, 0, Bonus.Length - 1)];
}

// ────────────────────────────────────────────────────────────────────────────
//  MetaProgressionManager  —  저장/로드, 해금, 영구 업그레이드
// ────────────────────────────────────────────────────────────────────────────
public class MetaProgressionManager : MonoBehaviour
{
    [Header("업그레이드 목록")]
    [SerializeField] private UpgradeDefinition[] upgrades;

    [Header("캐릭터 해금 비용")]
    [SerializeField] private int characterUnlockCost = 30;
    [SerializeField] private int skinUnlockCost      = 15;

    private SaveData _data = new();
    private string   SavePath => Path.Combine(Application.persistentDataPath, "save.json");

    public int Currency   => _data.Currency;
    public int TotalRuns  => _data.TotalRuns;
    public int TotalKills => _data.TotalKills;

    /// <summary>런이 끝날 때(사망/승리) 누적 통계를 갱신한다. Save() 는 호출자가 한다.</summary>
    public void RegisterRunResult(int kills)
    {
        _data.TotalRuns++;
        _data.TotalKills += kills;
    }

    // ── 재화 ────────────────────────────────────────────────────

    public void AddCurrency(int amount)
    {
        _data.Currency += amount;
        OnCurrencyChanged?.Invoke(_data.Currency);
    }

    public bool SpendCurrency(int amount)
    {
        if (_data.Currency < amount) return false;
        _data.Currency -= amount;
        OnCurrencyChanged?.Invoke(_data.Currency);
        return true;
    }

    public Action<int> OnCurrencyChanged;

    // ── 영구 업그레이드 ──────────────────────────────────────────

    public bool PurchaseUpgrade(UpgradeDefinition def)
    {
        _data.UpgradeLevels.TryGetValue(def.UpgradeId, out int currentLevel);
        if (currentLevel >= def.MaxLevel) return false;

        int cost = def.GetCost(currentLevel);
        if (!SpendCurrency(cost)) return false;

        _data.UpgradeLevels.Set(def.UpgradeId, currentLevel + 1);
        Save();
        return true;
    }

    public int GetUpgradeLevel(string id)
    {
        _data.UpgradeLevels.TryGetValue(id, out int lv);
        return lv;
    }

    /// <summary>영구 업그레이드를 StatBlock으로 합산해서 반환.</summary>
    public StatBlock GetStatBonus()
    {
        // 보너스는 0 에서 시작해야 한다. new StatBlock() 은 기본 스탯값(MaxHp 100 …)을 갖고 있어서
        // PlayerStats 의 base + meta 합산이 전 스탯 2배가 된다.
        var bonus = StatBlock.Zero();
        foreach (var def in upgrades)
        {
            int lv = GetUpgradeLevel(def.UpgradeId);
            if (lv == 0) continue;
            float val = def.GetBonus(lv);
            ApplyStatKey(bonus, def.StatKey, val);
        }
        return bonus;
    }

    private static void ApplyStatKey(StatBlock s, string key, float val)
    {
        switch (key)
        {
            case "MaxHp":          s.MaxHp          += val; break;
            case "MoveSpeed":      s.MoveSpeed       += val; break;
            case "Damage":         s.Damage          += val; break;
            case "AttackSpeed":    s.AttackSpeed     += val; break;
            case "ProjectileSize": s.ProjectileSize  += val; break;
            case "PickupRadius":   s.PickupRadius    += val; break;
            case "CritChance":     s.CritChance      += val; break;
            case "CritMultiplier": s.CritMultiplier  += val; break;
            case "Armor":          s.Armor           += val; break;
            case "XpGain":         s.XpGain          += val; break;
            case "GoldGain":       s.GoldGain        += val; break;
        }
    }

    // ── 캐릭터 / 스킨 해금 ──────────────────────────────────────

    public bool UnlockCharacter(string characterId)
    {
        if (_data.UnlockedCharacters.Contains(characterId)) return false;
        if (!SpendCurrency(characterUnlockCost)) return false;
        _data.UnlockedCharacters.Add(characterId);
        Save();
        return true;
    }

    public bool UnlockSkin(string skinId)
    {
        if (_data.UnlockedSkins.Contains(skinId)) return false;
        if (!SpendCurrency(skinUnlockCost)) return false;
        _data.UnlockedSkins.Add(skinId);
        Save();
        return true;
    }

    public bool IsCharacterUnlocked(string id) => _data.UnlockedCharacters.Contains(id);
    public bool IsSkinUnlocked(string id)      => _data.UnlockedSkins.Contains(id);

    // ── 저장 / 로드 ──────────────────────────────────────────────

    public void Save()
    {
        try
        {
            string json = JsonUtility.ToJson(_data, prettyPrint: true);
            File.WriteAllText(SavePath, json);
            Debug.Log($"[Meta] Saved to {SavePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Meta] Save failed: {e.Message}");
        }
    }

    public void Load()
    {
        try
        {
            if (File.Exists(SavePath))
            {
                string json = File.ReadAllText(SavePath);
                _data = JsonUtility.FromJson<SaveData>(json);
                Debug.Log("[Meta] Loaded save data.");
            }
            else
            {
                _data = new SaveData();
                _data.UnlockedCharacters.Add("default"); // 기본 캐릭터 해금
                Debug.Log("[Meta] No save found. Fresh start.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[Meta] Load failed: {e.Message}");
            _data = new SaveData();
        }
    }
}
