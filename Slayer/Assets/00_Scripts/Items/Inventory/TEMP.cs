#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Generate 25 Weapons, 25 Accessories, and 9 Skills from Assets/resource/* sprites.</summary>
public class DummyItemAutoGenerator : MonoBehaviour
{
    // --- Source sprite folders (NOT Resources folder) ---
    private static readonly string WeaponSpriteFolder = "Assets/resource/weapon";
    private static readonly string AccessorySpriteFolder = "Assets/resource/acc";
    private static readonly string SkillSpriteFolder = "Assets/resource/skill";

    // --- Output root (SO will be created here) ---
    private static readonly string OutputRoot = "Assets/06_Datas";

    // --- Expected counts ---
    private const int ExpectWeapons = 25;
    private const int ExpectAccessories = 25;
    private const int ExpectSkills = 9;

    // --- Random ranges ---
    private static readonly Vector2 WeaponAttackRange = new(25, 80);
    private static readonly Vector2 AccessoryBaseRange = new(30, 120);
    private static readonly Vector2 SkillMultiplierRange = new(250, 800);
    private static readonly Vector2Int RequiredHitsRange = new(4, 12);
    private static readonly Vector2Int HpCostRange = new(0, 30);
    private static readonly Vector2Int ResourceCostRange = new(10, 60);

    private const int UpgradeSteps = 7;

    [MenuItem("Tools/Generate Dummy Items")]
    public static void GenerateAll()
    {
        EnsureFolder($"{OutputRoot}/Weapon");
        EnsureFolder($"{OutputRoot}/Accessory");
        EnsureFolder($"{OutputRoot}/Skill");

        var weaponSprites = LoadSprites(WeaponSpriteFolder);
        var accSprites = LoadSprites(AccessorySpriteFolder);
        var skillSprites = LoadSprites(SkillSpriteFolder);

        weaponSprites = TakeExactly(weaponSprites, ExpectWeapons);
        accSprites = TakeExactly(accSprites, ExpectAccessories);
        skillSprites = TakeExactly(skillSprites, ExpectSkills);

        weaponSprites = SortByColorKeywordThenName(weaponSprites);
        accSprites = SortByColorKeywordThenName(accSprites);

        AssetDatabase.StartAssetEditing();
        try
        {
            GenerateByOrderedRarity(accSprites, $"{OutputRoot}/Accessory", ItemCategory.Accessory);
            GenerateByOrderedRarity(weaponSprites, $"{OutputRoot}/Weapon", ItemCategory.Weapon);
            GenerateSkills(skillSprites, $"{OutputRoot}/Skill");
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        Debug.Log("✅ Dummy item generation complete.");
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        string parent = "Assets";
        foreach (var part in folder.Replace('\\', '/').Split('/'))
        {
            if (string.IsNullOrEmpty(part) || part == "Assets") continue;
            string current = $"{parent}/{part}";
            if (!AssetDatabase.IsValidFolder(current))
                AssetDatabase.CreateFolder(parent, part);
            parent = current;
        }
    }

    private static List<Sprite> LoadSprites(string folder)
    {
        var list = new List<Sprite>();
        if (!AssetDatabase.IsValidFolder(folder))
        {
            Debug.LogWarning($"⚠ Sprite folder not found: {folder}");
            return list;
        }

        var guids = AssetDatabase.FindAssets("t:Sprite", new[] { folder });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var spr = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (spr) list.Add(spr);
        }
        return list;
    }

    private static List<Sprite> TakeExactly(List<Sprite> list, int n)
        => (list?.Count ?? 0) <= n ? new List<Sprite>(list ?? new()) : list.Take(n).ToList();

    private static string MakeDisplayName(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "Item";
        var name = raw.Replace('_', ' ').Trim();
        return char.ToUpper(name[0]) + (name.Length > 1 ? name.Substring(1) : string.Empty);
    }

    private static string MakeSafeFilename(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "NewItem";
        var invalid = Path.GetInvalidFileNameChars();
        var safe = new string(raw.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return safe.Trim();
    }

    private static string CreateUniqueAssetPath(string folder, string fileNameNoExt)
    {
        var safeName = MakeSafeFilename(fileNameNoExt);
        var path = $"{folder}/{safeName}.asset";
        return AssetDatabase.GenerateUniqueAssetPath(path);
    }

    private static void SaveSO(ScriptableObject so, string folder, string nameHint)
    {
        if (so == null) return;
        EnsureFolder(folder);
        var uniquePath = CreateUniqueAssetPath(folder, string.IsNullOrEmpty(nameHint) ? "NewItem" : nameHint);
        AssetDatabase.CreateAsset(so, uniquePath);
        EditorUtility.SetDirty(so);
    }

    private static void GenerateByOrderedRarity(List<Sprite> sprites, string outFolder, ItemCategory cat)
    {
        if (sprites == null || sprites.Count == 0) return;

        var rarities = (Rarity[])Enum.GetValues(typeof(Rarity));
        int groupSize = Mathf.Max(1, Mathf.CeilToInt(sprites.Count / (float)rarities.Length));

        for (int i = 0; i < sprites.Count; i++)
        {
            var spr = sprites[i];
            var rarity = rarities[Mathf.Min(rarities.Length - 1, i / groupSize)];

            if (cat == ItemCategory.Weapon) CreateWeaponSO(spr, rarity, outFolder, i + 1);
            else CreateAccessorySO(spr, rarity, outFolder, i + 1);
        }
    }

    private static List<Sprite> SortByColorKeywordThenName(List<Sprite> src)
    {
        int ColorKey(string n)
        {
            n = (n ?? string.Empty).ToLowerInvariant();
            if (n.Contains("red")) return 10;
            if (n.Contains("orange")) return 20;
            if (n.Contains("yellow")) return 30;
            if (n.Contains("green")) return 40;
            if (n.Contains("cyan")) return 50;
            if (n.Contains("blue")) return 60;
            if (n.Contains("purple")) return 70;
            if (n.Contains("violet")) return 75;
            if (n.Contains("pink")) return 80;
            if (n.Contains("white")) return 90;
            if (n.Contains("gray") || n.Contains("grey")) return 95;
            if (n.Contains("black")) return 99;
            return 0;
        }

        return src
            .OrderBy(s => ColorKey(s ? s.name : string.Empty))
            .ThenBy(s => s ? s.name : string.Empty, StringComparer.Ordinal)
            .ToList();
    }

    private static void CreateWeaponSO(Sprite icon, Rarity rarity, string folder, int index)
    {
        var so = ScriptableObject.CreateInstance<Weapon_SObj>();
        so.category = ItemCategory.Weapon;
        so.icon = icon;
        so.itemName = icon ? MakeDisplayName(icon.name) : $"Weapon_{index:00}";
        so.itemDescription = "Generated weapon.";
        so.rarity = rarity;
        so.attack = Mathf.RoundToInt(UnityEngine.Random.Range(WeaponAttackRange.x, WeaponAttackRange.y));
        so.upgradeLevels = BuildWeaponUpgrades(UpgradeSteps);


        so.upgradeLevels[0].ownedAdd = new List<UpgradeStat>
        {
            new UpgradeStat { label = "HP%", type = UpgradeType.HealthIncrease, value = 5 },
            new UpgradeStat { label = "ATK%", type = UpgradeType.DMGIncrease, value = 3 },
            new UpgradeStat { label = "DEF%", type = UpgradeType.DefenseIncrease, value = 2 }
        };

        SaveSO(so, folder, so.itemName);
    }

    private static void CreateAccessorySO(Sprite icon, Rarity rarity, string folder, int index)
    {
        var so = ScriptableObject.CreateInstance<Accessory_SObj>();
        so.category = ItemCategory.Accessory;
        so.icon = icon;
        so.itemName = icon ? MakeDisplayName(icon.name) : $"Accessory_{index:00}";
        so.itemDescription = "Generated accessory.";
        so.rarity = rarity;
        so.defence = Mathf.RoundToInt(UnityEngine.Random.Range(AccessoryBaseRange.x, AccessoryBaseRange.y));
        so.upgradeLevels = BuildAccessoryUpgrades(UpgradeSteps);

        so.upgradeLevels[0].ownedAdd = new List<UpgradeStat>
        {
            new UpgradeStat { label = "HP%", type = UpgradeType.HealthIncrease, value = 5 },
            new UpgradeStat { label = "ATK%", type = UpgradeType.DMGIncrease, value = 3 },
            new UpgradeStat { label = "DEF%", type = UpgradeType.DefenseIncrease, value = 2 }
        };
        SaveSO(so, folder, so.itemName);
    }

    private static void GenerateSkills(List<Sprite> sprites, string outFolder)
    {
        if (sprites == null || sprites.Count == 0)
        {
            Debug.LogWarning("⚠ No skill sprites found.");
            return;
        }

        for (int i = 0; i < sprites.Count; i++)
        {
            var spr = sprites[i];
            CreateSkillSO(spr, GuessElementFromName(spr ? spr.name : ""), outFolder, i + 1);
        }
    }

    private static void CreateSkillSO(Sprite icon, ElementType element, string folder, int index)
    {
        var so = ScriptableObject.CreateInstance<Skill_SObj>();
        so.category = ItemCategory.Skill;
        so.icon = icon;
        so.itemName = icon ? MakeDisplayName(icon.name) : $"Skill_{index:00}";
        so.itemDescription = "Generated skill.";
        so.element = element;
        so.skillAttack = Mathf.RoundToInt(UnityEngine.Random.Range(SkillMultiplierRange.x, SkillMultiplierRange.y));
        so.requiredHits = UnityEngine.Random.Range(RequiredHitsRange.x, RequiredHitsRange.y);
        so.hpCost = UnityEngine.Random.Range(HpCostRange.x, HpCostRange.y);
        so.resourceCostPerUse = UnityEngine.Random.Range(ResourceCostRange.x, ResourceCostRange.y);
        so.autoToggleDefault = true;
        so.upgradeLevels = BuildSkillUpgrades(UpgradeSteps);
        SaveSO(so, folder, so.itemName);
    }

    private static List<UpgradeLevel> BuildWeaponUpgrades(int count)
    {
        var list = new List<UpgradeLevel>(count);
        for (int i = 0; i < count; i++)
        {
            list.Add(new UpgradeLevel
            {
                cost = 5 + i * 3,
                equipAdd = new List<UpgradeStat>
                {
                    new UpgradeStat { label = "ATK%", type = UpgradeType.DMGIncrease, value = 10 + i * 5 }
                }
            });
        }
        return list;
    }

    private static List<UpgradeLevel> BuildAccessoryUpgrades(int count)
    {
        var list = new List<UpgradeLevel>(count);
        for (int i = 0; i < count; i++)
        {
            list.Add(new UpgradeLevel
            {
                cost = 4 + i * 2,
                equipAdd = new List<UpgradeStat>
                {
                    new UpgradeStat { label = "HP%", type = UpgradeType.HealthIncrease, value = 12 + i * 4 }
                }
            });
        }
        return list;
    }

    private static List<UpgradeLevel> BuildSkillUpgrades(int count)
    {
        var list = new List<UpgradeLevel>(count);
        for (int i = 0; i < count; i++)
        {
            list.Add(new UpgradeLevel
            {
                cost = 6 + i * 3,
                equipAdd = new List<UpgradeStat>
                {
                    new UpgradeStat { label = "Skill ATK%", type = UpgradeType.DMGIncrease, value = 12 + i * 6 }
                }
            });
        }
        return list;
    }

    private static ElementType GuessElementFromName(string name)
    {
        var n = (name ?? string.Empty).ToLowerInvariant();
        if (n.Contains("fire") || n.Contains("flame") || n.Contains("lava")) return ElementType.Fire;
        if (n.Contains("ice") || n.Contains("frost") || n.Contains("snow")) return ElementType.Ice;
        if (n.Contains("thunder") || n.Contains("lightning") || n.Contains("shock")) return ElementType.Lightning;
        return ElementType.Normal;
    }
}
#endif
