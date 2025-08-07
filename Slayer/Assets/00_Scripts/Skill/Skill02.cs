using System.Collections.Generic;
using UnityEngine;

public class Skill02 : SkillBase
{
    [SerializeField] private float radius = 3f;
    [SerializeField] private float baseDamage = 100f;

    public override void Activate()
    {
        if (!IsReady) return;

        var allMonsters = GameManager.Instance.activeMonsters();
        List<Monster> targets = new();

        foreach (var monster in allMonsters)
        {
            if (monster == null) continue;

            float dist = Vector3.Distance(transform.position, monster.transform.position);
            if (dist <= radius)
                targets.Add(monster);
        }

        if (targets.Count == 0) return;

        Vector3 center = Vector3.zero;
        foreach (var monster in targets)
            center += monster.transform.position;

        center /= targets.Count;

        PlayEffect(center + new Vector3(0, 0.3f, 0));

        foreach (var monster in targets)
        {
            monster.TakeDamage(GetFinalDamage());
        }

        cooldownTimer = skillData.cooldown;
    }
}
