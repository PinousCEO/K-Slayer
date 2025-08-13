using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillSlot : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private Button detailBtn;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Image fillBar;
    [SerializeField] private TMP_Text fillText;
    [SerializeField] private TMP_Text tierText;

    private SkillPopup popup;
    public InventoryManager.InventoryEntry Entry { get; private set; }

    public void Setup(InventoryManager.InventoryEntry data, SkillPopup popupRef)
    {
        Entry = data;
        popup = popupRef;
        var item = data.item;

        if (icon) { icon.sprite = item.icon; icon.preserveAspect = true; }
        if (detailBtn)
        {
            detailBtn.onClick.RemoveAllListeners();
            detailBtn.onClick.AddListener(() => popup?.OpenUI(Entry));
        }
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (Entry == null) return;
        if (countText) countText.text = Entry.count.ToString();
        if (levelText) levelText.text = $"Lv.{Entry.level}";

        var inv = InventoryManager.Instance;
        int maxLv = inv.GetMaxLevel(Entry);
        int nextCost = inv.GetNextLevelCost(Entry);

        if (fillBar)
        {
            if (nextCost > 0 && Entry.level < maxLv) fillBar.fillAmount = Mathf.Clamp01((float)Entry.count / nextCost);
            else fillBar.fillAmount = 1f;
        }
        if (fillText)
        {
            if (nextCost > 0 && Entry.level < maxLv) fillText.text = $"{Entry.count}/{nextCost}";
            else fillText.text = "-";
        }
        if (tierText)
        {
            int t = inv.GetTier(Entry);
            tierText.text = inv.GetTierStepText(t);
        }
    }
}
