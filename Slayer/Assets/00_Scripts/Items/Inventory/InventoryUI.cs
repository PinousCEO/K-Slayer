using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [Header("Layout / Scroll")]
    [Tooltip("스크롤 뷰")][SerializeField] private ScrollRect scrollRect;

    [Header("Groups (Weapon / Accessory)")]
    [Tooltip("무기 트랜스폼")][SerializeField] private RectTransform weaponGroup;
    [Tooltip("악세사리 트랜스폼")][SerializeField] private RectTransform accessoryGroup;

    [Header("Prefabs & Actions")]
    [Tooltip("인벤토리 슬롯 프리팹")][SerializeField] private GameObject inventorySlotPrefab;
    [Tooltip("전체 업그레이드 버튼")][SerializeField] private Button bulkUpgradeButton;

    private readonly Dictionary<ShopItem_BaseSObj, InventorySlot> slotMap = new();
    private ItemCategory currentCategory = ItemCategory.Weapon;
    private RectTransform[] allGroups;
    private RectTransform activeGroup;

    private void Awake()
    {
        SetUp();
    }

    void SetUp()
    {
        var list = new List<RectTransform>(2);
        if (weaponGroup) list.Add(weaponGroup);
        if (accessoryGroup) list.Add(accessoryGroup);
        allGroups = list.ToArray();

        activeGroup = GetGroupFor(currentCategory);
        for (int i = 0; i < allGroups.Length; i++)
            if (allGroups[i]) allGroups[i].gameObject.SetActive(allGroups[i] == activeGroup);

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

    /// <summary> Show Weapon </summary>
    public void _ShowWeaponTab() => ShowCategory(ItemCategory.Weapon);
    /// <summary> Show Accessory </summary>
    public void _ShowAccessoryTab() => ShowCategory(ItemCategory.Accessory);
    /// <summary> Bulk Upgrade current Category </summary>
    private void _BulkUpgrade() { var mgr = InventoryManager.Instance; if (mgr == null) return; mgr.LevelUpAllInCategoryToMax(currentCategory); }

    /// <summary>    /// Change Category Tab    /// </summary>
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

            ScrollToTop(); // Scroll to Top
        }
    }

    /// <summary> Refresh All UI /// </summary>
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
            if (e.item.category == ItemCategory.Skill) continue;
            if (e.count <= 0 && e.level <= 1) continue;
            desired.Add(e);
        }

        var desiredSet = new HashSet<ShopItem_BaseSObj>();
        for (int i = 0; i < desired.Count; i++)
            desiredSet.Add(desired[i].item);

        var toRemove = new List<ShopItem_BaseSObj>();
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

    /// <summary> Refresh certain Item   /// </summary>
    public void RefreshItem(ShopItem_BaseSObj item)
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
        else { EnsureSlot(entry); }

        ApplySortedOrder();
    }

    /// <summary> Ensure Slot based on entry   /// </summary>
    private void EnsureSlot(InventoryManager.InventoryEntry entry)
    {
        if (entry == null || entry.item == null || inventorySlotPrefab == null) return;

        var parent = GetGroupFor(entry.item.category);
        if (!parent) return;

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

    /// <summary> Sort Items in group /// </summary>
    private void ApplySortedOrder()
    {
        SortGroup(weaponGroup);
        SortGroup(accessoryGroup);
    }

    /// <summary> Sort Group's Children and index </summary>
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

    /// <summary>Find Entry from slot</summary>
    private InventoryManager.InventoryEntry GetEntryFromSlot(InventorySlot slot)
    {
        foreach (var kv in slotMap)
            if (kv.Value == slot) return InventoryManager.Instance.GetEntry(kv.Key);
        return null;
    }

    /// <summary>Get RectTransform based on group</summary>
    private RectTransform GetGroupFor(ItemCategory category)
    {
        return category switch
        {
            ItemCategory.Weapon => weaponGroup,
            ItemCategory.Accessory => accessoryGroup,
            _ => null
        };
    }

    /// <summary> Scroll to Top  /// </summary>
    private void ScrollToTop()
    {
        if (scrollRect == null) return;
        scrollRect.verticalNormalizedPosition = 1f;
    }
}
