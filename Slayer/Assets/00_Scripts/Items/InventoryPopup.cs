using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class InventoryPopup : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private TMP_Text tierText;

    [SerializeField] private TMP_Text itemName;
    [SerializeField] private TMP_Text itemRank;

    [Header("Effect Rows (pre-instantiated)")]
    [SerializeField] private PopupEffectRow[] equipRows;  //1
    [SerializeField] private PopupEffectRow[] ownedRows; //3

    [Header("Cost UI")]
    [SerializeField] private Image quantityFill;
    [SerializeField] private TMP_Text quantityFillText;

    [Header("Buttons")]
    [SerializeField] private Button levelUpButton;
    [SerializeField] private Button closeBtn;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;

    private InventoryManager.InventoryEntry currentEntry;
    private readonly List<InventoryManager.InventoryEntry> carousel = new();
    private int index;
    private const int selectedSteps = 1;

    private static readonly UpgradeType[] PreferredOrder = new[]
    {
        UpgradeType.DMGIncrease, UpgradeType.CritIncrease, UpgradeType.CritDamage
    };

    private void Awake()
    {
        if (root != null) root.SetActive(false);

        if (levelUpButton != null)
        {
            levelUpButton.onClick.RemoveAllListeners();
            levelUpButton.onClick.AddListener(OnClickLevelUp);
        }
        if (closeBtn != null)
        {
            closeBtn.onClick.RemoveAllListeners();
            closeBtn.onClick.AddListener(CloseUI);
        }
        if (prevButton != null)
        {
            prevButton.onClick.RemoveAllListeners();
            prevButton.onClick.AddListener(Prev);
        }
        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(Next);
        }
    }

    public void OpenUI(InventoryManager.InventoryEntry entry)
    {
        if (entry == null || entry.item == null || root == null) return;
        BuildCarousel(entry);
        if (!root.activeSelf) root.SetActive(true);
        Show(index);
    }

    public void RefreshCurrent()
    {
        if (root == null || !root.activeSelf || currentEntry == null) return;
        Show(index);
    }

    public void CloseUI()
    {
        if (root == null) return;
        if (root.activeSelf) root.SetActive(false);
    }

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
        if (found >= 0)
        {
            index = found;
        }
        else
        {
            carousel.Add(focus);
            index = carousel.Count - 1;
        }
    }

    private void Prev()
    {
        int count = carousel.Count;
        if (count == 0) return;
        index = (index - 1 + count) % count;
        Show(index);
    }

    private void Next()
    {
        int count = carousel.Count;
        if (count == 0) return;
        index = (index + 1) % count;
        Show(index);
    }

    private void Show(int idx)
    {
        if (carousel.Count == 0) return;
        if (idx < 0) idx = 0; else if (idx >= carousel.Count) idx = carousel.Count - 1;

        index = idx;
        currentEntry = carousel[index];
        var entry = currentEntry;
        var item = entry.item;

        if (icon != null) icon.sprite = item != null ? item.icon : null;
        if (levelText != null) levelText.text = $"+ {entry.level}";
        if (countText != null) countText.text = entry.count.ToString();
        if (tierText != null)
        {
            int tier = InventoryManager.Instance.GetTier(entry);
            tierText.text = InventoryManager.Instance.GetTierStepText(tier);
        }
        if (itemName != null) itemName.text = entry.item.itemName.ToString();
        if (itemRank != null) itemRank.text = entry.item.rarity.ToString();

        PopulateEffectRows(entry);

        int required = 0;
        var inv = InventoryManager.Instance;
        int maxLv = inv.GetMaxLevel(entry);
        if (item != null && item.upgradeLevels != null && entry.level < maxLv)
        {
            int idxCost = entry.level - 1;
            if (idxCost >= 0 && idxCost < item.upgradeLevels.Count)
                required = Mathf.Max(0, item.upgradeLevels[idxCost].cost);
        }

        if (quantityFill != null)
        {
            if (required > 0 && entry.level < maxLv)
                quantityFill.fillAmount = Mathf.Clamp01((float)entry.count / required);
            else
                quantityFill.fillAmount = 1f;
        }
        if (quantityFillText != null)
            quantityFillText.text = (required > 0 && entry.level < maxLv) ? $"{entry.count}/{required}" : "-";

        bool can = (required > 0) && (entry.count >= required) && (entry.level < maxLv);
        if (levelUpButton != null) levelUpButton.interactable = can;
    }

    private void PopulateEffectRows(InventoryManager.InventoryEntry entry)
    {
        if (equipRows != null)
            for (int i = 0; i < equipRows.Length; i++)
                if (equipRows[i] != null) equipRows[i].gameObject.SetActive(false);
        if (ownedRows != null)
            for (int i = 0; i < ownedRows.Length; i++)
                if (ownedRows[i] != null) ownedRows[i].gameObject.SetActive(false);

        var item = entry.item;
        var levels = item.upgradeLevels;
        if (levels == null || levels.Count == 0) return;

        var equipTypes = new List<UpgradeType>(4);
        var ownedTypes = new List<UpgradeType>(8);
        var equipLabel = new Dictionary<UpgradeType, string>();
        var ownedLabel = new Dictionary<UpgradeType, string>();

        for (int i = 0; i < levels.Count; i++)
        {
            var lvl = levels[i];
            if (lvl != null && lvl.equipAdd != null)
            {
                for (int j = 0; j < lvl.equipAdd.Count; j++)
                {
                    var s = lvl.equipAdd[j];
                    if (!equipTypes.Contains(s.type)) equipTypes.Add(s.type);
                    if (!equipLabel.ContainsKey(s.type) && !string.IsNullOrEmpty(s.label))
                        equipLabel[s.type] = s.label;
                }
            }
            if (lvl != null && lvl.ownedAdd != null)
            {
                for (int j = 0; j < lvl.ownedAdd.Count; j++)
                {
                    var s = lvl.ownedAdd[j];
                    if (!ownedTypes.Contains(s.type)) ownedTypes.Add(s.type);
                    if (!ownedLabel.ContainsKey(s.type) && !string.IsNullOrEmpty(s.label))
                        ownedLabel[s.type] = s.label;
                }
            }
        }

        SortTypesByPreference(equipTypes);
        SortTypesByPreference(ownedTypes);

        var inv = InventoryManager.Instance;

        int equipShown = 0;
        for (int i = 0; i < equipTypes.Count && equipShown < (equipRows?.Length ?? 0); i++)
        {
            var t = equipTypes[i];
            var row = equipRows[equipShown];
            if (row == null) continue;

            string label = equipLabel.TryGetValue(t, out var l) && !string.IsNullOrEmpty(l) ? l : GetDefaultLabel(t);
            float now = inv.GetEquipTotal(entry, t);
            float next = inv.GetEquipNextTotal(entry, t);
            row.Set(label, FormatValue(t, now, next, false));
            row.gameObject.SetActive(true);
            equipShown++;
        }

        int ownedShown = 0;
        for (int i = 0; i < ownedTypes.Count && ownedShown < (ownedRows?.Length ?? 0); i++)
        {
            var t = ownedTypes[i];
            var row = ownedRows[ownedShown];
            if (row == null) continue;

            string label = ownedLabel.TryGetValue(t, out var l) && !string.IsNullOrEmpty(l) ? l : GetDefaultLabel(t);
            float now = inv.GetOwnedTotalForItem(entry, t);
            float next = inv.GetOwnedNextTotalForItem(entry, t);
            row.Set(label, FormatValue(t, now, next, true));
            row.gameObject.SetActive(true);
            ownedShown++;
        }
    }

    private static void SortTypesByPreference(List<UpgradeType> list)
    {
        if (list == null || list.Count <= 1) return;
        int insertPos = 0;
        for (int p = 0; p < PreferredOrder.Length; p++)
        {
            var pref = PreferredOrder[p];
            for (int i = insertPos; i < list.Count; i++)
            {
                if (list[i] == pref)
                {
                    var tmp = list[i];
                    list.RemoveAt(i);
                    list.Insert(insertPos, tmp);
                    insertPos++;
                }
            }
        }
    }

    private static string GetDefaultLabel(UpgradeType t)
    {
        switch (t)
        {
            case UpgradeType.DMGIncrease: return "공격력 증가";
            case UpgradeType.CritIncrease: return "치명타 확률";
            case UpgradeType.CritDamage: return "치명타 데미지";
            default: return t.ToString();
        }
    }

    private static string FormatValue(UpgradeType t, float now, float next, bool isOwned)
    {
        bool asPercent = (t == UpgradeType.CritIncrease || t == UpgradeType.CritDamage) || isOwned;
        if (asPercent) return $"{now:0.##}%  →  {next:0.##}%";
        return $"{now:0.##}  →  {next:0.##}";
    }

    private void OnClickLevelUp()
    {
        if (currentEntry == null) return;
        var invMgr = InventoryManager.Instance;
        if (invMgr == null) return;
        invMgr.LevelUpMultiple(currentEntry, selectedSteps);
        Show(index);
    }
}
