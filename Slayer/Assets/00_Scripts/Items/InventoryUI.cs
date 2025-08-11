using System.Collections.Generic;
using System.Linq;
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

    private void Awake()
    {
        allContents = new[] { weaponContent, accessoryContent, skillContent }
            .Where(t => t != null)
            .Distinct()
            .ToArray();
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
        for (int i = 0; i < allContents.Length; i++)
            allContents[i].gameObject.SetActive(false);

        var target = GetParentFor(category);
        if (target != null) target.gameObject.SetActive(true);
    }

    public void RefreshUI()
    {
        ClearChildren(weaponContent);
        ClearChildren(accessoryContent);
        ClearChildren(skillContent);
        slotMap.Clear();

        var inv = InventoryManager.Instance != null ? InventoryManager.Instance.GetInventory() : null;
        if (inv == null) return;

        var sortedList = inv.Values
            .Where(entry => entry != null && (entry.count > 0 || entry.level > 1))
            .OrderBy(entry => (int)entry.item.rarity)
            .ThenBy(entry => entry.item.name)
            .ToList();

        foreach (var entry in sortedList)
        {
            var parent = GetParentFor(entry.item.category);
            if (parent == null || inventorySlotPrefab == null) continue;

            var slotObj = Instantiate(inventorySlotPrefab, parent);
            var slot = slotObj.GetComponent<InventorySlot>();
            if (slot == null) continue;

            slot.Setup(entry);
            slotMap[entry.item] = slot;
        }

        ShowCategory(currentCategory);
    }

    public void RefreshItem(ShopItem_SObj item)
    {
        if (InventoryManager.Instance == null || item == null) return;
        var entry = InventoryManager.Instance.GetEntry(item);
        if (entry == null) return;

        RefreshUI();
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

    private void ClearChildren(Transform t)
    {
        if (t == null) return;
        for (int i = t.childCount - 1; i >= 0; i--)
            Destroy(t.GetChild(i).gameObject);
    }
}
