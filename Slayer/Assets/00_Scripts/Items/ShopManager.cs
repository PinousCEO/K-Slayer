using System.Collections.Generic;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    [SerializeField] private List<ShopItem_SObj> allItems;
    [SerializeField] private ShopResultUI resultUI;

    private Dictionary<ItemCategory, Dictionary<Rarity, List<ShopItem_SObj>>> itemMap;

    private Dictionary<Rarity, float> rarityRates = new()
    {
        { Rarity.Common, 40f },
        { Rarity.Uncommon, 25f },
        { Rarity.Rare, 15f },
        { Rarity.Epic, 10f },
        { Rarity.Elite, 5f },
        { Rarity.Unique, 4f },
        { Rarity.Legendary, 1f }
    };

    void Awake()
    {
        BuildItemMap();
    }

    void BuildItemMap()
    {
        itemMap = new();

        foreach (var item in allItems)
        {
            if (!itemMap.ContainsKey(item.category))
                itemMap[item.category] = new();

            if (!itemMap[item.category].ContainsKey(item.rarity))
                itemMap[item.category][item.rarity] = new();

            itemMap[item.category][item.rarity].Add(item);
        }
    }

    public void Pull(ItemCategory category, int count)
    {
        if (resultUI.IsShowingResult()) return;

        List<ShopItem_SObj> pulledItems = new();

        for (int i = 0; i < count; i++)
        {
            Rarity rarity = GetRandomRarity();
            if (itemMap.TryGetValue(category, out var rarityMap))
            {
                if (rarityMap.TryGetValue(rarity, out var items) && items.Count > 0)
                {
                    pulledItems.Add(items[Random.Range(0, items.Count)]);
                }
                else
                {
                    pulledItems.Add(GetFallbackItem(rarityMap));
                }
            }
        }

        foreach (var item in pulledItems)
        {
            InventoryManager.Instance.AddItem(item);
        }

        resultUI.ShowResult(pulledItems);
    }

    Rarity GetRandomRarity()
    {
        float roll = Random.Range(0f, 100f);
        float accum = 0f;

        foreach (var pair in rarityRates)
        {
            accum += pair.Value;
            if (roll <= accum)
                return pair.Key;
        }

        return Rarity.Common;
    }

    ShopItem_SObj GetFallbackItem(Dictionary<Rarity, List<ShopItem_SObj>> rarityMap)
    {
        foreach (Rarity r in System.Enum.GetValues(typeof(Rarity)))
        {
            if (rarityMap.TryGetValue(r, out var items) && items.Count > 0)
                return items[Random.Range(0, items.Count)];
        }

        return null;
    }

    public void _SpawnWeapon(int count = 1) => Pull(ItemCategory.Weapon, count);
    public void _SpawnAccessory(int count = 1) => Pull(ItemCategory.Accessory, count);
    public void _SpawnSkills(int count = 1) => Pull(ItemCategory.Skill, count);
}
