using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class InventorySlot : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private Button detailBtn;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Image fillBar;

    private InventoryManager.InventoryEntry entry;

    public void Setup(InventoryManager.InventoryEntry data)
    {
        entry = data;
        if (entry == null) return;

        if (icon == null) icon = GetComponentInChildren<Image>(true);

        if (icon != null)
        {
            if (entry.item != null && entry.item.icon != null)
            {
                icon.sprite = entry.item.icon;
                icon.preserveAspect = true;
                icon.enabled = true;
            }
            else
            {
                icon.enabled = false;
            }
        }

        if (detailBtn != null)
        {
            detailBtn.onClick.RemoveAllListeners();
            detailBtn.onClick.AddListener(OpenPopup);
        }

        UpdateUI();
    }

    public void UpdateUI()
    {
        if (entry == null) return;

        if (countText != null) countText.text = entry.count.ToString();
        if (levelText != null) levelText.text = entry.level.ToString();

        var inv = InventoryManager.Instance;
        if (inv == null) return;

        int maxLv = inv.GetMaxLevel(entry);
        int required = inv.GetNextLevelCost(entry);

        if (fillBar != null)
        {
            if (required > 0 && entry.level < maxLv)
                fillBar.fillAmount = Mathf.Clamp01((float)entry.count / required);
            else
                fillBar.fillAmount = 1f;
        }
    }

    private void OpenPopup()
    {
        if (entry == null) return;
        InventoryManager.Instance.inventoryPopup.OpenUI(entry);
    }
}
