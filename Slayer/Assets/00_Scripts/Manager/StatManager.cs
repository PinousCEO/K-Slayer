using System;
using System.Collections.Generic;
using UnityEngine;

public enum StatType
{
    ATK,
    HP,
    REGEN,
    CRIT_CHANCE,
    CRIT_DAMAGE
}

[System.Serializable]
public class PlayerStatData
{
    public int atkLevel;
    public int hpLevel;
    public int regenLevel;
    public int critChanceLevel;
    public int critDamageLevel;

    public int playerLevel;
    public float currentExp;
    public float totalExp;
    public double gold = 10000;

    public float atkMultiplier = 1f;
    public float attackSpeedMultiplier = 1f; 
}

public class StatManager : MonoBehaviour
{
    public static PlayerStatData m_PlayerStatData = new PlayerStatData();

    public static Action<double> OnCoinChanged;
    public static Action OnStatUpgrade;
    public static Action OnLevelUp;
    public static Action OnCanLevelUp;
    public static float GetStatValue(StatType type, int level)
    {
        switch (type)
        {
            case StatType.ATK: 
                var baseValue = Mathf.Floor(10 * Mathf.Pow(1.15f, level));
                return baseValue * m_PlayerStatData.atkMultiplier;
            case StatType.HP: return Mathf.Floor(100 * Mathf.Pow(1.2f, level));
            case StatType.REGEN: return Mathf.Floor(1 + level * 0.5f);
            case StatType.CRIT_CHANCE: return Mathf.Min(5 + level * 0.5f, 50); // 최대 50%
            case StatType.CRIT_DAMAGE: return 1.5f + level * 0.1f; // 배수
            default: return 0;
        }
    }

    public static void SetCoin(double value)
    {
        m_PlayerStatData.gold += value;
        OnCoinChanged?.Invoke(m_PlayerStatData.gold);
    }

    public static int GetLevel(StatType stat)
    {
        switch(stat)
        {
            case StatType.ATK: return m_PlayerStatData.atkLevel;
            case StatType.HP: return m_PlayerStatData.hpLevel;
            case StatType.REGEN: return m_PlayerStatData.regenLevel;
            case StatType.CRIT_CHANCE: return m_PlayerStatData.critChanceLevel;
            case StatType.CRIT_DAMAGE: return m_PlayerStatData.critDamageLevel;
            default: return -1;
        }
    }

    public bool CanLevelUp() => m_PlayerStatData.currentExp >= GetExpToLevelUp(m_PlayerStatData.playerLevel);

    public static int GetUpgradeCost(StatType type, int level)
    {
        return Mathf.FloorToInt(100 * Mathf.Pow(1.25f, level));
    }

    int GetExpToLevelUp(int level)
    {
        return Mathf.FloorToInt(50 * Mathf.Pow(1.3f, level));
    }

    public void GainExp(float amount)
    {
        m_PlayerStatData.currentExp += amount;
        m_PlayerStatData.totalExp += amount;

        if (CanLevelUp())
        {
            OnCanLevelUp?.Invoke();
        }
    }

    public void LevelUp()
    {
        float needExp = GetExpToLevelUp(m_PlayerStatData.playerLevel);
        if (m_PlayerStatData.currentExp >= needExp)
        {
            m_PlayerStatData.currentExp -= needExp;
            m_PlayerStatData.playerLevel++;

            OnLevelUp?.Invoke();
        }
    }
    public static double GetMonsterHP(int stage, int wave)
    {
        double baseHP = 30;
        double stageMultiplier = Mathf.Pow(1.5f, stage - 1);
        double waveMultiplier = Mathf.Pow(1.1f, wave);       
        return Mathf.Floor((float)baseHP * (float)stageMultiplier * (float)waveMultiplier);
    }

    public static float GetMonsterExp(int stage, int wave)
    {
        float baseExp = 10f;
        float stageMultiplier = Mathf.Pow(1.3f, stage - 1);
        float waveMultiplier = 1 + (wave * 0.2f);
        return Mathf.Floor(baseExp * stageMultiplier * waveMultiplier);
    }

    public static bool UpgradeStat(StatType type)
    {
        int currentLevel = GetLevel(type);
        int cost = GetUpgradeCost(type, currentLevel);
        if (m_PlayerStatData.gold >= cost)
        {
            SetCoin(-cost);
            switch (type)
            {
                case StatType.ATK: m_PlayerStatData.atkLevel++; break;
                case StatType.HP: m_PlayerStatData.hpLevel++; break;
                case StatType.REGEN: m_PlayerStatData.regenLevel++; break;
                case StatType.CRIT_CHANCE: m_PlayerStatData.critChanceLevel++; break;
                case StatType.CRIT_DAMAGE: m_PlayerStatData.critDamageLevel++; break;
            }
            OnStatUpgrade?.Invoke();
            return true;
        }
        return false;
    }


}
