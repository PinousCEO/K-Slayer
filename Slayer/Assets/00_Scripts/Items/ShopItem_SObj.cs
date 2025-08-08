using UnityEngine;

public enum ItemCategory { Weapon, Accessory, Skill }
public enum Rarity { Common, Uncommon, Rare, Epic, Elite, Unique, Legendary }

[CreateAssetMenu(menuName = "Shop/ShopItem")]
public class ShopItem_SObj : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public ItemCategory category;
    public Rarity rarity;
}