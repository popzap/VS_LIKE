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

    /// <summary>
    /// 누적 전투 시간(초). 통계 화면이 쓴다 (D36).
    ///
    /// <para>ℹ️ 기존 세이브에는 이 필드가 없지만 <c>JsonUtility</c> 는 없는 필드를
    /// 기본값(0)으로 읽으므로 <b>예전 세이브가 깨지지 않는다.</b> 옛 판수만큼은 0 으로 시작한다.</para>
    /// </summary>
    public float  TotalPlaySeconds     = 0f;

    // 영구 스탯 강화 레벨 (키: 스탯명)
    public SerializableDictionary<string, int> UpgradeLevels = new();

    // 해금된 캐릭터 ID 목록
    public List<string> UnlockedCharacters = new();

    // 해금된 스킨 목록
    public List<string> UnlockedSkins = new();

    // ── 도감 (D54) ──────────────────────────────────────────
    //
    // 🔑 애셋 파일명(= CSV 의 Id)을 그대로 쓴다. ItemName 같은 표시 이름을 쓰면
    //    번역하거나 이름을 다듬는 순간 예전 세이브의 발견 기록이 통째로 날아간다.
    //
    // ℹ️ 옛 세이브에는 이 필드들이 없지만 JsonUtility 는 없는 필드를 건드리지 않으므로
    //    위 초기화(new())가 그대로 남는다 — TotalPlaySeconds 와 같은 사정이다.
    //    즉 예전 판을 하던 사람은 "아무것도 발견 안 한 상태"로 시작한다(깨지지 않는다).

    /// <summary>먹어 본 아이템. 무기·건물·패시브를 한 목록에 담는다(전부 ItemData 다).</summary>
    public List<string> DiscoveredItems   = new();

    /// <summary>골라 봤거나 승급해 본 직업.</summary>
    public List<string> DiscoveredClasses = new();

    /// <summary>화면에 나온 적. 죽였는지가 아니라 <b>봤는지</b>가 기준이다.</summary>
    public List<string> DiscoveredEnemies = new();
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

// 🔴 UpgradeDefinition 은 UpgradeDefinition.cs 로 옮겼다 (I-19, D30).
//    ScriptableObject 클래스가 다른 파일에 얹혀 있으면 새 .asset 의 m_Script 가 0 으로
//    기록되고 재임포트로도 복구되지 않는다. 여기로 되돌리지 말 것.

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

    /// <summary>
    /// 메타 화면이 그릴 강화 목록 (D30). 순서는 인스펙터 배열 순서 = <c>Upgrades.csv</c> 행 순서다.
    /// <para>🔴 <c>null</c> 이 아니라 <b>빈 배열</b>을 돌려준다 — 화면이 매번 검사하지 않게.</para>
    /// </summary>
    public UpgradeDefinition[] Upgrades => upgrades != null ? upgrades : System.Array.Empty<UpgradeDefinition>();

    public int Currency   => _data.Currency;
    public int   TotalRuns        => _data.TotalRuns;
    public int   TotalKills       => _data.TotalKills;
    public float TotalPlaySeconds => _data.TotalPlaySeconds;

    /// <summary>런이 끝날 때(사망/승리) 누적 통계를 갱신한다. Save() 는 호출자가 한다.</summary>
    public void RegisterRunResult(int kills, float seconds = 0f)
    {
        _data.TotalRuns++;
        _data.TotalKills += kills;
        _data.TotalPlaySeconds += Mathf.Max(0f, seconds);
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
        foreach (var def in Upgrades)
        {
            if (def == null) continue;
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

    // ── 도감 발견 기록 (D54) ────────────────────────────────
    //
    // 🔴 여기서 Save() 를 부르지 않는다. Discover 는 적이 스폰될 때마다 불리는데
    //    그때마다 파일을 쓰면 웨이브 중에 디스크 I/O 가 끼어든다.
    //    대신 더럽다고만 표시하고, 런이 끝날 때·도감을 열 때·게임을 끌 때 한 번에 쓴다.

    private bool _dirty;

    private List<string> ListOf(CodexKind kind) => kind switch
    {
        CodexKind.Class  => _data.DiscoveredClasses,
        CodexKind.Enemy  => _data.DiscoveredEnemies,
        _                => _data.DiscoveredItems,
    };

    /// <summary>
    /// 발견을 기록한다. <b>이미 있으면 아무 일도 안 한다.</b>
    /// </summary>
    /// <param name="id">애셋 파일명(= CSV 의 Id). 표시 이름을 넣지 말 것.</param>
    /// <returns>이번에 <b>처음</b> 발견했으면 true.</returns>
    public bool Discover(CodexKind kind, string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        var list = ListOf(kind);
        if (list.Contains(id)) return false;
        list.Add(id);
        _dirty = true;
        return true;
    }

    public bool IsDiscovered(CodexKind kind, string id)
        => !string.IsNullOrEmpty(id) && ListOf(kind).Contains(id);

    public int DiscoveredCount(CodexKind kind) => ListOf(kind).Count;

    /// <summary>쌓인 발견 기록을 파일에 내린다. 이미 깨끗하면 아무 일도 안 한다.</summary>
    public void FlushIfDirty()
    {
        if (_dirty) Save();
    }

    // 🔴 알트+F4 로 꺼도 그날 발견한 것이 남아야 한다.
    //    에디터에서는 플레이 종료 시에도 불린다.
    private void OnApplicationQuit() => FlushIfDirty();

    // ── 저장 / 로드 ──────────────────────────────────────────────

    public void Save()
    {
        try
        {
            string json = JsonUtility.ToJson(_data, prettyPrint: true);
            File.WriteAllText(SavePath, json);
            _dirty = false;   // D54 — 도감 발견 기록도 같이 내려갔다
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

/// <summary>
/// 도감이 나누는 갈래 (D54).
///
/// <para>🔴 <b>무기를 따로 두지 않는다.</b> 무기·건물·패시브는 전부 <see cref="ItemData"/> 라
/// 발견 경로가 하나(<c>LevelUpManager.ApplyItem</c>)다. 화면에서만 카테고리로 갈라 보여준다 —
/// 저장 쪽까지 갈라 두면 같은 사실이 두 목록에 나뉘어 들어간다.</para>
///
/// <para>진화는 여기 없다. <b>진화는 재료로부터 파생되는 상태</b>라
/// 따로 기록하면 재료 기록과 어긋날 수 있다 — 재료를 다 발견했으면 조건이 보인다.</para>
/// </summary>
public enum CodexKind { Item, Class, Enemy }
