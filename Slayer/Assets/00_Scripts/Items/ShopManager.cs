using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
    [Header("DB / UI 참조")]
    [SerializeField] private List<ShopItem_SObj> allItems;   // all item list
    [SerializeField] private ShopResultUI resultUI;           // result ui
    [SerializeField] private Button confirmButton;            // confirm
    [SerializeField] private Button pull1Button;              // 1 btn
    [SerializeField] private Button pull11Button;             // 11 btn (스킬)
    [SerializeField] private Button pull35Button;             // 35 btn (무기/악세)

    private (Rarity rarity, float weight)[] rarityWeights =
    {
        (Rarity.Common,    40f),
        (Rarity.Uncommon,  25f),
        (Rarity.Rare,      15f),
        (Rarity.Epic,      10f),
        (Rarity.Unique,     6f),
        (Rarity.Legendary,  4f),
    };

    private Dictionary<ItemCategory, Dictionary<Rarity, List<ShopItem_SObj>>> itemMap;
    private ItemCategory activeCategory = ItemCategory.Weapon;

    private void Awake()
    {
        BuildItemMap();

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
        itemMap = new();
        foreach (ItemCategory cat in System.Enum.GetValues(typeof(ItemCategory)))
            itemMap[cat] = new Dictionary<Rarity, List<ShopItem_SObj>>();

        foreach (var it in allItems.Where(i => i != null))
        {
            if (!itemMap[it.category].TryGetValue(it.rarity, out var list))
            {
                list = new List<ShopItem_SObj>();
                itemMap[it.category][it.rarity] = list;
            }
            list.Add(it);
        }
    }

    public void SelectWeapon() => activeCategory = ItemCategory.Weapon;
    public void SelectAccessory() => activeCategory = ItemCategory.Accessory;
    public void SelectSkill() => activeCategory = ItemCategory.Skill;

    public void _SpawnWeapon(int count = 1) { activeCategory = ItemCategory.Weapon; Pull(activeCategory, count); }
    public void _SpawnAccessory(int count = 1) { activeCategory = ItemCategory.Accessory; Pull(activeCategory, count); }
    public void _SpawnSkills(int count = 1) { activeCategory = ItemCategory.Skill; Pull(activeCategory, count); }

    private void OnClickConfirm() { if (resultUI != null && resultUI.IsShowingResult()) resultUI.ConfirmClose(); }
    private void OnClickPull(int count) { StartCoroutine(Co_RePullAfterClose(activeCategory, count)); }

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

        var pulled = new List<ShopItem_SObj>();
        for (int i = 0; i < count; i++)
        {
            var targetRarity = GetRandomRarityWeighted();
            var pick = GetRandomItemFrom(category, targetRarity) ?? FallbackPick(category, targetRarity);
            if (pick != null) pulled.Add(pick);
        }

        for (int i = 0; i < pulled.Count; i++)
            InventoryManager.Instance.AddItem(pulled[i]);

        if (resultUI != null)
        {
            resultUI.gameObject.SetActive(true);
            resultUI.ShowResult(pulled);
        }
    }

    private Rarity GetRandomRarityWeighted()
    {
        float total = 0f;
        for (int i = 0; i < rarityWeights.Length; i++) total += rarityWeights[i].weight;
        if (total <= 0f) return Rarity.Common;

        float roll = Random.Range(0f, total);
        float acc = 0f;
        for (int i = 0; i < rarityWeights.Length; i++)
        {
            acc += rarityWeights[i].weight;
            if (roll <= acc) return rarityWeights[i].rarity;
        }
        return rarityWeights[0].rarity;
    }

    private ShopItem_SObj GetRandomItemFrom(ItemCategory cat, Rarity rarity)
    {
        if (!itemMap.TryGetValue(cat, out var byRarity)) return null;
        if (!byRarity.TryGetValue(rarity, out var list) || list == null || list.Count == 0) return null;
        return list[Random.Range(0, list.Count)];
    }

    private ShopItem_SObj FallbackPick(ItemCategory cat, Rarity prefer)
    {
        int idx = System.Array.FindIndex(rarityWeights, x => x.rarity == prefer);
        if (idx < 0) idx = 0;

        for (int i = idx; i >= 0; i--)
        {
            var p = GetRandomItemFrom(cat, rarityWeights[i].rarity);
            if (p != null) return p;
        }

        for (int i = idx + 1; i < rarityWeights.Length; i++)
        {
            var p = GetRandomItemFrom(cat, rarityWeights[i].rarity);
            if (p != null) return p;
        }
        return null;
    }
}
