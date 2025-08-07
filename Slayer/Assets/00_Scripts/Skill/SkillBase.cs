using UnityEngine;
public enum ElementType
{
    Normal,
    Fire,
    Ice,
    Lightning
}
public abstract class SkillBase : MonoBehaviour
{
    public SkillData skillData;
    public Player player;
    protected float cooldownTimer;
    public float RemainingCooldown => Mathf.Max(0f, cooldownTimer);
    public bool IsReady => cooldownTimer <= 0f;

    public virtual void Initialize(SkillData data)
    {
        skillData = data;
        cooldownTimer = 0f;
    }

    public virtual void UpdateCooldown(float deltaTime)
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= deltaTime;
    }
    public void TryActivate()
    {
        if (!IsReady) return;

        player.AnimationChange("SKILL", false);
        Activate();
        cooldownTimer = skillData.cooldown;
    }
    protected float GetFinalDamage()
    {
        var stats = StatManager.m_PlayerStatData;
        float atk = StatManager.GetStatValue(StatType.ATK, stats.atkLevel);
        return atk * skillData.damagePercentage;
    }
    public abstract void Activate();

    protected void PlayEffect(Vector3 position, Transform parent = null)
    {
        if (skillData.effectPrefab == null) return;

        GameObject fx = Instantiate(skillData.effectPrefab, position, Quaternion.identity, parent);

        Animator animator = fx.GetComponent<Animator>();
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            AnimationClip clip = animator.runtimeAnimatorController.animationClips[0];
            Destroy(fx, clip.length);
        }
        else
        {
            Destroy(fx, 2f);
        }
    }
}