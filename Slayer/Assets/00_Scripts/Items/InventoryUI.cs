using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform weaponGroup;
    [SerializeField] private RectTransform accessoryGroup;
    [SerializeField] private RectTransform skillGroup;
    [SerializeField] private GameObject inventorySlotPrefab;
    [SerializeField] private Button bulkUpgradeButton;

    private readonly Dictionary<ShopItem_SObj, InventorySlot> slotMap = new();
    private ItemCategory currentCategory = ItemCategory.Weapon;
    private RectTransform[] allGroups;
    private RectTransform activeGroup;

    private void Awake()
    {
        if (scrollRect != null) scrollRect.movementType = ScrollRect.MovementType.Clamped;

        var list = new List<RectTransform>(3);
        if (weaponGroup) list.Add(weaponGroup);
        if (accessoryGroup) list.Add(accessoryGroup);
        if (skillGroup) list.Add(skillGroup);
        allGroups = list.ToArray();

        activeGroup = GetGroupFor(currentCategory);
        for (int i = 0; i < allGroups.Length; i++)
            allGroups[i].gameObject.SetActive(allGroups[i] == activeGroup);

        if (bulkUpgradeButton != null)
        {
            bulkUpgradeButton.onClick.RemoveAllListeners();
            bulkUpgradeButton.onClick.AddListener(_BulkUpgrade);
        }
    }

    private void Start()
    {
        RefreshUI();
        ShowCategory(currentCategory);
    }

    public void _ShowWeaponTab() => ShowCategory(ItemCategory.Weapon);
    public void _ShowAccessoryTab() => ShowCategory(ItemCategory.Accessory);
    public void _ShowSkillTab() => ShowCategory(ItemCategory.Skill);

    private void _BulkUpgrade()
    {
        var mgr = InventoryManager.Instance;
        if (mgr == null) return;
        mgr.LevelUpAllInCategory(currentCategory);
    }

    private void ShowCategory(ItemCategory category)
    {
        currentCategory = category;
        var target = GetGroupFor(category);
        if (!target) return;

        if (activeGroup != target)
        {
            if (activeGroup) activeGroup.gameObject.SetActive(false);
            activeGroup = target;
            activeGroup.gameObject.SetActive(true);
        }
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
            EnsureSlot(desired[i]);

        ApplySortedOrder();
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
        }
        else
        {
            EnsureSlot(entry);
        }

        ApplySortedOrder();
    }

    private void EnsureSlot(InventoryManager.InventoryEntry entry)
    {
        if (entry == null || entry.item == null || inventorySlotPrefab == null) return;

        var parent = GetGroupFor(entry.item.category);
        if (!parent) return;

        if (!slotMap.TryGetValue(entry.item, out var slot) || slot == null)
        {
            var go = Object.Instantiate(inventorySlotPrefab, parent);
            slot = go.GetComponent<InventorySlot>();
            slotMap[entry.item] = slot;
        }

        slot.Setup(entry);

        if (slot.transform.parent != parent)
            slot.transform.SetParent(parent, false);
    }

    private void ApplySortedOrder()
    {
        SortGroup(weaponGroup);
        SortGroup(accessoryGroup);
        SortGroup(skillGroup);
    }

    private void SortGroup(Transform group)
    {
        if (!group) return;

        var temp = new List<InventorySlot>(group.childCount);
        for (int i = 0; i < group.childCount; i++)
        {
            var s = group.GetChild(i).GetComponent<InventorySlot>();
            if (s != null) temp.Add(s);
        }

        temp.Sort((a, b) =>
        {
            var ea = GetEntryFromSlot(a);
            var eb = GetEntryFromSlot(b);
            if (ea == null && eb == null) return 0;
            if (ea == null) return 1;
            if (eb == null) return -1;

            int ta = InventoryManager.Instance.GetTier(ea);
            int tb = InventoryManager.Instance.GetTier(eb);
            int r = tb.CompareTo(ta);
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

    private RectTransform GetGroupFor(ItemCategory category)
    {
        return category switch
        {
            ItemCategory.Weapon => weaponGroup,
            ItemCategory.Accessory => accessoryGroup,
            ItemCategory.Skill => skillGroup,
            _ => null
        };
    }
}
