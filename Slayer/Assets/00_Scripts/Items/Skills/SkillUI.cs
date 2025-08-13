using System.Collections.Generic;
using UnityEngine;

public class SkillUI : MonoBehaviour
{
    [SerializeField] private Transform skillContainer;
    [SerializeField] private GameObject skillSlotPrefab;
    [SerializeField] private SkillPopup skillPopup;

    private readonly Dictionary<ShopItem_BaseSObj, SkillSlot> slotMap = new();

    public void RefreshSkills()
    {
        var inv = InventoryManager.Instance?.GetInventory();
        if (inv == null) return;

        var desired = new List<InventoryManager.InventoryEntry>();
        foreach (var e in inv.Values)
        {
            if (e?.item == null) continue;
            if (e.item.category != ItemCategory.Skill) continue;
            if (e.count <= 0 && e.level <= 1) continue;
            desired.Add(e);
        }

        var desiredSet = new HashSet<ShopItem_BaseSObj>();
        foreach (var e in desired) desiredSet.Add(e.item);

        var toRemove = new List<ShopItem_BaseSObj>();
        foreach (var kv in slotMap)
            if (!desiredSet.Contains(kv.Key)) toRemove.Add(kv.Key);

        for (int i = 0; i < toRemove.Count; i++)
        {
            if (slotMap.TryGetValue(toRemove[i], out var s) && s != null) Destroy(s.gameObject);
            slotMap.Remove(toRemove[i]);
        }

        foreach (var e in desired) EnsureSlot(e);
        ApplySortedOrder();
    }

    public void RefreshItem(ShopItem_BaseSObj item)
    {
        if (item == null || item.category != ItemCategory.Skill) return;
        var e = InventoryManager.Instance?.GetEntry(item);
        bool visible = e != null && (e.count > 0 || e.level > 1);

        if (!visible)
        {
            if (slotMap.TryGetValue(item, out var s) && s) Destroy(s.gameObject);
            slotMap.Remove(item);
        }
        else EnsureSlot(e);

        ApplySortedOrder();
    }

    public void OpenPopup(InventoryManager.InventoryEntry entry) => skillPopup?.OpenUI(entry);

    private void EnsureSlot(InventoryManager.InventoryEntry e)
    {
        if (e == null || e.item == null || skillSlotPrefab == null || skillContainer == null) return;

        if (!slotMap.TryGetValue(e.item, out var slot) || slot == null)
        {
            var go = Instantiate(skillSlotPrefab, skillContainer);
            slot = go.GetComponent<SkillSlot>();
            slotMap[e.item] = slot;
        }

        slot.Setup(e, skillPopup);
        if (slot.transform.parent != skillContainer) slot.transform.SetParent(skillContainer, false);
    }

    private void ApplySortedOrder()
    {
        if (skillContainer == null) return;

        var list = new List<SkillSlot>();
        for (int i = 0; i < skillContainer.childCount; i++)
        {
            var s = skillContainer.GetChild(i).GetComponent<SkillSlot>();
            if (s) list.Add(s);
        }

        list.Sort((a, b) =>
        {
            var ea = a.Entry; var eb = b.Entry;
            if (ea == null && eb == null) return 0;
            if (ea == null) return 1; if (eb == null) return -1;
            int ta = InventoryManager.Instance.GetTier(ea);
            int tb = InventoryManager.Instance.GetTier(eb);
            int r = tb.CompareTo(ta);
            if (r != 0) return r;
            return string.Compare(ea.item.itemName, eb.item.itemName, System.StringComparison.Ordinal);
        });

        for (int i = 0; i < list.Count; i++) list[i].transform.SetSiblingIndex(i);
    }
}
