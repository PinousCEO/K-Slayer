using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

        if (icon == null)
        {
            var tr = transform;
            if (tr.childCount > 0) icon = tr.GetChild(0).GetComponent<Image>();
        }

        if (icon != null && entry.item != null && entry.item.icon != null)
        {
            icon.sprite = entry.item.icon;
            icon.preserveAspect = true;
            icon.enabled = true;
        }

        UpdateUI();

        if (detailBtn != null)
        {
            detailBtn.onClick.RemoveAllListeners();
            detailBtn.onClick.AddListener(OpenPopup);
        }
    }

    public void UpdateUI()
    {
        if (entry == null) return;

        if (countText != null) countText.text = $"{entry.count}";
        if (levelText != null) levelText.text = $"{entry.level}";

        int nextLevel = entry.level + 1;
        int required = nextLevel * 2;
        if (fillBar != null)
            fillBar.fillAmount = Mathf.Clamp01(required > 0 ? (float)entry.count / required : 0f);
    }

    void OpenPopup()
    {
        if (InventoryPopup.Instance != null)
            InventoryPopup.Instance.Open(entry);
    }
}
