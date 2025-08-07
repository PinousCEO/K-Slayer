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
            skeleton.AnimationState.SetAnimation(0, "RUN", true);
            skeleton.timeScale = dashSpeed / 2f;

            while (Vector2.Distance(player.transform.position, target.transform.position) > stopDistance)
            {
                Vector3 dir = (target.transform.position - player.transform.position).normalized;
                player.transform.position += dir * dashSpeed * Time.deltaTime;

                if (target.isDead)
                    break;
                
                else if (!target.isDead)
                {
                    target.TakeDamage(GetFinalDamage());
                    
                    if(!target.isDead)
                        yield break;
                    
                    break;
                }

                yield return null;
            }
        }
    }
}
