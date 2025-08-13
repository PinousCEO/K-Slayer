using DG.Tweening.Core.Easing;
using Spine.Unity;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
public class Monster : MonoBehaviour
{
    public bool isBoss;
    public double maxHp = 100;
    public double hp = 100;

    [SerializeField] private GameObject hpUI;
    [SerializeField] private Image immediateFill;
    [SerializeField] private Image delayedFill;
    [SerializeField] private float smoothSpeed = 2f;
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private GameObject HitPrefab;
    SkeletonAnimation skeletonAnimation;
    private float targetFill = 1f;
    float speed;
    public bool isDead = false;

    [Header("Hit Flash")]
    [SerializeField] private Color hitFlashColor = new Color(1f, 0.25f, 0.25f, 1f);
    [SerializeField] private float flashIn = 0.03f;   
    [SerializeField] private float hold = 0.05f;
    [SerializeField] private float flashOut = 0.12f;  

    private Color baseColor = Color.white;
    private Coroutine flashCo;
    void Start()
    {
        skeletonAnimation = GetComponent<SkeletonAnimation>();

        var skel = skeletonAnimation.Skeleton;
        baseColor = new Color(skel.R, skel.G, skel.B, skel.A);

        hpUI.SetActive(false);
        speed = GameManager.Instance.speed;
    }
    IEnumerator HardCoodCoroutine()
    {
        yield return new WaitForSeconds(5.0f);
        skeletonAnimation.AnimationState.SetAnimation(0, "skill", false);
        yield return new WaitForSeconds(2.0f);
        FindFirstObjectByType<Player>().AnimationChange("DIE", false);
        yield return new WaitForSeconds(2.0f);
        GameManager.Instance.Game_StateChange(Game_State.GAMECLEAR);
    }
    void Update()
    {
        if (hpUI.activeSelf)
        {
            float currentFill = delayedFill.fillAmount;
            delayedFill.fillAmount = Mathf.Lerp(currentFill, targetFill, Time.deltaTime * smoothSpeed);
        }

        if (GameManager.Instance.game_State == Game_State.MOVE || GameManager.Instance.game_State == Game_State.ATTACKANDMOVE) 
        transform.Translate(Vector3.left * speed * Time.deltaTime);
    }

    public void Initialize()
    {
        var hpBase = StatManager.GetMonsterHP(GameManager.Instance.CurrentRound.Stage, GameManager.Instance.CurrentRound.Wave);
        hp = isBoss ? hpBase * 10.0f : hpBase;

        if (GameManager.Instance.isDungeon)
        {
            hp *= 10;
            StartCoroutine(HardCoodCoroutine());
        }
        maxHp = hp;
    }
    public void TakeDamage(double damage, ElementType element = ElementType.Normal)
    {
        if(GameManager.Instance.isDungeon && isBoss)
        {
            
        }
        else
            skeletonAnimation.AnimationState.SetAnimation(0, "hit", false);

        StartHitFlash();

        hp -= damage;
        hp = Mathf.Clamp((float)hp, 0, (float)maxHp);
        Instantiate(HitPrefab, transform.position + Random.insideUnitSphere * 0.5f, Quaternion.Euler(0, 0, Random.Range(-180, 180.0f)));
        if(isBoss)
        {
            CanvasScriptHolder.main.OnBossRoundFill(hp, maxHp);
        }
        Color color = Color.white;
        switch(element)
        {
            case ElementType.Normal: break;
            case ElementType.Fire: color = Color.red; break;
            case ElementType.Ice: color = Color.blue; break;
            case ElementType.Lightning: color = Color.yellow; break;
        }

        ShowDamageText((int)damage, color);
        UpdateHPUI();

        if (hp <= 0)
        {
            Die();
        }
    }
    private void StartHitFlash()
    {
        if (flashCo != null) StopCoroutine(flashCo);
        flashCo = StartCoroutine(HitFlashRoutine());
    }

    private IEnumerator HitFlashRoutine()
    {
        var skel = skeletonAnimation.Skeleton;

        float t = 0f;
        while (t < flashIn)
        {
            t += Time.deltaTime;
            skel.SetColor(Color.Lerp(baseColor, hitFlashColor, t / flashIn));
            yield return null;
        }
        skel.SetColor(hitFlashColor);

        if (hold > 0f) yield return new WaitForSeconds(hold);

        t = 0f;
        while (t < flashOut)
        {
            t += Time.deltaTime;
            skel.SetColor(Color.Lerp(hitFlashColor, baseColor, t / flashOut));
            yield return null;
        }
        skel.SetColor(baseColor);
        flashCo = null;
    }

    private void UpdateHPUI()
    {
        if (!hpUI.activeSelf)
            hpUI.SetActive(true);

        float fillAmount = (float)hp / (float)maxHp;
        immediateFill.fillAmount = fillAmount;
        targetFill = fillAmount;
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        GameManager.Instance.UnregisterMonster(this);
        if (GameManager.Instance.MonstersCount() == 0)
        {
            if (GameManager.Instance.isBoss || GameManager.Instance.isDungeon)
            {
                GameManager.Instance.Game_StateChange(Game_State.GAMECLEAR);
            }
            else
            {
                if (GameManager.Instance.CurrentRound.Wave >= GameManager.Instance.CurrentRound.MaxWave)
                {
                    GameManager.Instance.Game_StateChange(Game_State.BOSS);
                }
                else
                    GameManager.Instance.Game_StateChange(Game_State.MOVE);
            }
        }
        else
            GameManager.Instance.Game_StateChange(Game_State.ATTACKANDMOVE);

        ItemNoti.OnGetItem?.Invoke(ItemType.Exp, StatManager.GetMonsterExp(GameManager.Instance.CurrentRound.Stage, GameManager.Instance.CurrentRound.Wave));

        SpawnCoins(Random.Range(1,3));
        StartCoroutine(DieEffect());
    }

    private IEnumerator DieEffect()
    {
        var skeleton = GetComponent<SkeletonAnimation>();
        if (skeleton != null)
        {
            Color startColor = skeleton.skeleton.GetColor(); 
            Color targetColor = new Color(0f, 0f, 0f, 0f); 

            float t = 0f;
            float duration = 1.5f;

            while (t < duration)
            {
                t += Time.deltaTime;
                float progress = t / duration;
                Color lerped = Color.Lerp(startColor, targetColor, progress);
                skeleton.skeleton.SetColor(lerped);
                yield return null;
            }
        }

        Destroy(gameObject);
    }

    private void ShowDamageText(double damage, Color color)
    {
        DamageText.Create(transform.position, damage, color);
    }

    private void SpawnCoins(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var coinObj = Instantiate(coinPrefab, transform.position, Quaternion.identity);
            var coin = coinObj.GetComponent<Item>();

            coin.Init(transform.position, ItemType.Coin, Random.Range(50, 300));
        }
    }
}
