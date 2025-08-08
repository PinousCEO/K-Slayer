using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;
    public InventoryUI inventoryUI;

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
        else Destroy(gameObject);
    }

    public void AddItem(ShopItem_SObj item)
    {
        if (!inventory.ContainsKey(item))
            inventory[item] = new InventoryEntry(item);

        var entry = inventory[item];
        entry.count++;

        inventoryUI.RefreshItem(item);
    }

    public void LevelUp(InventoryEntry entry)
    {
        int nextLevel = entry.level + 1;
        int required = nextLevel * 2;

        if (entry.count >= required && nextLevel <= 5)
        {
            entry.count -= required;
            entry.level++;
            inventoryUI.RefreshItem(entry.item);
            InventoryPopup.Instance.Open(entry);
        }
    }

    public Dictionary<ShopItem_SObj, InventoryEntry> GetInventory() => inventory;
    public InventoryEntry GetEntry(ShopItem_SObj item) => inventory.TryGetValue(item, out var entry) ? entry : null;
}
