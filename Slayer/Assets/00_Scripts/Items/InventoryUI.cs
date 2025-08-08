using System.Collections.Generic;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject inventorySlotPrefab;

    private Dictionary<ShopItem_SObj, InventorySlot> slotMap = new();

    void Start()
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        slotMap.Clear();

        foreach (var pair in InventoryManager.Instance.GetInventory())
        {
            if (pair.Value.count <= 0 && pair.Value.level <= 1) continue;

            GameObject slotObj = Instantiate(inventorySlotPrefab, contentParent);
            var slot = slotObj.GetComponent<InventorySlot>();
            slot.Setup(pair.Value);
            slotMap[pair.Key] = slot;
        }
    }

    public void RefreshItem(ShopItem_SObj item)
    {
        var entry = InventoryManager.Instance.GetEntry(item);
        if (entry == null) return;

        if (!slotMap.ContainsKey(item))
        {
            GameObject slotObj = Instantiate(inventorySlotPrefab, contentParent);
            var slot = slotObj.GetComponent<InventorySlot>();
            slot.Setup(entry);
            slotMap[item] = slot;
        }
        else
        {
            slotMap[item].UpdateUI();
        }
    }
}