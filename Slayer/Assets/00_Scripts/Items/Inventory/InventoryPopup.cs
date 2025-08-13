using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryPopup : MonoBehaviour
{
    [Header("Root")]
    [Tooltip("인벤토리 팝업 Root 오브젝트")][SerializeField] private GameObject root;

    [Header("Main Info UI")]
    [Tooltip("아이템 아이콘 이미지")][SerializeField] private Image icon;
    [Tooltip("아이템 레벨 표시 텍스트")][SerializeField] private TMP_Text levelText;
    [Tooltip("아이템 티어(단계) 표시 텍스트")][SerializeField] private TMP_Text tierText;
    [Tooltip("아이템 이름 표시 텍스트")][SerializeField] private TMP_Text itemName;

    [Header("Effect Rows")]
    [Tooltip("장착 효과 표시 행 (최대 1줄)")][SerializeField] private PopupEffectRow[] equipRows;
    [Tooltip("보유 효과 표시 행 (최대 3줄)")][SerializeField] private PopupEffectRow[] ownedRows;

    [Header("Cost UI")]
    [Tooltip("업그레이드 수량 게이지")][SerializeField] private Image quantityFill;
    [Tooltip("업그레이드 수량 텍스트")][SerializeField] private TMP_Text quantityFillText;

    [Header("Buttons")]
    [Tooltip("레벨업 버튼")][SerializeField] private Button levelUpButton;
    [Tooltip("닫기 버튼")][SerializeField] private Button closeBtn;
    [Tooltip("이전 아이템 버튼")][SerializeField] private Button prevButton;
    [Tooltip("다음 아이템 버튼")][SerializeField] private Button nextButton;

    private InventoryManager.InventoryEntry currentEntry;
    private readonly List<InventoryManager.InventoryEntry> carousel = new();
    private int index;

    private void Start() { SetUpButton(); }

    void SetUpButton()
    {
        if (root != null) root.SetActive(false);

        if (levelUpButton != null) { levelUpButton.onClick.RemoveAllListeners(); levelUpButton.onClick.AddListener(OnClickLevelUp); }
        if (closeBtn != null) { closeBtn.onClick.RemoveAllListeners(); closeBtn.onClick.AddListener(CloseUI); }
        if (prevButton != null) { prevButton.onClick.RemoveAllListeners(); prevButton.onClick.AddListener(Prev); }
        if (nextButton != null) { nextButton.onClick.RemoveAllListeners(); nextButton.onClick.AddListener(Next); }
    }

    /// <summary>  Opening Inventory Based on Entry    /// </summary>
    public void OpenUI(InventoryManager.InventoryEntry entry)
    {
        if (entry == null || entry.item == null || root == null) return;
        BuildCarousel(entry);

        if (!root.activeSelf) root.SetActive(true);
        Show(index);
    }

    /// <summary>  Close PopUp    /// </summary>
    public void CloseUI()
    {
        if (root == null) return; if (root.activeSelf) root.SetActive(false);
    }

    /// <summary>  Item Category Build Carousel    /// </summary>
    private void BuildCarousel(InventoryManager.InventoryEntry focus)
    {
        carousel.Clear();
        var invMgr = InventoryManager.Instance;
        var inv = invMgr != null ? invMgr.GetInventory() : null;
        if (inv != null && inv.Count > 0)
        {
            foreach (var e in inv.Values)
            {
                if (e == null || e.item == null) continue;
                if (e.item.category != focus.item.category) continue;
                if (e.count <= 0 && e.level <= 1) continue;
                carousel.Add(e);
            }
            carousel.Sort((a, b) =>
            {
                int ta = InventoryManager.Instance.GetTier(a);
                int tb = InventoryManager.Instance.GetTier(b);
                int r = tb.CompareTo(ta);
                if (r != 0) return r;
                return string.Compare(a.item.itemName, b.item.itemName, System.StringComparison.Ordinal);
            });
        }

        int found = -1;
        for (int i = 0; i < carousel.Count; i++)
        {
            if (carousel[i].item == focus.item) { found = i; break; }
        }
        index = (found >= 0) ? found : carousel.Count;
        if (found < 0) carousel.Add(focus);
    }

    /// <summary>  Move to Previous Item    /// </summary>
    private void Prev() { int count = carousel.Count; if (count == 0) return; index = (index - 1 + count) % count; Show(index); }

    /// <summary>  Move to Next Item    /// </summary>
    private void Next() { int count = carousel.Count; if (count == 0) return; index = (index + 1) % count; Show(index); }

    /// <summary>  Show UI    /// </summary>
    private void Show(int idx)
    {
        if (carousel.Count == 0) return;
        idx = Mathf.Clamp(idx, 0, carousel.Count - 1);

        index = idx;
        currentEntry = carousel[index];
        var entry = currentEntry;
        var item = entry.item;

        if (icon != null) icon.sprite = item?.icon;
        if (levelText != null) levelText.text = $"+ {entry.level}";
        if (tierText != null)
        {
            int tier = InventoryManager.Instance.GetTier(entry);
            tierText.text = InventoryManager.Instance.GetTierStepText(tier);
        }
        if (itemName != null) itemName.text = entry.item.itemName;

        PopulateEffectRows(entry);

        int required = 0;
        var inv = InventoryManager.Instance;
        int maxLv = inv.GetMaxLevel(entry);

        if (item != null && item.upgradeLevels != null && entry.level < maxLv)
        {
            int idxCost = entry.level - 1;
            if (idxCost >= 0 && idxCost < item.upgradeLevels.Count) required = Mathf.Max(0, item.upgradeLevels[idxCost].cost);
        }

        if (quantityFill != null) quantityFill.fillAmount = (required > 0 && entry.level < maxLv) ? Mathf.Clamp01((float)entry.count / required) : 1f;
        if (quantityFillText != null) quantityFillText.text = (required > 0 && entry.level < maxLv) ? $"{entry.count}/{required}" : "-";
        if (levelUpButton != null) levelUpButton.interactable = (required > 0 && entry.count >= required && entry.level < maxLv);
    }

    /// <summary> SetUp Equip, Own Effect Text    /// </summary>
    private void PopulateEffectRows(InventoryManager.InventoryEntry entry)
    {

        if (equipRows != null) foreach (var row in equipRows) if (row != null) row.gameObject.SetActive(false);
        if (ownedRows != null) foreach (var row in ownedRows) if (row != null) row.gameObject.SetActive(false);

        var item = entry.item;
        var levels = item.upgradeLevels;
        if (levels == null || levels.Count == 0) return;

        var equipTypes = new List<UpgradeType>(4);
        var ownedTypes = new List<UpgradeType>(8);
        var equipLabel = new Dictionary<UpgradeType, string>();
        var ownedLabel = new Dictionary<UpgradeType, string>();

        foreach (var lvl in levels)
        {
            if (lvl?.equipAdd != null)
            {
                foreach (var s in lvl.equipAdd)
                {
                    if (!equipTypes.Contains(s.type)) equipTypes.Add(s.type);
                    if (!equipLabel.ContainsKey(s.type) && !string.IsNullOrEmpty(s.label))
                        equipLabel[s.type] = s.label;
                }
            }
            if (lvl?.ownedAdd != null)
            {
                foreach (var s in lvl.ownedAdd)
                {
                    if (!ownedTypes.Contains(s.type)) ownedTypes.Add(s.type);
                    if (!ownedLabel.ContainsKey(s.type) && !string.IsNullOrEmpty(s.label))
                        ownedLabel[s.type] = s.label;
                }
            }
        }

        var inv = InventoryManager.Instance;

        int equipShown = 0;
        foreach (var t in equipTypes)
        {
            if (equipShown >= (equipRows?.Length ?? 0)) break;
            var row = equipRows[equipShown];
            if (row == null) continue;

            string label = equipLabel.TryGetValue(t, out var l) && !string.IsNullOrEmpty(l) ? l : GetDefaultLabel(t);
            row.Set(label, FormatValue(t, inv.GetEquipTotal(entry, t), inv.GetEquipNextTotal(entry, t), false));
            row.gameObject.SetActive(true);
            equipShown++;
        }

        int ownedShown = 0;
        foreach (var t in ownedTypes)
        {
            if (ownedShown >= (ownedRows?.Length ?? 0)) break;
            var row = ownedRows[ownedShown];
            if (row == null) continue;

            string label = ownedLabel.TryGetValue(t, out var l) && !string.IsNullOrEmpty(l) ? l : GetDefaultLabel(t);
            row.Set(label, FormatValue(t, inv.GetOwnedTotalForItem(entry, t), inv.GetOwnedNextTotalForItem(entry, t), true));
            row.gameObject.SetActive(true);
            ownedShown++;
        }
    }

    /// <summary> Upgrade Type Text Show </summary>
    private static string GetDefaultLabel(UpgradeType t)
    {
        return t switch
        {
            UpgradeType.DMGIncrease => "공격력 증가",
            UpgradeType.CritIncrease => "치명타 확률",
            UpgradeType.CritDamage => "치명타 데미지",
            UpgradeType.GoldDropIncrease => "골드 드랍률 증가",
            UpgradeType.HealthRegen => "체력 재생력",
            UpgradeType.HealthIncrease => "체력 재생",
            _ => t.ToString()
        };
    }

    /// <summary> Upgrade Incrase Text format </summary>
    private static string FormatValue(UpgradeType t, float now, float next, bool isOwned)
    {
        bool asPercent = (t == UpgradeType.CritIncrease || t == UpgradeType.CritDamage) || isOwned;
        return asPercent
            ? $"{now:0.##}%  →  {next:0.##}%"
            : $"{now:0.##}  →  {next:0.##}";
    }

    /// <summary>  Level UP Item    /// </summary>
    private void OnClickLevelUp()
    {
        if (currentEntry == null) return;
        var invMgr = InventoryManager.Instance;
        if (invMgr == null) return;
        invMgr.LevelUpMultiple(currentEntry, 1);
        Show(index);
    }

    /// <summary>  Refresh Current PopUp Item     /// </summary>
    public void RefreshCurrent()
    {
        if (root == null || !root.activeSelf) return;

        if (currentEntry == null)
        {
            if (carousel.Count == 0) { CloseUI(); return; }

            index = Mathf.Clamp(index, 0, carousel.Count - 1);
            currentEntry = carousel[index];
        }

        if (!IsEntryStillVisible(currentEntry))
        {
            BuildCarousel(currentEntry);

            if (carousel.Count == 0) { CloseUI(); return; }

            index = Mathf.Clamp(index, 0, carousel.Count - 1);
            currentEntry = carousel[index];
            Show(index);
            return;
        }

        int found = FindIndexOf(currentEntry.item);
        if (found >= 0) index = found;

        Show(index);
    }

    /// <summary> Check if Entry can be shown /// </summary>
    private static bool IsEntryStillVisible(InventoryManager.InventoryEntry e)
    {
        if (e == null || e.item == null) return false;
        return !(e.count <= 0 && e.level <= 1);
    }

    /// <summary> Find item from Carousel /// </summary>
    private int FindIndexOf(ShopItem_BaseSObj item)
    {
        if (item == null) return -1;
        for (int i = 0; i < carousel.Count; i++)
            if (carousel[i].item == item) return i;
        return -1;
    }
}
