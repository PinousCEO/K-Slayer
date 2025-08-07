using System.Collections;
using UnityEngine;

public class Skill01 : SkillBase
{
    [SerializeField] private float buffDuration = 10f;
    [SerializeField] private float attackMultiplier = 1.3f; // +30%

    private Coroutine buffCoroutine;

    public override void Activate()
    {
        if (!IsReady) return;

        if (buffCoroutine != null)
            StopCoroutine(buffCoroutine);

        buffCoroutine = StartCoroutine(ApplyBuff());
        cooldownTimer = skillData.cooldown;

        PlayEffect(player.transform.position + new Vector3(0, 0.3f, 0), player.transform);
    }

    IEnumerator ApplyBuff()
    {
        var stats = StatManager.m_PlayerStatData;
        if (stats == null) yield break;

        stats.atkMultiplier *= attackMultiplier;
        yield return new WaitForSeconds(buffDuration);
        stats.atkMultiplier /= attackMultiplier;
    }
}
