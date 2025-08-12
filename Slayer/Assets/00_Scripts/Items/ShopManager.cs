using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ShopManager : MonoBehaviour
{
    [Header("DB / UI 참조")]
    [SerializeField] private List<ShopItem_SObj> allItems;
    [SerializeField] private ShopResultUI resultUI;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button pull1Button;
    [SerializeField] private Button pull11Button;
    [SerializeField] private Button pull35Button;

    private (Rarity rarity, float weight)[] rarityWeights =
    {
        (Rarity.Common,    40f),
        (Rarity.Uncommon,  25f),
        (Rarity.Rare,      15f),
        (Rarity.Epic,      10f),
        (Rarity.Unique,     6f),
        (Rarity.Legendary,  4f),
    };

    private float totalWeight;
    private float[] cumulative;
    private Rarity[] rarityOrder;

    private Dictionary<ItemCategory, Dictionary<Rarity, List<ShopItem_SObj>>> itemMap;
    private ItemCategory activeCategory = ItemCategory.Weapon;

    private void Awake()
    {
        BuildItemMap();
        BuildRarityPicker();

        if (resultUI != null)
        {
            resultUI.SetClickInteraction(true);
            resultUI.OnAllShown += () => ToggleResultButtons(true);
            resultUI.OnClosed += () => ToggleResultButtons(false);
            resultUI.gameObject.SetActive(false);
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnClickConfirm);
        }
        if (pull1Button != null)
        {
            pull1Button.onClick.RemoveAllListeners();
            pull1Button.onClick.AddListener(() => OnClickPull(1));
        }
        if (pull11Button != null)
        {
            pull11Button.onClick.RemoveAllListeners();
            pull11Button.onClick.AddListener(() => OnClickPull(11));
        }
        if (pull35Button != null)
        {
            pull35Button.onClick.RemoveAllListeners();
            pull35Button.onClick.AddListener(() => OnClickPull(35));
        }

        ToggleResultButtons(false);
    }

    private void BuildItemMap()
    {
        itemMap = new Dictionary<ItemCategory, Dictionary<Rarity, List<ShopItem_SObj>>>(3);
        var cats = (ItemCategory[])System.Enum.GetValues(typeof(ItemCategory));
        var rars = (Rarity[])System.Enum.GetValues(typeof(Rarity));

        for (int ci = 0; ci < cats.Length; ci++)
        {
            var cat = cats[ci];
            var inner = new Dictionary<Rarity, List<ShopItem_SObj>>(rars.Length);
            for (int ri = 0; ri < rars.Length; ri++)
                inner[rars[ri]] = new List<ShopItem_SObj>(8);
            itemMap[cat] = inner;
        }

        if (allItems == null) return;

        for (int i = 0; i < allItems.Count; i++)
        {
            var it = allItems[i];
            if (it == null) continue;
            itemMap[it.category][it.rarity].Add(it);
        }
    }

    private void BuildRarityPicker()
    {
        int n = rarityWeights.Length;
        cumulative = new float[n];
        rarityOrder = new Rarity[n];

        totalWeight = 0f;
        for (int i = 0; i < n; i++)
        {
            totalWeight += Mathf.Max(0f, rarityWeights[i].weight);
            cumulative[i] = totalWeight;
            rarityOrder[i] = rarityWeights[i].rarity;
        }

        if (totalWeight <= 0f)
        {
            // fallback to equal weights if misconfigured
            totalWeight = n;
            for (int i = 0; i < n; i++)
                cumulative[i] = i + 1;
        }
    }

    public void SelectWeapon() { activeCategory = ItemCategory.Weapon; ToggleResultButtons(resultUI != null && resultUI.IsShowingResult()); }
    public void SelectAccessory() { activeCategory = ItemCategory.Accessory; ToggleResultButtons(resultUI != null && resultUI.IsShowingResult()); }
    public void SelectSkill() { activeCategory = ItemCategory.Skill; ToggleResultButtons(resultUI != null && resultUI.IsShowingResult()); }

    public void _SpawnWeapon(int count = 1) { activeCategory = ItemCategory.Weapon; Pull(activeCategory, count); }
    public void _SpawnAccessory(int count = 1) { activeCategory = ItemCategory.Accessory; Pull(activeCategory, count); }
    public void _SpawnSkills(int count = 1) { activeCategory = ItemCategory.Skill; Pull(activeCategory, count); }

    private void OnClickConfirm()
    {
        if (resultUI != null && resultUI.IsShowingResult())
            resultUI.ConfirmClose();
    }

    private void OnClickPull(int count)
    {
        StartCoroutine(Co_RePullAfterClose(activeCategory, count));
    }

    private IEnumerator Co_RePullAfterClose(ItemCategory category, int count)
    {
        if (resultUI != null && resultUI.IsShowingResult())
        {
            ToggleResultButtons(false);
            resultUI.SkipAnimation();
            yield return null;
            Pull(category, count);
            yield break;
        }
        Pull(category, count);
    }

    private void ToggleResultButtons(bool on)
    {
        if (confirmButton != null) confirmButton.gameObject.SetActive(on);
        if (pull1Button != null) pull1Button.gameObject.SetActive(on);

        bool show11 = on && activeCategory == ItemCategory.Skill;
        bool show35 = on && (activeCategory == ItemCategory.Weapon || activeCategory == ItemCategory.Accessory);

        if (pull11Button != null) pull11Button.gameObject.SetActive(show11);
        if (pull35Button != null) pull35Button.gameObject.SetActive(show35);
    }

    private void Pull(ItemCategory category, int count)
    {
        if (count <= 0) return;

        ToggleResultButtons(false);

        var pulled = new List<ShopItem_SObj>(count);
        for (int i = 0; i < count; i++)
        {
            var targetRarity = GetRandomRarityWeighted();
            var pick = GetRandomItemFrom(category, targetRarity);
            if (pick == null) pick = FallbackPick(category, targetRarity);
            if (pick != null) pulled.Add(pick);
        }

        var inv = InventoryManager.Instance;
        if (inv != null)
        {
            for (int i = 0; i < pulled.Count; i++)
                inv.AddItem(pulled[i]);
        }

        if (resultUI != null)
        {
            resultUI.gameObject.SetActive(true);
            resultUI.ShowResult(pulled);
        }
    }

    private Rarity GetRandomRarityWeighted()
    {
        float r = Random.value * totalWeight;
        for (int i = 0; i < cumulative.Length; i++)
        {
            if (r <= cumulative[i]) return rarityOrder[i];
        }
        return rarityOrder[0];
    }

    private ShopItem_SObj GetRandomItemFrom(ItemCategory cat, Rarity rarity)
    {
        if (!itemMap.TryGetValue(cat, out var byRarity)) return null;
        if (!byRarity.TryGetValue(rarity, out var list) || list == null || list.Count == 0) return null;
        int idx = Random.Range(0, list.Count);
        return list[idx];
    }

    private ShopItem_SObj FallbackPick(ItemCategory cat, Rarity prefer)
    {
        int idx = -1;
        for (int i = 0; i < rarityOrder.Length; i++)
            if (rarityOrder[i] == prefer) { idx = i; break; }
        if (idx < 0) idx = 0;

        for (int i = idx; i >= 0; i--)
        {
            var p = GetRandomItemFrom(cat, rarityOrder[i]);
            if (p != null) return p;
        }

        for (int i = idx + 1; i < rarityOrder.Length; i++)
        {
            var p = GetRandomItemFrom(cat, rarityOrder[i]);
            if (p != null) return p;
        }
        return null;
    }
}
