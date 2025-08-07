using UnityEngine;

public class Skill03 : SkillBase
{
    public override void Activate()
    {
        if (!IsReady) return;

        Monster nearest = GameManager.Instance.TargetMonster;
        if (nearest == null) return;

        PlayEffect(nearest.transform.position + new Vector3(0, 0.3f, 0));

        nearest.TakeDamage(GetFinalDamage(), ElementType.Fire);

        cooldownTimer = skillData.cooldown;
    }
}
