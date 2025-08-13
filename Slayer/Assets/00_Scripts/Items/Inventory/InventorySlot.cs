using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class InventorySlot : MonoBehaviour
{
    [Header("UI Refs")]
    [Tooltip("아이템 아이콘 이미지")][SerializeField] private Image icon;
    [Tooltip("상세 보기 버튼")][SerializeField] private Button detailBtn;
    [Tooltip("아이템 조각/개수 텍스트")][SerializeField] private TMP_Text countText;
    [Tooltip("아이템 레벨 텍스트 (+_)")][SerializeField] private TMP_Text levelText;
    [Tooltip("업그레이드 게이지 이미지")][SerializeField] private Image fillBar;
    [Tooltip("업그레이드 게이지 텍스트 (보유/필요)")][SerializeField] private TMP_Text fillText;
    [Tooltip("티어 텍스트 (_단계)")][SerializeField] private TMP_Text tierText;

    private InventoryManager.InventoryEntry entry;

    /// <summary>  SetUp Slot     /// </summary>
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
            else { icon.enabled = false; }
        }

        if (detailBtn != null)
        {
            detailBtn.onClick.RemoveAllListeners();
            detailBtn.onClick.AddListener(OpenPopup);
            detailBtn.interactable = true;
        }

        UpdateUI();
    }

    /// <summary> UpdateUI based on entry  /// </summary>
    public void UpdateUI()
    {
        if (entry == null) return;

        if (countText != null) countText.text = entry.count.ToString();
        if (levelText != null) levelText.text = $"+ {entry.level}";

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

        if (fillText != null)
        {
            if (required > 0 && entry.level < maxLv) fillText.text = $"{entry.count}/{required}";
            else fillText.text = "-"; // Max
        }

        if (tierText != null)
        {
            int tier = inv.GetTier(entry);
            tierText.text = inv.GetTierStepText(tier);
        }
    }

    /// <summary> Open Inventory Popup /// </summary>
    private void OpenPopup()
    {
        if (entry == null || entry.item == null) return;

        var mgr = InventoryManager.Instance;
        if (mgr == null) return;

        mgr.inventoryPopup?.OpenUI(entry);
    }
}
