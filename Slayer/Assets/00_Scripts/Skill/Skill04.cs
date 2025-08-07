using UnityEngine;

public class Skill04 : SkillBase
{
    public override void Activate()
    {
        if (!IsReady) return;

        var stats = StatManager.m_PlayerStatData;

        float atk = StatManager.GetStatValue(StatType.ATK, stats.atkLevel);
        float healAmount = atk * skillData.damagePercentage;

        HealPlayer(healAmount);
        PlayEffect(player.transform.position + Vector3.up * 0.3f);
        cooldownTimer = skillData.cooldown;
    }

    private void HealPlayer(float amount)
    {
        //float maxHp = StatManager.GetStatValue(StatType.HP, StatManager.m_PlayerStatData.hpLevel);
        //StatManager.m_PlayerStatData.currentHp = Mathf.Min(
        //    StatManager.m_PlayerStatData.currentHp + amount,
        //    maxHp
        //);

        DamageText.Create(player.transform.position, amount, Color.green);
    }
}
