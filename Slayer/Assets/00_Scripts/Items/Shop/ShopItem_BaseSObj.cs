using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class UpgradeStat
{
    [Tooltip("표기 Label")] public string label;
    [Tooltip("변동 타입")] public UpgradeType type;
    [Tooltip("증가량")] public float value;
}

[System.Serializable]
public class UpgradeLevel
{
    [Tooltip("업그레이드 비용")] public int cost;
    [Tooltip("장착 능력치")] public List<UpgradeStat> equipAdd = new();
    [Tooltip("보유 능력치")] public List<UpgradeStat> ownedAdd = new();
}

public class ShopItem_BaseSObj : ScriptableObject
{
    [Header("기본 베이스")]
    [Tooltip("아이템 이름")] public string itemName;
    [Tooltip("간단 설명")][TextArea(1, 3)] public string itemDescription;
    [Tooltip("아이템 아이콘")] public Sprite icon;
    [Tooltip("아이템 분류")] public ItemCategory category;

    [Header("업그레이드")]
    [Tooltip("기본 전투 수치")] public float baseAttack = 0f;
    [Tooltip("업그레이드 단계")] public List<UpgradeLevel> upgradeLevels = new(7);
}
