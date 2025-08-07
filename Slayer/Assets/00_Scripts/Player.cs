using Spine.Unity;
using System.Collections;
using UnityEngine;

public class Player : MonoBehaviour
{
    private SkeletonAnimation skeletonAnimation;
    [SerializeField] private float attackRange;
    private bool isAttacking = false;
    private void Start()
    {
        skeletonAnimation = GetComponent<SkeletonAnimation>();
        GameManager.Instance.RegisterStateAction(Game_State.IDLE, OnIDLE);
        GameManager.Instance.RegisterStateAction(Game_State.MOVE, OnMOVE);
        GameManager.Instance.RegisterStateAction(Game_State.ATTACK, OnATTACK);
        GameManager.Instance.RegisterStateAction(Game_State.ATTACKANDMOVE, OnMOVE);
        GameManager.Instance.RegisterStateAction(Game_State.BOSS, OnBOSS);
    }
    #region Destroy
    private void OnDestroy()
    {
        GameManager.Instance.UnregisterStateAction(Game_State.IDLE, OnIDLE);
        GameManager.Instance.UnregisterStateAction(Game_State.MOVE, OnMOVE);
        GameManager.Instance.UnregisterStateAction(Game_State.ATTACK, OnATTACK);
        GameManager.Instance.UnregisterStateAction(Game_State.ATTACKANDMOVE, OnMOVE);
        GameManager.Instance.UnregisterStateAction(Game_State.BOSS, OnBOSS);
    }
    #endregion
    private void Update()
    {
        if (GameManager.Instance.game_State == Game_State.MOVE || GameManager.Instance.game_State == Game_State.ATTACKANDMOVE)
        {
            CheckAttackRange();
        }
    }

    private void CheckAttackRange()
    {
        var target = GameManager.Instance.TargetMonster;
        if (target == null) return;

        if (Vector2.Distance(transform.position, target.transform.position) <= attackRange)
        {
            GameManager.Instance.Game_StateChange(Game_State.ATTACK);
        }
    }

    private void OnIDLE()
    {
        StopAllCoroutines();
        skeletonAnimation.timeScale = 1.0f;
        AnimationChange("IDLE", true);
    }

    private void OnMOVE()
    {
        StopAllCoroutines();
        AnimationChange("RUN", true);

        float baseSpeed = 2f;
        float actualSpeed = GameManager.Instance.speed;

        float animationSpeed = actualSpeed / baseSpeed;
        skeletonAnimation.timeScale = animationSpeed;
    }

    private void OnATTACK()
    {
        float attackSpeed = StatManager.m_PlayerStatData.attackSpeedMultiplier;
        skeletonAnimation.timeScale = attackSpeed;
        AnimationChange("ATTACK", false);
        StartCoroutine(AttackCoroutine());
    }

    private void OnBOSS()
    {
        StopAllCoroutines();
        skeletonAnimation.timeScale = 1.0f;
        AnimationChange("IDLE", true);
    }

    private IEnumerator AttackCoroutine()
    {
        float attackSpeed = StatManager.m_PlayerStatData.attackSpeedMultiplier;

        yield return new WaitForSeconds(0.5f / attackSpeed);

        var target = GameManager.Instance.TargetMonster;
        if (target != null)
        {
            target.TakeDamage(StatManager.GetStatValue(StatType.ATK, StatManager.m_PlayerStatData.atkLevel));
        }

        yield return new WaitForSeconds(1.0f / attackSpeed);

        isAttacking = false;
        OnATTACK();
    }

    public void AnimationChange(string temp, bool loop)
    {
        skeletonAnimation.AnimationState.SetAnimation(0, temp, loop);
        if(temp == "SKILL")
        {
            StartCoroutine(DelayAnimationCheck());
        }
    }

    IEnumerator DelayAnimationCheck()
    {
        yield return new WaitForSeconds(1.167f);
        var currentAnim = skeletonAnimation.AnimationState.GetCurrent(0);
        if (currentAnim != null && currentAnim.Animation.Name == "SKILL")
        {
            skeletonAnimation.AnimationState.SetAnimation(0, "RUN", true);
        }
    }
}
