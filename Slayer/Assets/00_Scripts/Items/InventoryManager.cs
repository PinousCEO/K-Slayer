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
        else { Destroy(gameObject); }
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

    public float GetAttack(InventoryEntry entry)
    {
        if (!IsValid(entry)) return 0f;

        float atk = entry.item.baseAttack;
        var levels = entry.item.upgradeLevels;
        if (levels == null || levels.Count == 0) return atk;

        int gained = Mathf.Clamp(entry.level - 1, 0, levels.Count);
        for (int i = 0; i < gained; i++) atk += levels[i].attackAdd;

        return atk;
    }

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
