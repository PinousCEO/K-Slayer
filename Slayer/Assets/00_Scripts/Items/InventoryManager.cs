using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;
    [SerializeField] public InventoryUI inventoryUI;

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

    private Dictionary<ShopItem_SObj, InventoryEntry> inventory = new();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    public void AddItem(ShopItem_SObj item)
    {
        if (!inventory.ContainsKey(item))
            inventory[item] = new InventoryEntry(item);

        var entry = inventory[item];
        entry.count++;

        if (inventoryUI != null) inventoryUI.RefreshItem(item);
    }

    public void LevelUp(InventoryEntry entry) => LevelUpMultiple(entry, 1);

    public void LevelUpMultiple(InventoryEntry entry, int steps)
    {
        if (entry == null || entry.item == null || steps <= 0) return;

        int performed = 0;
        for (int i = 0; i < steps; i++)
        {
            int cost = GetNextLevelCost(entry);
            if (cost <= 0) break;
            if (entry.count < cost) break;

            entry.count -= cost;
            entry.level++;
            performed++;

            int maxLevel = GetMaxLevel(entry);
            if (entry.level >= maxLevel) break;
        }

        if (performed > 0)
        {
            if (inventoryUI != null) inventoryUI.RefreshItem(entry.item);
            if (InventoryPopup.Instance != null) InventoryPopup.Instance.RefreshCurrent();
        }
    }

    public int GetMaxLevel(InventoryEntry entry)
    {
        int extra = (entry.item.upgradeLevels != null) ? Mathf.Max(0, entry.item.upgradeLevels.Count) : 0;
        return 1 + extra;
    }

    public int GetNextLevelCost(InventoryEntry entry)
    {
        var levels = entry.item.upgradeLevels;
        if (levels == null || levels.Count == 0) return 0;

        int current = entry.level;
        int maxLv = GetMaxLevel(entry);
        if (current >= maxLv) return 0;

        int idx = Mathf.Clamp(current - 1, 0, levels.Count - 1);
        return Mathf.Max(0, levels[idx].cost);
    }

    public float GetAttack(InventoryEntry entry)
    {
        if (entry == null || entry.item == null) return 0f;
        float atk = entry.item.baseAttack;
        var levels = entry.item.upgradeLevels;
        if (levels == null || levels.Count == 0) return atk;

        int gained = Mathf.Clamp(entry.level - 1, 0, levels.Count);
        for (int i = 0; i < gained; i++) atk += levels[i].attackAdd;
        return atk;
    }

    public Dictionary<ShopItem_SObj, InventoryEntry> GetInventory() => inventory;
    public InventoryEntry GetEntry(ShopItem_SObj item) => inventory.TryGetValue(item, out var entry) ? entry : null;
}