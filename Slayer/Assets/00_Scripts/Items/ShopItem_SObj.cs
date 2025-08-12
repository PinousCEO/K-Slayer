using System.Collections.Generic;
using UnityEngine;

public enum ItemCategory { Weapon, Accessory, Skill }
public enum Rarity { Common, Uncommon, Rare, Epic, Elite, Unique, Legendary, Mythic }
public enum UpgradeType { DMGIncrease, CritIncrease, CritDamage }

[System.Serializable]
public class UpgradeStat
{
    public string label;
    public UpgradeType type;
    public float value;
}

[System.Serializable]
public class UpgradeLevel
{
    public int cost;
    public List<UpgradeStat> equipAdd = new();
    public List<UpgradeStat> ownedAdd = new();
}

[CreateAssetMenu(menuName = "Shop/ShopItem")]
public class ShopItem_SObj : ScriptableObject
{
    [Header("Basic")]
    public string itemName;
    public Rarity rarity;
    public Sprite icon;
    public ItemCategory category;

    [Header("Combat & Upgrade")]
    public float baseAttack = 0f;
    public List<UpgradeLevel> upgradeLevels = new(5);
}
