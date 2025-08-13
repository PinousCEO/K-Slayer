using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Skill Item")]
public class Skill_SObj : ShopItem_BaseSObj
{
    [Header("Skill Only")]
    public ElementType element = ElementType.Normal;
    [TextArea(1, 3)] public string skillShortDesc;
    [TextArea(1, 3)] public string attackPatternText;
    public int requiredHits = 0;
    public int hpCost = 0;
    public int resourceCostPerUse = 0;
    public bool autoToggleDefault = true;

    public int skillAttack;

    private void OnValidate() { category = ItemCategory.Skill; }
}