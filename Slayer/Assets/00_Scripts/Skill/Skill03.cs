using System.Collections.Generic;
using UnityEngine;

public class Skill03 : SkillBase
{
    public override void Activate()
    {
        if (!IsReady) return;

        var allMonsters = GameManager.Instance.activeMonsters();
        List<Monster> targets = new();

        foreach (var monster in allMonsters)
        {
            targets.Add(monster);
        }
        if (targets.Count == 0) return;

        PlayEffect(transform.position + new Vector3(0, 0.3f, 0));

        foreach (var monster in targets)
        {
            monster.TakeDamage(GetFinalDamage(), ElementType.Fire);
        }
        cooldownTimer = skillData.cooldown;
    }
}
