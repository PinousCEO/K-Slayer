using System.Collections;
using UnityEngine;

public class Skill05 : SkillBase
{
    [SerializeField] private float buffDuration = 5f;
    [SerializeField] private float speedMultiplier = 1.5f; // 50% ¡ı∞°

    private Coroutine speedBuffCoroutine;

    public override void Activate()
    {
        if (!IsReady) return;

        if (speedBuffCoroutine != null)
            StopCoroutine(speedBuffCoroutine);

        speedBuffCoroutine = StartCoroutine(ApplySpeedBuff());
        cooldownTimer = skillData.cooldown;

        PlayEffect(player.transform.position + Vector3.up * 0.3f);
    }

    IEnumerator ApplySpeedBuff()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) yield break;

        float originalSpeed = gm.speed;
        gm.speed *= speedMultiplier;

        yield return new WaitForSeconds(buffDuration);

        gm.speed = originalSpeed;
    }
}
