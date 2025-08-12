using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class InventoryPopup : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameObject root;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text countText;

    [Header("Data")]
    [SerializeField] private TMP_Text atkText;
    [SerializeField] private TMP_Text requirementText;

    [SerializeField] private Image quantityFill;

    [SerializeField] private Button levelUpButton;
    [SerializeField] private Button closeBtn;

    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;

    private InventoryManager.InventoryEntry currentEntry;
    private readonly List<InventoryManager.InventoryEntry> carousel = new();
    private int index;
    private const int selectedSteps = 1;

    private void Awake()
    {
        if (root != null) root.SetActive(false);
        SetUpButton();
    }

    public void SetUpButton()
    {
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
        if (invMgr != null)
        {
            var inv = invMgr.GetInventory();
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
                    int r = ((int)a.item.rarity).CompareTo((int)b.item.rarity);
                    if (r != 0) return r;
                    return string.Compare(a.item.itemName, b.item.itemName, System.StringComparison.Ordinal);
                });
            }
        }

        int found = -1;
        for (int i = 0; i < carousel.Count; i++)
        {
            if (carousel[i].item == focus.item) { found = i; break; }
        }

        if (found >= 0) index = found;
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
        int count = carousel.Count;
        if (count == 0) return;

        if (idx < 0) idx = 0;
        else if (idx >= count) idx = count - 1;

        index = idx;
        currentEntry = carousel[index];
        var entry = currentEntry;
        var item = entry.item;

        if (icon != null) icon.sprite = item != null ? item.icon : null;
        if (levelText != null) levelText.text = $"LV : {entry.level}";
        if (countText != null) countText.text = entry.count.ToString();

        var invMgr = InventoryManager.Instance;
        float curAtk = invMgr != null ? invMgr.GetAttack(entry) : 0f;
        float nextAtk = curAtk;
        int required = 0;

        int curLv = entry.level;
        int maxLv = invMgr != null ? invMgr.GetMaxLevel(entry) : 1;

        if (item != null && item.upgradeLevels != null && curLv < maxLv)
        {
            int idxCost = curLv - 1;
            if (idxCost >= 0 && idxCost < item.upgradeLevels.Count)
            {
                var u = item.upgradeLevels[idxCost];
                required = u.cost > 0 ? u.cost : 0;
                nextAtk += u.attackAdd;
            }
        }

        if (atkText != null) atkText.text = $"ATK  {curAtk:0.##}  →  {nextAtk:0.##}";

        if (requirementText != null)
            requirementText.text = (required > 0 && curLv < maxLv) ? $"Need: {required}" : "MAX";

        if (quantityFill != null)
        {
            if (required > 0 && curLv < maxLv)
                quantityFill.fillAmount = Mathf.Clamp01(required > 0 ? (float)entry.count / required : 1f);
            else
                quantityFill.fillAmount = 1f;
        }

        bool can = (required > 0) && (entry.count >= required) && (curLv < maxLv);
        if (levelUpButton != null) levelUpButton.interactable = can;

        bool showNav = count > 1;
        if (prevButton != null) prevButton.gameObject.SetActive(showNav);
        if (nextButton != null) nextButton.gameObject.SetActive(showNav);
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
