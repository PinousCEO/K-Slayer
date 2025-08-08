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
        icon.sprite = entry.item.icon;
        UpdateUI();
        detailBtn.GetComponent<Button>().onClick.AddListener(OpenPopup);
    }

    public void UpdateUI()
    {
        countText.text = $"{entry.count}";
        levelText.text = $"{entry.level}";
        int nextLevel = entry.level + 1;
        int required = nextLevel * 2;
        fillBar.fillAmount = Mathf.Clamp01((float)entry.count / required);
    }

    void OpenPopup()
    {
        InventoryPopup.Instance.Open(entry);
    }
}
