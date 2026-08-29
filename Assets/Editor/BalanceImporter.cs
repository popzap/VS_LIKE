using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Balance;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 밸런스 CSV ↔ ScriptableObject 양방향 변환기.
///
/// <para><b>왜 CSV 인가</b> — 적/무기/패시브 수치는 사람이 표로 훑어보며 고치는 게 가장 빠르다.
/// SO 애셋은 인스펙터에서 한 번에 하나씩만 볼 수 있어 밸런싱에 부적합하다.
/// 그래서 <c>Assets/Game/Balance/*.csv</c> 를 <b>원본(authoring source)</b> 으로 삼고,
/// SO 는 거기서 생성되는 <b>런타임 산출물</b>로 취급한다.</para>
///
/// <para><b>사용법</b>
/// <list type="bullet">
/// <item>CSV 를 고쳤다 → <c>Game/Balance/Import CSV -&gt; ScriptableObjects</c></item>
/// <item>인스펙터에서 SO 를 고쳤다 → <c>Game/Balance/Export ScriptableObjects -&gt; CSV</c> 로 CSV 에 되돌린다</item>
/// </list></para>
///
/// <para>Id 열이 곧 애셋 파일명이다. Id 를 바꾸면 새 애셋이 생기고 옛 애셋은 남으니 직접 지워야 한다.</para>
/// </summary>
public static class BalanceImporter
{
    private const string CsvFolder      = "Assets/Game/Balance";
    private const string EnemyFolder    = "Assets/Game/EnemyData";
    private const string WeaponFolder   = "Assets/Game/WeaponData";
    private const string BuildingFolder = "Assets/Game/BuildingData";
    private const string PassiveFolder  = "Assets/Game/PassiveData";
    private const string ItemFolder     = "Assets/Game/ItemData";
    private const string WaveFolder     = "Assets/Game/WaveData";
    private const string ClassFolder    = "Assets/Game/ClassData";

    // ════════════════════════════════════════════════════════════════
    //  Import
    // ════════════════════════════════════════════════════════════════

    [MenuItem("Game/Balance/Import CSV -> ScriptableObjects", priority = 0)]
    public static void ImportAll()
    {
        var log = new StringBuilder();

        try
        {
            AssetDatabase.StartAssetEditing();

            // 참조 순서가 있다: 무기/건물/패시브 → 아이템, 적 → 웨이브
            ImportWeapons(log);
            ImportBuildings(log);
            ImportPassives(log);
            ImportEnemies(log);
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 2차: 다른 애셋을 참조하는 테이블 (위에서 만든 애셋을 로드해야 하므로 Refresh 이후)
        try
        {
            AssetDatabase.StartAssetEditing();
            ImportItems(log);
            ImportWaves(log);
            ImportClasses(log);
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 3차: 씬 컴포넌트에 직접 써 넣는 테이블
        ImportEvents(log);
        ImportEconomy(log);

        Debug.Log("[BalanceImporter] Import 완료\n" + log);
    }

    // ── 적 ──────────────────────────────────────────────────────────

    private static void ImportEnemies(StringBuilder log)
    {
        var table = LoadCsv("Enemies.csv", log);
        if (table == null) return;

        foreach (var row in table.Rows)
        {
            var id = CsvRow.Str(row, "Id");
            if (string.IsNullOrEmpty(id)) continue;

            var a = GetOrCreate<EnemyData>(EnemyFolder, id);
            a.EnemyName    = CsvRow.Str  (row, "EnemyName", id);
            a.Prefab       = LoadRef<GameObject>(row, "Prefab", a.Prefab);
            a.Sprite       = LoadRef<Sprite>    (row, "Sprite", a.Sprite);
            a.Tint         = CsvRow.Color(row, "Tint", a.Tint);
            a.SizeScale    = CsvRow.Float(row, "SizeScale",     a.SizeScale);

            a.MaxHp         = CsvRow.Float(row, "MaxHp",         a.MaxHp);
            a.MoveSpeed     = CsvRow.Float(row, "MoveSpeed",     a.MoveSpeed);
            a.ContactDamage = CsvRow.Float(row, "ContactDamage", a.ContactDamage);
            a.Armor         = CsvRow.Float(row, "Armor",         a.Armor);
            a.XpDrop        = CsvRow.Int  (row, "XpDrop",        a.XpDrop);
            a.CurrencyDrop  = CsvRow.Int  (row, "CurrencyDrop",  a.CurrencyDrop);

            a.AI = CsvRow.Enum(row, "AI", a.AI);

            a.ProjectilePrefab = LoadRef<GameObject>(row, "ProjectilePrefab", a.ProjectilePrefab);
            a.PreferredRange   = CsvRow.Float(row, "PreferredRange",   a.PreferredRange);
            a.AttackCooldown   = CsvRow.Float(row, "AttackCooldown",   a.AttackCooldown);
            a.ProjectileSpeed  = CsvRow.Float(row, "ProjectileSpeed",  a.ProjectileSpeed);
            a.ProjectileDamage = CsvRow.Float(row, "ProjectileDamage", a.ProjectileDamage);

            a.ChargeRange     = CsvRow.Float(row, "ChargeRange",     a.ChargeRange);
            a.ChargeWindup    = CsvRow.Float(row, "ChargeWindup",    a.ChargeWindup);
            a.ChargeSpeedMult = CsvRow.Float(row, "ChargeSpeedMult", a.ChargeSpeedMult);
            a.ChargeDuration  = CsvRow.Float(row, "ChargeDuration",  a.ChargeDuration);
            a.ChargeRecover   = CsvRow.Float(row, "ChargeRecover",   a.ChargeRecover);
            a.ChargeCooldown  = CsvRow.Float(row, "ChargeCooldown",  a.ChargeCooldown);

            a.EliteHpMult     = CsvRow.Float(row, "EliteHpMult",     a.EliteHpMult);
            a.EliteDamageMult = CsvRow.Float(row, "EliteDamageMult", a.EliteDamageMult);
            a.EliteSpeedMult  = CsvRow.Float(row, "EliteSpeedMult",  a.EliteSpeedMult);
            a.EliteXpMult     = CsvRow.Int  (row, "EliteXpMult",     a.EliteXpMult);

            a.BossHpMult     = CsvRow.Float(row, "BossHpMult",     a.BossHpMult);
            a.BossDamageMult = CsvRow.Float(row, "BossDamageMult", a.BossDamageMult);
            a.BossSpeedMult  = CsvRow.Float(row, "BossSpeedMult",  a.BossSpeedMult);
            a.BossXpMult     = CsvRow.Int  (row, "BossXpMult",     a.BossXpMult);

            EditorUtility.SetDirty(a);
        }
        log.AppendLine($"  Enemies   : {table.RowCount}");
    }

    // ── 무기 ────────────────────────────────────────────────────────

    private static void ImportWeapons(StringBuilder log)
    {
        var table = LoadCsv("Weapons.csv", log);
        if (table == null) return;

        foreach (var row in table.Rows)
        {
            var id = CsvRow.Str(row, "Id");
            if (string.IsNullOrEmpty(id)) continue;

            var a = GetOrCreate<WeaponData>(WeaponFolder, id);
            a.WeaponName      = CsvRow.Str(row, "WeaponName", id);
            a.WeaponPrefab    = LoadRef<GameObject>(row, "WeaponPrefab",     a.WeaponPrefab);
            a.Icon            = LoadRef<Sprite>    (row, "Icon",             a.Icon);
            a.ProjectilePrefab= LoadRef<GameObject>(row, "ProjectilePrefab", a.ProjectilePrefab);
            a.TravelPrefab    = LoadRef<GameObject>(row, "TravelPrefab",     a.TravelPrefab);

            a.ProjectileSpeed = CsvRow.Float (row, "ProjectileSpeed", a.ProjectileSpeed);
            a.Damage          = CsvRow.Floats(row, "Damage",          a.Damage);
            a.Cooldown        = CsvRow.Floats(row, "Cooldown",        a.Cooldown);
            a.ProjectileSize  = CsvRow.Floats(row, "ProjectileSize",  a.ProjectileSize);
            a.ProjectileCount = CsvRow.Ints  (row, "ProjectileCount", a.ProjectileCount);
            a.Range           = CsvRow.Floats(row, "Range",           a.Range);

            EditorUtility.SetDirty(a);
        }
        log.AppendLine($"  Weapons   : {table.RowCount}");
    }

    // ── 건물 ────────────────────────────────────────────────────────

    private static void ImportBuildings(StringBuilder log)
    {
        var table = LoadCsv("Buildings.csv", log);
        if (table == null) return;

        foreach (var row in table.Rows)
        {
            var id = CsvRow.Str(row, "Id");
            if (string.IsNullOrEmpty(id)) continue;

            var a = GetOrCreate<BuildingData>(BuildingFolder, id);
            a.BuildingName   = CsvRow.Str(row, "BuildingName", id);
            a.Prefab         = LoadRef<GameObject>(row, "Prefab", a.Prefab);
            a.Icon           = LoadRef<Sprite>    (row, "Icon",   a.Icon);
            a.Damage         = CsvRow.Floats(row, "Damage",         a.Damage);
            a.AttackRange    = CsvRow.Floats(row, "AttackRange",    a.AttackRange);
            a.AttackCooldown = CsvRow.Floats(row, "AttackCooldown", a.AttackCooldown);
            a.MaxCount       = CsvRow.Ints  (row, "MaxCount",       a.MaxCount);
            a.Output         = CsvRow.Floats(row, "Output",         a.Output);

            EditorUtility.SetDirty(a);
        }
        log.AppendLine($"  Buildings : {table.RowCount}");
    }

    // ── 패시브 ──────────────────────────────────────────────────────

    private static void ImportPassives(StringBuilder log)
    {
        var table = LoadCsv("Passives.csv", log);
        if (table == null) return;

        foreach (var row in table.Rows)
        {
            var id = CsvRow.Str(row, "Id");
            if (string.IsNullOrEmpty(id)) continue;

            var a = GetOrCreate<PassiveData>(PassiveFolder, id);
            a.PassiveName        = CsvRow.Str(row, "PassiveName", id);
            a.BonusMaxHp         = CsvRow.Floats(row, "BonusMaxHp",         a.BonusMaxHp);
            a.BonusMoveSpeed     = CsvRow.Floats(row, "BonusMoveSpeed",     a.BonusMoveSpeed);
            a.BonusDamage        = CsvRow.Floats(row, "BonusDamage",        a.BonusDamage);
            a.BonusAttackSpeed   = CsvRow.Floats(row, "BonusAttackSpeed",   a.BonusAttackSpeed);
            a.BonusProjectileSize= CsvRow.Floats(row, "BonusProjectileSize",a.BonusProjectileSize);
            a.BonusPickupRadius  = CsvRow.Floats(row, "BonusPickupRadius",  a.BonusPickupRadius);
            a.BonusCritChance    = CsvRow.Floats(row, "BonusCritChance",    a.BonusCritChance);
            a.BonusArmor         = CsvRow.Floats(row, "BonusArmor",         a.BonusArmor);
            a.BonusXpGain        = CsvRow.Floats(row, "BonusXpGain",        a.BonusXpGain);
            a.BonusGoldGain      = CsvRow.Floats(row, "BonusGoldGain",      a.BonusGoldGain);
            a.BonusBuildingCooldown = CsvRow.Floats(row, "BonusBuildingCooldown", a.BonusBuildingCooldown);

            EditorUtility.SetDirty(a);
        }
        log.AppendLine($"  Passives  : {table.RowCount}");
    }

    // ── 아이템 (무기/건물/패시브를 Id 로 참조) ─────────────────────

    private static void ImportItems(StringBuilder log)
    {
        var table = LoadCsv("Items.csv", log);
        if (table == null) return;

        foreach (var row in table.Rows)
        {
            var id = CsvRow.Str(row, "Id");
            if (string.IsNullOrEmpty(id)) continue;

            var a = GetOrCreate<ItemData>(ItemFolder, id);
            a.ItemName    = CsvRow.Str(row, "ItemName", id);
            a.Description = CsvRow.Str(row, "Description", a.Description);
            a.Icon        = LoadRef<Sprite>(row, "Icon", a.Icon);
            a.MaxLevel    = CsvRow.Int(row, "MaxLevel",  a.MaxLevel);
            a.ShopPrice   = CsvRow.Int(row, "ShopPrice", a.ShopPrice);

            var category = CsvRow.Str(row, "Category", "Passive");
            var refId    = CsvRow.Str(row, "RefId");

            // 카테고리를 바꿔도 옛 참조가 남아 있으면 혼란스러우니 매번 전부 비우고 하나만 채운다.
            a.WeaponRef = null; a.BuildingRef = null; a.PassiveRef = null;

            switch (category)
            {
                case "Weapon":
                    a.Category  = ItemCategory.Weapon;
                    a.WeaponRef = LoadById<WeaponData>(WeaponFolder, refId, id, "WeaponData", log);
                    break;
                case "Building":
                    a.Category    = ItemCategory.Building;
                    a.BuildingRef = LoadById<BuildingData>(BuildingFolder, refId, id, "BuildingData", log);
                    break;
                default:
                    a.Category   = ItemCategory.Passive;
                    a.PassiveRef = LoadById<PassiveData>(PassiveFolder, refId, id, "PassiveData", log);
                    break;
            }

            EditorUtility.SetDirty(a);
        }
        log.AppendLine($"  Items     : {table.RowCount}");
    }

    // ── 웨이브 ──────────────────────────────────────────────────────

    private static void ImportWaves(StringBuilder log)
    {
        var table = LoadCsv("Waves.csv", log);
        if (table == null) return;

        foreach (var row in table.Rows)
        {
            var id = CsvRow.Str(row, "Id");
            if (string.IsNullOrEmpty(id)) continue;

            var a = GetOrCreate<WaveData>(WaveFolder, id);
            a.UseTimerClear = CsvRow.Bool (row, "UseTimerClear", a.UseTimerClear);
            a.SurvivalTime  = CsvRow.Float(row, "SurvivalTime",  a.SurvivalTime);
            a.UseKillClear  = CsvRow.Bool (row, "UseKillClear",  a.UseKillClear);
            a.KillTarget    = CsvRow.Int  (row, "KillTarget",    a.KillTarget);
            a.SpawnRadius   = CsvRow.Float(row, "SpawnRadius",   a.SpawnRadius);
            a.MaxAlive      = CsvRow.Int  (row, "MaxAlive",      a.MaxAlive);
            a.EliteCount    = CsvRow.Int  (row, "EliteCount",    a.EliteCount);
            a.EliteTime     = CsvRow.Float(row, "EliteTime",     a.EliteTime);
            a.BossTime      = CsvRow.Float(row, "BossTime",      a.BossTime);

            a.EliteOverride = LoadById<EnemyData>(EnemyFolder, CsvRow.Str(row, "EliteOverride"), id, "EnemyData", log);
            a.BossOverride  = LoadById<EnemyData>(EnemyFolder, CsvRow.Str(row, "BossOverride"),  id, "EnemyData", log);

            a.Spawns = ParseSpawns(CsvRow.Str(row, "Spawns"), id, log);

            EditorUtility.SetDirty(a);
        }
        log.AppendLine($"  Waves     : {table.RowCount}");
    }

    /// <summary>
    /// 스폰 셀 문법: <c>Goblin*18@0.9:10-60|Zombie*22@0.6</c>
    /// (적Id * 마리수 @ 스폰간격초 : 시작초-종료초).
    ///
    /// <para>간격을 생략하면 0.5초, 시간창을 생략하면 웨이브 내내(0부터 제한 없음)다.
    /// 항목끼리는 <b>동시에</b> 진행되므로 시간창이 곧 난이도 곡선이다.</para>
    /// </summary>
    private static List<WaveSpawnEntry> ParseSpawns(string cell, string waveId, StringBuilder log)
    {
        var list = new List<WaveSpawnEntry>();
        if (string.IsNullOrWhiteSpace(cell)) return list;

        foreach (var chunk in cell.Split(CsvTable.ArraySeparator))
        {
            var part = chunk.Trim();
            if (part.Length == 0) continue;

            // 시간창 ':시작-종료' — '@' 보다 먼저 떼야 한다 (뒤쪽에 붙기 때문)
            float start = 0f, end = 0f;
            int colon = part.IndexOf(':');
            if (colon >= 0)
            {
                var window = part[(colon + 1)..].Trim();
                part = part[..colon];

                int dash = window.IndexOf('-');
                if (dash >= 0)
                {
                    ParseFloat(window[..dash],        out start);
                    ParseFloat(window[(dash + 1)..],  out end);
                }
                else ParseFloat(window, out start);
            }

            float interval = 0.5f;
            int at = part.IndexOf('@');
            if (at >= 0)
            {
                ParseFloat(part[(at + 1)..], out interval);
                part = part[..at];
            }

            int count = 1;
            int star = part.IndexOf('*');
            if (star >= 0)
            {
                int.TryParse(part[(star + 1)..].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out count);
                part = part[..star];
            }

            var enemy = LoadById<EnemyData>(EnemyFolder, part.Trim(), waveId, "EnemyData", log);
            if (enemy == null) continue;

            list.Add(new WaveSpawnEntry
            {
                Enemy         = enemy,
                Count         = count,
                SpawnInterval = interval,
                StartTime     = start,
                EndTime       = end
            });
        }
        return list;
    }

    private static void ParseFloat(string s, out float value) =>
        float.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);

    // ── 직업 (무기를 Id 로 참조) ───────────────────────────────────

    private static void ImportClasses(StringBuilder log)
    {
        var table = LoadCsv("Classes.csv", log);
        if (table == null) return;

        foreach (var row in table.Rows)
        {
            var id = CsvRow.Str(row, "Id");
            if (string.IsNullOrEmpty(id)) continue;

            var a = GetOrCreate<CharacterClassData>(ClassFolder, id);
            a.ClassName   = CsvRow.Str(row, "ClassName", id);
            a.Description = CsvRow.Str(row, "Description", a.Description);

            a.StartingWeapon      = LoadById<WeaponData>(WeaponFolder, CsvRow.Str(row, "StartingWeapon"), id, "WeaponData", log);
            a.StartingWeaponLevel = CsvRow.Int(row, "StartingWeaponLevel", a.StartingWeaponLevel);

            a.BonusMaxHp          = CsvRow.Float(row, "BonusMaxHp",          a.BonusMaxHp);
            a.BonusMoveSpeed      = CsvRow.Float(row, "BonusMoveSpeed",      a.BonusMoveSpeed);
            a.BonusDamage         = CsvRow.Float(row, "BonusDamage",         a.BonusDamage);
            a.BonusAttackSpeed    = CsvRow.Float(row, "BonusAttackSpeed",    a.BonusAttackSpeed);
            a.BonusProjectileSize = CsvRow.Float(row, "BonusProjectileSize", a.BonusProjectileSize);
            a.BonusPickupRadius   = CsvRow.Float(row, "BonusPickupRadius",   a.BonusPickupRadius);
            a.BonusCritChance     = CsvRow.Float(row, "BonusCritChance",     a.BonusCritChance);
            a.BonusArmor          = CsvRow.Float(row, "BonusArmor",          a.BonusArmor);
            a.BonusXpGain         = CsvRow.Float(row, "BonusXpGain",         a.BonusXpGain);
            a.BonusGoldGain       = CsvRow.Float(row, "BonusGoldGain",       a.BonusGoldGain);

            a.Portrait    = LoadRef<Sprite>    (row, "Portrait",    a.Portrait);
            a.BodySprite  = LoadRef<Sprite>    (row, "BodySprite",  a.BodySprite);
            a.WalkFrames  = LoadSpriteSheet    (row, "WalkSheet",   a.WalkFrames);
            a.ModelPrefab = LoadRef<GameObject>(row, "ModelPrefab", a.ModelPrefab);

            a.UnlockedByDefault = CsvRow.Bool(row, "UnlockedByDefault", a.UnlockedByDefault);
            a.UnlockCost        = CsvRow.Int (row, "UnlockCost",        a.UnlockCost);

            EditorUtility.SetDirty(a);
        }
        log.AppendLine($"  Classes   : {table.RowCount}");
    }

    // ── 이벤트 (씬의 EventManager 에 직접 기록) ────────────────────

    private static void ImportEvents(StringBuilder log)
    {
        var table = LoadCsv("Events.csv", log);
        if (table == null) return;

        var mgr = FindSceneComponent("EventManager");
        if (mgr == null) { log.AppendLine("  Events    : 씬에서 EventManager 를 찾지 못해 건너뜀"); return; }

        var so    = new SerializedObject(mgr);
        var array = so.FindProperty("events");
        array.arraySize = table.RowCount;

        for (int i = 0; i < table.RowCount; i++)
        {
            var row = table.Rows[i];
            var el  = array.GetArrayElementAtIndex(i);
            el.FindPropertyRelative("Title").stringValue             = CsvRow.Str(row, "Title");
            el.FindPropertyRelative("Description").stringValue       = CsvRow.Str(row, "Description");
            el.FindPropertyRelative("XpBonus").intValue              = CsvRow.Int(row, "XpBonus");
            el.FindPropertyRelative("CurrencyBonus").intValue        = CsvRow.Int(row, "CurrencyBonus");
            el.FindPropertyRelative("TriggerRandomWave").boolValue   = CsvRow.Bool(row, "TriggerRandomWave");
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        MarkSceneDirty(mgr);
        log.AppendLine($"  Events    : {table.RowCount} (EventManager)");
    }

    // ── 씬 컴포넌트의 직렬화 필드에 직접 기록 ─────────────────────

    private static void ImportEconomy(StringBuilder log)
    {
        ImportComponentFields("Economy.csv",    log);
        ImportComponentFields("SceneWiring.csv", log);
    }

    /// <summary>
    /// Component / Field / Value 3열 테이블을 씬 컴포넌트의 직렬화 필드에 그대로 써 넣는다.
    /// Field 는 SerializedProperty 경로라 <c>baseStats.MaxHp</c> 같은 중첩도 된다.
    /// 오브젝트 참조는 애셋 경로로 적고, 배열은 | 로 나눈다.
    /// </summary>
    private static void ImportComponentFields(string file, StringBuilder log)
    {
        var table = LoadCsv(file, log);
        if (table == null) return;

        int applied = 0;
        foreach (var row in table.Rows)
        {
            var typeName = CsvRow.Str(row, "Component");
            var field    = CsvRow.Str(row, "Field");
            var value    = CsvRow.Str(row, "Value");
            if (string.IsNullOrEmpty(typeName) || string.IsNullOrEmpty(field)) continue;

            var comp = FindSceneComponent(typeName);
            if (comp == null) { log.AppendLine($"    ! 씬에 {typeName} 없음 ({field})"); continue; }

            var so   = new SerializedObject(comp);
            var prop = so.FindProperty(field);
            if (prop == null) { log.AppendLine($"    ! {typeName}.{field} 필드 없음"); continue; }

            if (!WriteProperty(prop, value)) { log.AppendLine($"    ! {typeName}.{field} 타입 미지원"); continue; }

            so.ApplyModifiedPropertiesWithoutUndo();
            MarkSceneDirty(comp);
            applied++;
        }
        log.AppendLine($"  {file,-16}: {applied}/{table.RowCount} 적용");
    }

    private static bool WriteProperty(SerializedProperty prop, string value)
    {
        if (prop.isArray && prop.propertyType != SerializedPropertyType.String)
        {
            var parts = string.IsNullOrEmpty(value)
                ? new string[0]
                : value.Split(CsvTable.ArraySeparator);

            prop.arraySize = parts.Length;
            for (int i = 0; i < parts.Length; i++)
                if (!WriteScalar(prop.GetArrayElementAtIndex(i), parts[i].Trim())) return false;
            return true;
        }
        return WriteScalar(prop, value);
    }

    private static bool WriteScalar(SerializedProperty prop, string value)
    {
        switch (prop.propertyType)
        {
            case SerializedPropertyType.Integer:
                if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)) prop.intValue = i;
                return true;
            case SerializedPropertyType.Float:
                if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var f)) prop.floatValue = f;
                return true;
            case SerializedPropertyType.Boolean:
                var v = value.ToLowerInvariant();
                prop.boolValue = v == "1" || v == "true" || v == "y" || v == "yes";
                return true;
            case SerializedPropertyType.String:
                prop.stringValue = value;
                return true;
            case SerializedPropertyType.LayerMask:
                // 숫자(비트마스크) 또는 레이어 이름 목록("Building|Player") 둘 다 받는다.
                prop.intValue = int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var mask)
                    ? mask
                    : ParseLayerNames(value);
                return true;
            case SerializedPropertyType.ObjectReference:
                // 빈 칸은 "참조 없음"이 아니라 "건드리지 않음"으로 본다.
                if (!string.IsNullOrEmpty(value))
                    prop.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Object>(value);
                return true;
            default:
                return false;
        }
    }

    /// <summary>"Building|Player" → 비트마스크. 없는 레이어 이름은 무시된다.</summary>
    private static int ParseLayerNames(string value)
    {
        int mask = 0;
        if (string.IsNullOrWhiteSpace(value)) return 0;
        foreach (var part in value.Split(CsvTable.ArraySeparator))
        {
            var layer = LayerMask.NameToLayer(part.Trim());
            if (layer >= 0) mask |= 1 << layer;
        }
        return mask;
    }

    // ════════════════════════════════════════════════════════════════
    //  Export  (인스펙터에서 손댄 값을 CSV 로 되돌린다)
    // ════════════════════════════════════════════════════════════════

    [MenuItem("Game/Balance/Export ScriptableObjects -> CSV", priority = 1)]
    public static void ExportAll()
    {
        EnsureFolder(CsvFolder);

        ExportRows("Enemies.csv",
            "Id,EnemyName,Prefab,Sprite,Tint,SizeScale,MaxHp,MoveSpeed,ContactDamage,Armor,XpDrop,CurrencyDrop," +
            "AI,ProjectilePrefab,PreferredRange,AttackCooldown,ProjectileSpeed,ProjectileDamage," +
            "ChargeRange,ChargeWindup,ChargeSpeedMult,ChargeDuration,ChargeRecover,ChargeCooldown," +
            "EliteHpMult,EliteDamageMult,EliteSpeedMult,EliteXpMult,BossHpMult,BossDamageMult,BossSpeedMult,BossXpMult",
            LoadAll<EnemyData>(EnemyFolder), (a, id) => string.Join(",",
                id, E(a.EnemyName), E(Path(a.Prefab)), E(Path(a.Sprite)), CsvTable.ToHex(a.Tint), N(a.SizeScale),
                N(a.MaxHp), N(a.MoveSpeed), N(a.ContactDamage), N(a.Armor), a.XpDrop, a.CurrencyDrop,
                a.AI, E(Path(a.ProjectilePrefab)), N(a.PreferredRange), N(a.AttackCooldown),
                N(a.ProjectileSpeed), N(a.ProjectileDamage),
                N(a.ChargeRange), N(a.ChargeWindup), N(a.ChargeSpeedMult),
                N(a.ChargeDuration), N(a.ChargeRecover), N(a.ChargeCooldown),
                N(a.EliteHpMult), N(a.EliteDamageMult), N(a.EliteSpeedMult), a.EliteXpMult,
                N(a.BossHpMult), N(a.BossDamageMult), N(a.BossSpeedMult), a.BossXpMult));

        ExportRows("Weapons.csv",
            "Id,WeaponName,WeaponPrefab,Icon,ProjectilePrefab,TravelPrefab,ProjectileSpeed,Damage,Cooldown,ProjectileSize,ProjectileCount,Range",
            LoadAll<WeaponData>(WeaponFolder), (a, id) => string.Join(",",
                id, E(a.WeaponName), E(Path(a.WeaponPrefab)), E(Path(a.Icon)), E(Path(a.ProjectilePrefab)),
                E(Path(a.TravelPrefab)),
                N(a.ProjectileSpeed), CsvTable.JoinArray(a.Damage), CsvTable.JoinArray(a.Cooldown),
                CsvTable.JoinArray(a.ProjectileSize), CsvTable.JoinArray(a.ProjectileCount), CsvTable.JoinArray(a.Range)));

        ExportRows("Buildings.csv",
            "Id,BuildingName,Prefab,Icon,Damage,AttackRange,AttackCooldown,MaxCount,Output",
            LoadAll<BuildingData>(BuildingFolder), (a, id) => string.Join(",",
                id, E(a.BuildingName), E(Path(a.Prefab)), E(Path(a.Icon)),
                CsvTable.JoinArray(a.Damage), CsvTable.JoinArray(a.AttackRange),
                CsvTable.JoinArray(a.AttackCooldown), CsvTable.JoinArray(a.MaxCount),
                CsvTable.JoinArray(a.Output)));

        ExportRows("Passives.csv",
            "Id,PassiveName,BonusMaxHp,BonusMoveSpeed,BonusDamage,BonusAttackSpeed,BonusProjectileSize," +
            "BonusPickupRadius,BonusCritChance,BonusArmor,BonusXpGain,BonusGoldGain,BonusBuildingCooldown",
            LoadAll<PassiveData>(PassiveFolder), (a, id) => string.Join(",",
                id, E(a.PassiveName), CsvTable.JoinArray(a.BonusMaxHp), CsvTable.JoinArray(a.BonusMoveSpeed),
                CsvTable.JoinArray(a.BonusDamage), CsvTable.JoinArray(a.BonusAttackSpeed),
                CsvTable.JoinArray(a.BonusProjectileSize), CsvTable.JoinArray(a.BonusPickupRadius),
                CsvTable.JoinArray(a.BonusCritChance), CsvTable.JoinArray(a.BonusArmor),
                CsvTable.JoinArray(a.BonusXpGain), CsvTable.JoinArray(a.BonusGoldGain),
                CsvTable.JoinArray(a.BonusBuildingCooldown)));

        ExportRows("Items.csv",
            "Id,ItemName,Description,Icon,Category,MaxLevel,RefId,ShopPrice",
            LoadAll<ItemData>(ItemFolder), (a, id) => string.Join(",",
                id, E(a.ItemName), E(a.Description), E(Path(a.Icon)), a.Category, a.MaxLevel,
                E(a.Category switch
                {
                    ItemCategory.Weapon   => Name(a.WeaponRef),
                    ItemCategory.Building => Name(a.BuildingRef),
                    _                     => Name(a.PassiveRef)
                }),
                a.ShopPrice));

        ExportRows("Waves.csv",
            "Id,UseTimerClear,SurvivalTime,UseKillClear,KillTarget,SpawnRadius,MaxAlive,Spawns,EliteOverride,BossOverride,EliteCount,EliteTime,BossTime",
            LoadAll<WaveData>(WaveFolder), (a, id) => string.Join(",",
                id, a.UseTimerClear ? 1 : 0, N(a.SurvivalTime), a.UseKillClear ? 1 : 0, a.KillTarget,
                N(a.SpawnRadius), a.MaxAlive, E(JoinSpawns(a.Spawns)),
                E(Name(a.EliteOverride)), E(Name(a.BossOverride)), a.EliteCount,
                N(a.EliteTime), N(a.BossTime)));

        ExportRows("Classes.csv",
            "Id,ClassName,Description,StartingWeapon,StartingWeaponLevel,BonusMaxHp,BonusMoveSpeed,BonusDamage," +
            "BonusAttackSpeed,BonusProjectileSize,BonusPickupRadius,BonusCritChance,BonusArmor,BonusXpGain,BonusGoldGain," +
            "Portrait,BodySprite,WalkSheet,ModelPrefab,UnlockedByDefault,UnlockCost",
            LoadAll<CharacterClassData>(ClassFolder), (a, id) => string.Join(",",
                id, E(a.ClassName), E(a.Description), E(Name(a.StartingWeapon)), a.StartingWeaponLevel,
                N(a.BonusMaxHp), N(a.BonusMoveSpeed), N(a.BonusDamage), N(a.BonusAttackSpeed),
                N(a.BonusProjectileSize), N(a.BonusPickupRadius), N(a.BonusCritChance),
                N(a.BonusArmor), N(a.BonusXpGain), N(a.BonusGoldGain),
                E(Path(a.Portrait)), E(Path(a.BodySprite)),
                // 프레임은 전부 같은 .png 에서 나오므로 시트 경로 한 줄이면 복원된다.
                E(a.WalkFrames != null && a.WalkFrames.Length > 0 ? Path(a.WalkFrames[0]) : ""),
                E(Path(a.ModelPrefab)),
                a.UnlockedByDefault ? 1 : 0, a.UnlockCost));

        AssetDatabase.Refresh();
        Debug.Log($"[BalanceImporter] Export 완료 → {CsvFolder}");
    }

    private static string JoinSpawns(List<WaveSpawnEntry> spawns)
    {
        if (spawns == null || spawns.Count == 0) return "";
        var parts = new List<string>(spawns.Count);
        foreach (var s in spawns)
        {
            if (s?.Enemy == null) continue;

            string window = (s.StartTime > 0f || s.EndTime > 0f)
                          ? $":{N(s.StartTime)}-{N(s.EndTime)}"
                          : "";
            parts.Add($"{s.Enemy.name}*{s.Count}@{N(s.SpawnInterval)}{window}");
        }
        return string.Join(CsvTable.ArraySeparator.ToString(), parts);
    }

    private static void ExportRows<T>(string file, string header, List<T> assets,
                                      System.Func<T, string, string> toRow) where T : ScriptableObject
    {
        var sb = new StringBuilder();
        sb.AppendLine("# " + file + " — Game/Balance 메뉴로 CSV↔SO 를 오간다. 배열은 | 로 구분.");
        sb.AppendLine(header);
        foreach (var a in assets) sb.AppendLine(toRow(a, a.name));
        File.WriteAllText(System.IO.Path.Combine(CsvFolder, file), sb.ToString(), new UTF8Encoding(true));
    }

    // ════════════════════════════════════════════════════════════════
    //  공용 헬퍼
    // ════════════════════════════════════════════════════════════════

    private static CsvTable LoadCsv(string file, StringBuilder log)
    {
        var path = System.IO.Path.Combine(CsvFolder, file);
        if (!File.Exists(path)) { log.AppendLine($"  {file} 없음 — 건너뜀"); return null; }
        return CsvTable.Parse(File.ReadAllText(path));
    }

    private static T GetOrCreate<T>(string folder, string id) where T : ScriptableObject
    {
        EnsureFolder(folder);
        var path  = $"{folder}/{id}.asset";
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
        }
        return asset;
    }

    private static List<T> LoadAll<T>(string folder) where T : ScriptableObject
    {
        var list = new List<T>();
        if (!AssetDatabase.IsValidFolder(folder)) return list;
        foreach (var guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder }))
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
            if (a != null) list.Add(a);
        }
        list.Sort((x, y) => string.CompareOrdinal(x.name, y.name));
        return list;
    }

    /// <summary>Id 로 같은 폴더의 애셋을 찾는다. 못 찾으면 로그만 남기고 null.</summary>
    private static T LoadById<T>(string folder, string id, string owner, string kind, StringBuilder log)
        where T : ScriptableObject
    {
        if (string.IsNullOrEmpty(id)) return null;
        var a = AssetDatabase.LoadAssetAtPath<T>($"{folder}/{id}.asset");
        if (a == null) log.AppendLine($"    ! {owner}: {kind} '{id}' 를 찾을 수 없음");
        return a;
    }

    private static T LoadRef<T>(Dictionary<string, string> row, string key, T fallback) where T : Object
    {
        var path = CsvRow.Str(row, key);
        if (string.IsNullOrEmpty(path)) return fallback;
        var a = AssetDatabase.LoadAssetAtPath<T>(path);
        return a != null ? a : fallback;
    }

    /// <summary>
    /// 스프라이트시트 .png 하나에서 잘린 프레임을 전부 가져온다.
    /// LoadAssetAtPath 는 첫 장만 주기 때문에 LoadAllAssetsAtPath 로 훑어야 한다.
    /// 반환 순서는 이름의 숫자 꼬리표 기준 — Unity 가 돌려주는 순서는 보장되지 않는다.
    /// </summary>
    private static Sprite[] LoadSpriteSheet(Dictionary<string, string> row, string key, Sprite[] fallback)
    {
        var path = CsvRow.Str(row, key);
        if (string.IsNullOrEmpty(path)) return fallback;

        var list = new List<Sprite>();
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
            if (o is Sprite s) list.Add(s);

        if (list.Count == 0) return fallback;

        list.Sort((x, y) => FrameIndex(x.name).CompareTo(FrameIndex(y.name)));
        return list.ToArray();
    }

    /// <summary>"frame_10" → 10. 숫자 꼬리표가 없으면 int.MaxValue 라 뒤로 밀린다.</summary>
    private static int FrameIndex(string name)
    {
        int i = name.Length;
        while (i > 0 && char.IsDigit(name[i - 1])) i--;
        return i < name.Length && int.TryParse(name[i..], out var n) ? n : int.MaxValue;
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        var parent = folder[..folder.LastIndexOf('/')];
        var leaf   = folder[(folder.LastIndexOf('/') + 1)..];
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }

    /// <summary>열려 있는 씬에서 타입 이름으로 컴포넌트를 찾는다 (비활성 포함).</summary>
    private static MonoBehaviour FindSceneComponent(string typeName)
    {
        foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (mb != null && mb.GetType().Name == typeName) return mb;
        return null;
    }

    private static void MarkSceneDirty(Object obj)
    {
        EditorUtility.SetDirty(obj);
        if (obj is Component c) EditorSceneManager.MarkSceneDirty(c.gameObject.scene);
    }

    private static string E(string s)     => CsvTable.Escape(s);
    private static string N(float v)      => v.ToString("0.####", CultureInfo.InvariantCulture);
    private static string Path(Object o)  => o == null ? "" : AssetDatabase.GetAssetPath(o);
    private static string Name(Object o)  => o == null ? "" : o.name;
}
