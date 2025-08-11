using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryPopup : MonoBehaviour
{
    public static InventoryPopup Instance;

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

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        root.SetActive(false);

        levelUpButton.onClick.RemoveAllListeners();
        levelUpButton.onClick.AddListener(OnClickLevelUp);

        closeBtn.onClick.RemoveAllListeners();
        closeBtn.onClick.AddListener(Close);

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

    public void Open(InventoryManager.InventoryEntry entry)
    {
        BuildCarousel(entry);
        root.SetActive(true);
        Show(index);
    }

    public void RefreshCurrent()
    {
        if (!root.activeSelf || currentEntry == null) return;
        Show(index);
    }

    public void Close()
    {
        root.SetActive(false);
    }

    private void BuildCarousel(InventoryManager.InventoryEntry focus)
    {
        carousel.Clear();
        var inv = InventoryManager.Instance != null ? InventoryManager.Instance.GetInventory() : null;
        if (inv != null)
        {
            var list = inv.Values
                .Where(e => e != null && e.item != null &&
                            e.item.category == focus.item.category &&
                            (e.count > 0 || e.level > 1))
                .OrderBy(e => (int)e.item.rarity)
                .ThenBy(e => e.item.itemName)
                .ToList();
            carousel.AddRange(list);
        }

        index = Mathf.Max(0, carousel.FindIndex(e => e.item == focus.item));
        if (index < 0)
        {
            carousel.Add(focus);
            index = carousel.Count - 1;
        }
    }

    private void Prev()
    {
        if (carousel.Count == 0) return;
        index = (index - 1 + carousel.Count) % carousel.Count;
        Show(index);
    }

    private void Next()
    {
        if (carousel.Count == 0) return;
        index = (index + 1) % carousel.Count;
        Show(index);
    }

    private void Show(int idx)
    {
        if (carousel.Count == 0) return;
        index = Mathf.Clamp(idx, 0, carousel.Count - 1);
        currentEntry = carousel[index];

        if (icon != null) icon.sprite = currentEntry.item.icon;
        if (levelText != null) levelText.text = $"LV : {currentEntry.level}";
        if (countText != null) countText.text = $"{currentEntry.count}";

        float curAtk = InventoryManager.Instance.GetAttack(currentEntry);
        float nextAtk = curAtk;
        int required = 0;

        var levels = currentEntry.item.upgradeLevels;
        int maxLv = InventoryManager.Instance.GetMaxLevel(currentEntry);
        int curLv = currentEntry.level;

        if (curLv < maxLv)
        {
            int idxCost = curLv - 1;
            if (levels != null && idxCost >= 0 && idxCost < levels.Count)
            {
                required = Mathf.Max(0, levels[idxCost].cost);
                nextAtk += levels[idxCost].attackAdd;
            }
        }

        if (atkText != null) atkText.text = $"ATK  {curAtk:0.##}  →  {nextAtk:0.##}";
        if (requirementText != null)
        {
            if (required > 0 && curLv < maxLv) requirementText.text = $"Need: {required}";
            else requirementText.text = "MAX";
        }

        if (quantityFill != null)
        {
            if (required > 0 && curLv < maxLv)
                quantityFill.fillAmount = Mathf.Clamp01((float)currentEntry.count / required);
            else
                quantityFill.fillAmount = 1f;
        }

        bool can = (required > 0) && (currentEntry.count >= required) && (currentEntry.level < maxLv);
        levelUpButton.interactable = can;

        if (prevButton != null) prevButton.gameObject.SetActive(carousel.Count > 1);
        if (nextButton != null) nextButton.gameObject.SetActive(carousel.Count > 1);
    }

    private void OnClickLevelUp()
    {
        if (currentEntry == null) return;
        InventoryManager.Instance.LevelUpMultiple(currentEntry, selectedSteps);
        Show(index);
    }
}
