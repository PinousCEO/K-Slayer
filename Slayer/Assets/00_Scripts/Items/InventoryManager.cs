using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;
    [SerializeField] public InventoryUI inventoryUI;
    [SerializeField] public InventoryPopup inventoryPopup;

    [System.Serializable]
    public class InventoryEntry
    {
        public ShopItem_SObj item;
        public string label;
        public Rarity rarity;
        public int count;
        public int level;
        public InventoryEntry(ShopItem_SObj item)
        {
            this.item = item;
            count = 0;
            level = 1;
        }
    }

    private readonly Dictionary<ShopItem_SObj, InventoryEntry> inventory = new();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void AddItem(ShopItem_SObj item)
    {
        if (item == null) return;
        var entry = GetOrCreateEntry(item);
        entry.count++;
        inventoryUI?.RefreshItem(item);
    }

    public void LevelUp(InventoryEntry entry) => LevelUpMultiple(entry, 1);

    public void LevelUpMultiple(InventoryEntry entry, int steps)
    {
        if (!IsValid(entry) || steps <= 0) return;
        int performed = 0;
        int maxLevel = GetMaxLevel(entry);
        if (entry.level >= maxLevel) return;
        for (int i = 0; i < steps; i++)
        {
            if (entry.level >= maxLevel) break;
            int cost = GetNextLevelCost(entry);
            if (cost <= 0 || entry.count < cost) break;
            entry.count -= cost;
            entry.level++;
            performed++;
        }
        if (performed > 0)
        {
            inventoryUI?.RefreshItem(entry.item);
            inventoryPopup.RefreshCurrent();
        }
    }

    public int LevelUpAllInCategory(ItemCategory category)
    {
        if (inventory.Count == 0) return 0;
        int total = 0;
        bool progressed = true;
        while (progressed)
        {
            progressed = false;
            foreach (var kv in inventory)
            {
                var e = kv.Value;
                if (!IsValid(e)) continue;
                if (e.item.category != category) continue;
                int maxLv = GetMaxLevel(e);
                if (e.level >= maxLv) continue;
                int cost = GetNextLevelCost(e);
                if (cost <= 0 || e.count < cost) continue;
                e.count -= cost;
                e.level++;
                total++;
                progressed = true;
            }
        }
        if (total > 0)
        {
            inventoryUI?.RefreshUI();
            inventoryPopup.RefreshCurrent();
        }
        return total;
    }

    public int GetMaxLevel(InventoryEntry entry)
    {
        if (!IsValid(entry)) return 1;
        int extra = entry.item.upgradeLevels != null ? Mathf.Max(0, entry.item.upgradeLevels.Count) : 0;
        return 1 + extra;
    }

    public int GetNextLevelCost(InventoryEntry entry)
    {
        if (!IsValid(entry)) return 0;
        var levels = entry.item.upgradeLevels;
        if (levels == null || levels.Count == 0) return 0;
        int maxLv = GetMaxLevel(entry);
        if (entry.level >= maxLv) return 0;
        int idx = entry.level - 1;
        if (idx < 0 || idx >= levels.Count) return 0;
        return Mathf.Max(0, levels[idx].cost);
    }

    static float Sum(List<UpgradeStat> list, UpgradeType type)
    {
        if (list == null) return 0f;
        float v = 0f;
        for (int i = 0; i < list.Count; i++)
            if (list[i] != null && list[i].type == type) v += list[i].value;
        return v;
    }

    public float GetEquipTotal(InventoryEntry entry, UpgradeType type)
    {
        if (!IsValid(entry)) return 0f;
        float baseVal = type == UpgradeType.DMGIncrease ? entry.item.baseAttack : 0f;
        var levels = entry.item.upgradeLevels;
        if (levels == null || levels.Count == 0) return baseVal;
        int gained = Mathf.Clamp(entry.level - 1, 0, levels.Count);
        float inc = 0f;
        for (int i = 0; i < gained; i++) inc += Sum(levels[i].equipAdd, type);
        return baseVal + inc;
    }

    public float GetEquipNextTotal(InventoryEntry entry, UpgradeType type)
    {
        if (!IsValid(entry)) return 0f;
        int maxLv = GetMaxLevel(entry);
        if (entry.level >= maxLv) return GetEquipTotal(entry, type);
        var levels = entry.item.upgradeLevels;
        if (levels == null || levels.Count == 0) return GetEquipTotal(entry, type);
        int idx = entry.level - 1;
        float nextDelta = (idx >= 0 && idx < levels.Count) ? Sum(levels[idx].equipAdd, type) : 0f;
        return GetEquipTotal(entry, type) + nextDelta;
    }

    public float GetOwnedTotalForItem(InventoryEntry entry, UpgradeType type)
    {
        if (!IsValid(entry)) return 0f;
        var levels = entry.item.upgradeLevels;
        if (levels == null || levels.Count == 0) return 0f;
        int gained = Mathf.Clamp(entry.level - 1, 0, levels.Count);
        float inc = 0f;
        for (int i = 0; i < gained; i++) inc += Sum(levels[i].ownedAdd, type);
        return inc;
    }

    public float GetOwnedNextTotalForItem(InventoryEntry entry, UpgradeType type)
    {
        if (!IsValid(entry)) return 0f;
        int maxLv = GetMaxLevel(entry);
        if (entry.level >= maxLv) return GetOwnedTotalForItem(entry, type);
        var levels = entry.item.upgradeLevels;
        if (levels == null || levels.Count == 0) return GetOwnedTotalForItem(entry, type);
        int idx = entry.level - 1;
        float nextDelta = (idx >= 0 && idx < levels.Count) ? Sum(levels[idx].ownedAdd, type) : 0f;
        return GetOwnedTotalForItem(entry, type) + nextDelta;
    }

    public float GetOwnedTotalGlobal(UpgradeType type)
    {
        float total = 0f;
        foreach (var kv in inventory)
        {
            var e = kv.Value;
            if (!IsValid(e)) continue;
            total += GetOwnedTotalForItem(e, type);
        }
        return total;
    }

    public int GetTier(InventoryEntry entry)
    {
        if (!IsValid(entry)) return 1;
        int total = System.Enum.GetValues(typeof(Rarity)).Length;
        int idx = Mathf.Clamp((int)entry.item.rarity, 0, total - 1);
        int tier = total - idx;
        return Mathf.Clamp(tier, 1, total);
    }

    public string GetTierStepText(int tier) => $"{tier}등급";

    public Dictionary<ShopItem_SObj, InventoryEntry> GetInventory() => inventory;

    public InventoryEntry GetEntry(ShopItem_SObj item) =>
        item != null && inventory.TryGetValue(item, out var entry) ? entry : null;

    private InventoryEntry GetOrCreateEntry(ShopItem_SObj item)
    {
        if (inventory.TryGetValue(item, out var entry)) return entry;
        entry = new InventoryEntry(item);
        inventory[item] = entry;
        return entry;
    }

    private static bool IsValid(InventoryEntry entry) => entry != null && entry.item != null;
}
