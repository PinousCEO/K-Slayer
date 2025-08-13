using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Weapon Item")]
public class Weapon_SObj : ShopItem_BaseSObj
{
    [Header("Weapon Only")]
    public Rarity rarity = Rarity.Common;
    public int attack;

    private void OnValidate() { category = ItemCategory.Weapon; }
}