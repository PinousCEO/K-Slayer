using System.Collections.Generic;
using UnityEngine;

public enum ItemCategory { Weapon, Accessory, Skill }
public enum Rarity { Common, Uncommon, Rare, Epic, Elite, Unique, Legendary }

[System.Serializable]
public class UpgradeLevel
{
    public int cost;
    public float attackAdd;
}

[CreateAssetMenu(menuName = "Shop/ShopItem")]
public class ShopItem_SObj : ScriptableObject
{
    [Header("Basic")]
    public string itemName;
    public Sprite icon;
    public ItemCategory category;
    public Rarity rarity;

    [Header("Combat & Upgrade (내장)")]
    public float baseAttack = 0f;
    public List<UpgradeLevel> upgradeLevels = new List<UpgradeLevel>(5);
}
