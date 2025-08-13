using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillPopup : MonoBehaviour
{
    [Header("Root/Toggle")]
    [Tooltip("팝업 루트 오브젝트")][SerializeField] private GameObject root;
    [Tooltip("AUTO 사용 토글(스킬 전용)")][SerializeField] private Toggle autoToggle;

    [Header("Top")]
    [Tooltip("스킬 아이콘")][SerializeField] private Image icon;
    [Header("Cost UI")]
    [SerializeField] private Image quantityFill;
    [SerializeField] private TMP_Text quantityFillText;
    [Tooltip("레벨 텍스트")][SerializeField] private TMP_Text levelText;
    [Tooltip("제목(속성 + 스킬명)")][SerializeField] private TMP_Text titleText;
    [Tooltip("간단 설명")][SerializeField] private TMP_Text shortDesc;

    [Header("Middle")]
    [Tooltip("공격/발동 패턴 설명")][SerializeField] private TMP_Text patternText;
    [Tooltip("배율/증가율 라인 (이전 → 다음 레벨)")]
    [SerializeField] private TMP_Text multiplierText;

    [Header("Stats")]
    [Tooltip("필요 공격수")][SerializeField] private TMP_Text needHitsText;
    [Tooltip("HP 소모")][SerializeField] private TMP_Text hpCostText;
    [Tooltip("재화 소모(사용당)")][SerializeField] private TMP_Text resourceCostText;

    [Header("Cost & Buttons")]
    [Tooltip("레벨업 버튼")][SerializeField] private Button levelUpButton;
    [Tooltip("닫기 버튼")][SerializeField] private Button closeButton;

    private readonly List<InventoryManager.InventoryEntry> carousel = new();
    private int index;
    private InventoryManager.InventoryEntry current;

    private void Awake()
    {
        if (root) root.SetActive(false);
        if (levelUpButton)
        {
            levelUpButton.onClick.RemoveAllListeners();
            levelUpButton.onClick.AddListener(OnClickLevelUp);
        }
        if (closeButton)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => root.SetActive(false));
        }
    }

    public void OpenUI(InventoryManager.InventoryEntry focus)
    {
        BuildCarousel(focus);
        if (!root.activeSelf) root.SetActive(true);
        Show(index);
    }

    public void RefreshCurrent()
    {
        if (root == null || !root.activeSelf) return;

        if (current == null)
        {
            if (carousel.Count == 0) { root.SetActive(false); return; }
            index = Mathf.Clamp(index, 0, carousel.Count - 1);
            current = carousel[index];
        }

        if (!IsEntryStillVisible(current))
        {
            BuildCarousel(current);
            if (carousel.Count == 0) { root.SetActive(false); return; }

            index = Mathf.Clamp(index, 0, carousel.Count - 1);
            current = carousel[index];
            Show(index);
            return;
        }

        int found = FindIndexOf(current.item);
        if (found >= 0) index = found;

        Show(index);
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

    private void BuildCarousel(InventoryManager.InventoryEntry focus)
    {
        carousel.Clear();
        if (focus == null || focus.item == null) { index = 0; return; }

        var inv = InventoryManager.Instance?.GetInventory();
        if (inv != null)
        {
            foreach (var e in inv.Values)
            {
                if (e?.item == null) continue;
                if (e.item.category != ItemCategory.Skill) continue;
                if (e.count <= 0 && e.level <= 1) continue;
                carousel.Add(e);
            }
            carousel.Sort((a, b) => string.Compare(a.item.itemName, b.item.itemName, System.StringComparison.Ordinal));
        }

        int found = -1;
        for (int i = 0; i < carousel.Count; i++)
            if (carousel[i].item == focus.item) { found = i; break; }

        if (found >= 0) index = found;
        else { carousel.Add(focus); index = carousel.Count - 1; }
    }

    private void Show(int idx)
    {
        if (carousel.Count == 0) return;
        index = Mathf.Clamp(idx, 0, carousel.Count - 1);
        current = carousel[index];

        var item = current.item;
        var s = item as Skill_SObj;

        if (autoToggle) autoToggle.isOn = s != null ? s.autoToggleDefault : true;

        if (titleText) titleText.text = $"[{(s != null ? s.element.ToString() : "NoElem")}] {item.itemName}";
        if (shortDesc) shortDesc.text = s != null ? (s.skillShortDesc ?? "") : item.itemDescription ?? "";
        if (icon) icon.sprite = item.icon;

        if (levelText) levelText.text = $"Lv.{current.level}";

        if (patternText) patternText.text = s != null ? (s.attackPatternText ?? "") : "";

        if (multiplierText) multiplierText.text = BuildMultiplierLine(item, current);

        if (needHitsText) needHitsText.text = s != null ? s.requiredHits.ToString() : "0";
        if (hpCostText) hpCostText.text = s != null ? s.hpCost.ToString() : "0";
        if (resourceCostText) resourceCostText.text = s != null ? s.resourceCostPerUse.ToString() : "0";

        int need = InventoryManager.Instance.GetNextLevelCost(current);
        int have = current.count;
        int maxLv = InventoryManager.Instance.GetMaxLevel(item);

        if (quantityFill != null)
        {
            quantityFill.fillAmount = (need > 0 && current.level < maxLv)
                ? Mathf.Clamp01((float)have / need)
                : 1f;
        }
        if (quantityFillText != null)
        {
            quantityFillText.text = (need > 0 && current.level < maxLv)
                ? $"{have}/{need}"
                : "-";
        }

        // 버튼 활성화 여부
        if (levelUpButton != null)
        {
            levelUpButton.interactable = (need > 0 && have >= need && current.level < maxLv);
        }
    }

    // 이전 → 다음 레벨 배율 라인
    private string BuildMultiplierLine(ShopItem_BaseSObj item, InventoryManager.InventoryEntry e)
    {
        int maxLv = InventoryManager.Instance.GetMaxLevel(item);
        float now = GetTotalDMGPercent(item, e.level);

        if (e.level >= maxLv)
            return $"공격력의 {now:0.#}% (MAX)";

        float next = GetTotalDMGPercent(item, e.level + 1);
        return $"공격력의 {now:0.#}%  →  {next:0.#}%";
    }

    private float GetTotalDMGPercent(ShopItem_BaseSObj item, int level)
    {
        float basePct = 0f;
        if (item is Skill_SObj s) basePct = Mathf.Max(0, s.skillAttack);
        else basePct = Mathf.Max(0, item.baseAttack);

        float addPct = 0f;
        int last = Mathf.Min(level, item.upgradeLevels?.Count ?? 0);
        for (int i = 0; i < last; i++)
        {
            var lv = item.upgradeLevels[i];
            if (lv == null) continue;

            if (lv.equipAdd != null)
                for (int j = 0; j < lv.equipAdd.Count; j++)
                    if (lv.equipAdd[j].type == UpgradeType.DMGIncrease) addPct += lv.equipAdd[j].value;

            if (lv.ownedAdd != null)
                for (int j = 0; j < lv.ownedAdd.Count; j++)
                    if (lv.ownedAdd[j].type == UpgradeType.DMGIncrease) addPct += lv.ownedAdd[j].value;
        }
        return basePct + addPct;
    }

    private void OnClickLevelUp()
    {
        if (current == null) return;
        InventoryManager.Instance.LevelUpMultiple(current, 1);
        Show(index);
    }

    private static bool IsEntryStillVisible(InventoryManager.InventoryEntry e)
    {
        if (e == null || e.item == null) return false;
        return !(e.count <= 0 && e.level <= 1);
    }

    private int FindIndexOf(ShopItem_BaseSObj item)
    {
        if (item == null) return -1;
        for (int i = 0; i < carousel.Count; i++)
            if (carousel[i].item == item) return i;
        return -1;
    }
}
