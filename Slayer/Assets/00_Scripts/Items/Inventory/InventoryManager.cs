using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [System.Serializable]
    public class InventoryEntry
    {
        public ShopItem_BaseSObj item;
        public int count = 0;
        public int level = 1;
    }

    [Header("하위 매니저 등록")]
    [Tooltip("InventoryUI 스크립트")][SerializeField] public InventoryUI inventoryUI;
    [Tooltip("InventoryPopUp 스크립트")][SerializeField] public InventoryPopup inventoryPopup;
    [Tooltip("SkillUI 스크립트")][SerializeField] public SkillUI skillUI;

    private readonly Dictionary<ShopItem_BaseSObj, InventoryEntry> _entries = new Dictionary<ShopItem_BaseSObj, InventoryEntry>(); // Inventory Entry 
    public Dictionary<ShopItem_BaseSObj, InventoryEntry> GetInventory() => _entries;

    private void Awake() { if (Instance == null) Instance = this; else if (Instance != this) Destroy(gameObject); }


    /// <summary>Get Item From Inventory(Entry)</summary>
    public InventoryEntry GetEntry(ShopItem_BaseSObj item)
    {
        if (item == null) return null;
        _entries.TryGetValue(item, out var e);
        return e;
    }

    /// <summary>Get Item From Inventory(Entry) or Create</summary>
    InventoryEntry GetOrCreateEntry(ShopItem_BaseSObj item)
    {
        if (item == null) return null;
        if (!_entries.TryGetValue(item, out var e))
        {
            e = new InventoryEntry { item = item, count = 0, level = 1 };
            _entries[item] = e;
        }
        return e;
    }

    /// <summary>Add Item To Inventory(Entry) and Refresh</summary>
    public void AddItem(ShopItem_BaseSObj item)
    {
        if (item == null) return;
        var e = GetOrCreateEntry(item);
        e.count++;

        if (item.category == ItemCategory.Skill) skillUI?.RefreshItem(item);
        else inventoryUI?.RefreshItem(item);
    }

    /// <summary>Level Up Current Entry Item</summary>
    public void LevelUpMultiple(InventoryEntry entry, int steps)
    {
        if (entry == null || entry.item == null || steps <= 0) return;
        int performed = 0;
        int maxLevel = GetMaxLevel(entry);

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
            if (entry.item.category == ItemCategory.Skill) skillUI?.RefreshItem(entry.item);
            else inventoryUI?.RefreshItem(entry.item);
            inventoryPopup?.RefreshCurrent();
        }
    }

    /// <summary>Level Up All Items In Category.</summary>
    public void LevelUpAllInCategoryToMax(ItemCategory category)
    {
        // Guard: allow only Weapon / Accessory
        if (category != ItemCategory.Weapon && category != ItemCategory.Accessory) return;

        bool any = false;

        foreach (var e in _entries.Values)
        {
            if (e?.item == null || e.item.category != category) continue;

            int before = e.level;
            int maxLv = GetMaxLevel(e);
            int diff = Mathf.Max(0, maxLv - e.level);

            if (diff > 0)
            {
                LevelUpMultiple(e, diff);
                if (e.level != before) any = true;
            }
        }

        if (any)
        {
            inventoryUI?.RefreshUI();
            inventoryPopup?.RefreshCurrent();
        }
    }

    /// <summary>Get Max Level Of Current Entry</summary>
    public int GetMaxLevel(InventoryEntry e)
    {
        int n = e?.item?.upgradeLevels?.Count ?? 0;
        return Mathf.Max(1, 1 + n); // Lv1 + 업글개수
    }

    /// <summary>Get Max Level Of ShopItem_SObj</summary>
    public int GetMaxLevel(ShopItem_BaseSObj item)
    {
        int n = item?.upgradeLevels?.Count ?? 0;
        return Mathf.Max(1, 1 + n);
    }

    /// <summary>Get LevelUp Cost Of Current Entry</summary>
    public int GetNextLevelCost(InventoryEntry e)
    {
        if (e == null || e.item == null) return 0;
        int idx = e.level - 1;
        var lvls = e.item.upgradeLevels;
        if (idx < 0 || lvls == null || idx >= lvls.Count) return 0;
        return Mathf.Max(0, lvls[idx].cost);
    }

    /// <summary>Get Tier Of Current Entry</summary>
    public int GetTier(InventoryEntry e) => e != null ? e.level : 1;

    /// <summary>Return Tier Text ( ~등급 )</summary>
    public string GetTierStepText(int tier) => $"{tier}등급";

    /// <summary>Get Equip Total Value Of Entry</summary>
    public float GetEquipTotal(InventoryEntry entry, UpgradeType type)
    {
        if (entry?.item?.upgradeLevels == null) return 0f;
        float sum = 0f;
        int last = Mathf.Min(entry.level, entry.item.upgradeLevels.Count);
        for (int i = 0; i < last; i++)
        {
            var lv = entry.item.upgradeLevels[i];
            if (lv?.equipAdd == null) continue;
            for (int j = 0; j < lv.equipAdd.Count; j++)
                if (lv.equipAdd[j].type == type) sum += lv.equipAdd[j].value;
        }
        return sum;
    }

    /// <summary> Get Equip Value of next level  /// </summary>
    public float GetEquipNextTotal(InventoryEntry entry, UpgradeType type)
    {
        float now = GetEquipTotal(entry, type);
        int idx = entry.level - 1;
        if (entry?.item?.upgradeLevels != null && idx >= 0 && idx < entry.item.upgradeLevels.Count)
        {
            var lv = entry.item.upgradeLevels[idx];
            if (lv?.equipAdd != null)
                for (int j = 0; j < lv.equipAdd.Count; j++)
                    if (lv.equipAdd[j].type == type) now += lv.equipAdd[j].value;
        }
        return now;
    }

    /// <summary>  Get current OwnedValue based on item  /// </summary>
    public float GetOwnedTotalForItem(InventoryEntry entry, UpgradeType type)
    {
        if (entry?.item?.upgradeLevels == null) return 0f;
        float sum = 0f;
        int last = Mathf.Min(entry.level, entry.item.upgradeLevels.Count);
        for (int i = 0; i < last; i++)
        {
            var lv = entry.item.upgradeLevels[i];
            if (lv?.ownedAdd == null) continue;
            for (int j = 0; j < lv.ownedAdd.Count; j++)
                if (lv.ownedAdd[j].type == type) sum += lv.ownedAdd[j].value;
        }
        return sum;
    }

    /// <summary>  Get value of next level's Owned Based  /// </summary>
    public float GetOwnedNextTotalForItem(InventoryEntry entry, UpgradeType type)
    {
        float now = GetOwnedTotalForItem(entry, type);
        int idx = entry.level - 1;
        if (entry?.item?.upgradeLevels != null && idx >= 0 && idx < entry.item.upgradeLevels.Count)
        {
            var lv = entry.item.upgradeLevels[idx];
            if (lv?.ownedAdd != null)
                for (int j = 0; j < lv.ownedAdd.Count; j++)
                    if (lv.ownedAdd[j].type == type) now += lv.ownedAdd[j].value;
        }
        return now;
    }
}
