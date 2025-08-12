using System.Collections;
using UnityEngine;

public class Skill08 : SkillBase
{
    [SerializeField] private float dashSpeed = 8f;
    [SerializeField] private float stopDistance = 0.1f;
    [SerializeField] private float attackDelay = 0.3f;

    public override void Activate()
    {
        if (!IsReady) return;

        cooldownTimer = skillData.cooldown;
        StartCoroutine(DashToTarget());
        PlayEffect(player.transform.position + Vector3.up * 0.3f);
    }

    private IEnumerator DashToTarget()
    {
        var monsters = GameManager.Instance.activeMonsters();
        int targetIndex = 0;

        while (targetIndex < monsters.Count)
        {
            var target = monsters[targetIndex];

            if (target == null || target.isDead)
            {
                targetIndex++;
                continue;
            }

            var skeleton = player.GetComponent<Spine.Unity.SkeletonAnimation>();
            skeleton.timeScale = dashSpeed / 2f;

            if (target.isDead)
                break;

            else if (!target.isDead)
            {
                target.TakeDamage(GetFinalDamage());

                if (!target.isDead)
                    yield break;

                break;
            }

            yield return null;
        }
    }
}
