using System.Collections.Generic;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private Transform weaponContent;
    [SerializeField] private Transform accessoryContent;
    [SerializeField] private Transform skillContent;
    [SerializeField] private GameObject inventorySlotPrefab;

    private readonly Dictionary<ShopItem_SObj, InventorySlot> slotMap = new();
    private ItemCategory currentCategory = ItemCategory.Weapon;
    private Transform[] allContents;
    private Transform activeParent;

    private void Awake()
    {
        var list = new List<Transform>(3);
        if (weaponContent != null && !list.Contains(weaponContent)) list.Add(weaponContent);
        if (accessoryContent != null && !list.Contains(accessoryContent)) list.Add(accessoryContent);
        if (skillContent != null && !list.Contains(skillContent)) list.Add(skillContent);
        allContents = list.ToArray();

        activeParent = GetParentFor(currentCategory);
        for (int i = 0; i < allContents.Length; i++)
            allContents[i].gameObject.SetActive(allContents[i] == activeParent);
    }

    private void Start()
    {
        RefreshUI();
        ShowCategory(currentCategory);
    }

    public void _ShowWeaponTab() => ShowCategory(ItemCategory.Weapon);
    public void _ShowAccessoryTab() => ShowCategory(ItemCategory.Accessory);
    public void _ShowSkillTab() => ShowCategory(ItemCategory.Skill);

    private void ShowCategory(ItemCategory category)
    {
        currentCategory = category;
        var target = GetParentFor(category);
        if (target == null) return;

        if (activeParent == target) return;

        if (activeParent != null) activeParent.gameObject.SetActive(false);
        activeParent = target;
        activeParent.gameObject.SetActive(true);
    }

    public void RefreshUI()
    {
        var invMgr = InventoryManager.Instance;
        if (invMgr == null) return;

        var inv = invMgr.GetInventory();
        if (inv == null) return;

        var desired = new List<InventoryManager.InventoryEntry>(inv.Count);
        foreach (var e in inv.Values)
        {
            if (e == null || e.item == null) continue;
            if (e.count <= 0 && e.level <= 1) continue;
            desired.Add(e);
        }

        var desiredSet = new HashSet<ShopItem_SObj>();
        for (int i = 0; i < desired.Count; i++)
            desiredSet.Add(desired[i].item);

        var toRemove = new List<ShopItem_SObj>();
        foreach (var kv in slotMap)
            if (!desiredSet.Contains(kv.Key)) toRemove.Add(kv.Key);

        for (int i = 0; i < toRemove.Count; i++)
        {
            var key = toRemove[i];
            if (slotMap.TryGetValue(key, out var slot) && slot != null)
                Destroy(slot.gameObject);
            slotMap.Remove(key);
        }

        for (int i = 0; i < desired.Count; i++)
        {
            var entry = desired[i];
            EnsureSlot(entry);
        }

        ApplySortedOrder();
        ShowCategory(currentCategory);
    }

    public void RefreshItem(ShopItem_SObj item)
    {
        var invMgr = InventoryManager.Instance;
        if (invMgr == null || item == null) return;

        var entry = invMgr.GetEntry(item);
        bool visible = entry != null && entry.item != null && (entry.count > 0 || entry.level > 1);

        if (!visible)
        {
            if (slotMap.TryGetValue(item, out var oldSlot) && oldSlot != null)
                Destroy(oldSlot.gameObject);
            slotMap.Remove(item);
            ApplySortedOrder();
            return;
        }

        EnsureSlot(entry);
        ApplySortedOrder();
    }

    private void EnsureSlot(InventoryManager.InventoryEntry entry)
    {
        if (entry == null || entry.item == null || inventorySlotPrefab == null) return;

        var parent = GetParentFor(entry.item.category);
        if (parent == null) return;

        if (!slotMap.TryGetValue(entry.item, out var slot) || slot == null)
        {
            var go = Instantiate(inventorySlotPrefab, parent);
            slot = go.GetComponent<InventorySlot>();
            slotMap[entry.item] = slot;
        }

        slot.Setup(entry);

        if (slot.transform.parent != parent)
            slot.transform.SetParent(parent, false);
    }

    private void ApplySortedOrder()
    {
        if (weaponContent != null) SortParent(weaponContent);
        if (accessoryContent != null) SortParent(accessoryContent);
        if (skillContent != null) SortParent(skillContent);
    }

    private void SortParent(Transform parent)
    {
        if (parent == null) return;

        var temp = new List<InventorySlot>(parent.childCount);
        for (int i = 0; i < parent.childCount; i++)
        {
            var s = parent.GetChild(i).GetComponent<InventorySlot>();
            if (s != null) temp.Add(s);
        }

        temp.Sort((a, b) =>
        {
            var ea = GetEntryFromSlot(a);
            var eb = GetEntryFromSlot(b);
            if (ea == null && eb == null) return 0;
            if (ea == null) return 1;
            if (eb == null) return -1;

            int r = ((int)ea.item.rarity).CompareTo((int)eb.item.rarity);
            if (r != 0) return r;
            return string.Compare(ea.item.itemName, eb.item.itemName, System.StringComparison.Ordinal);
        });

        for (int i = 0; i < temp.Count; i++)
            temp[i].transform.SetSiblingIndex(i);
    }

    private InventoryManager.InventoryEntry GetEntryFromSlot(InventorySlot slot)
    {
        foreach (var kv in slotMap)
            if (kv.Value == slot) return InventoryManager.Instance.GetEntry(kv.Key);
        return null;
    }

    private Transform GetParentFor(ItemCategory category)
    {
        switch (category)
        {
            case ItemCategory.Weapon: return weaponContent;
            case ItemCategory.Accessory: return accessoryContent;
            case ItemCategory.Skill: return skillContent;
            default: return null;
        }
    }
}
