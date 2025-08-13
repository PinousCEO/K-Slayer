using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Accessory Item")]
public class Accessory_SObj : ShopItem_BaseSObj
{
    [Header("Accessory Only")]
    public Rarity rarity = Rarity.Common;
    public int defence;

    private void OnValidate() { category = ItemCategory.Accessory; }
}