using System.Collections;
using UnityEngine;

public class Skill07 : SkillBase
{
    [SerializeField] private float buffDuration = 5f;
    [SerializeField] private float speedMultiplier = 1.5f; // 50% ¡ı∞°

    private Coroutine buffCoroutine;

    public override void Activate()
    {
        if (!IsReady) return;

        if (buffCoroutine != null)
            StopCoroutine(buffCoroutine);

        buffCoroutine = StartCoroutine(ApplyBuff());
        cooldownTimer = skillData.cooldown;

        PlayEffect(transform.position + Vector3.up * 1.5f);
    }

    IEnumerator ApplyBuff()
    {
        var stats = StatManager.m_PlayerStatData;
        if (stats == null) yield break;

        stats.attackSpeedMultiplier *= speedMultiplier;
        yield return new WaitForSeconds(buffDuration);
        stats.attackSpeedMultiplier /= speedMultiplier;
    }
}
