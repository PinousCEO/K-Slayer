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
    SkeletonAnimation skeletonAnimation;
    private float targetFill = 1f;
    float speed;
    public bool isDead = false;

    void Start()
    {
        skeletonAnimation = GetComponent<SkeletonAnimation>();
        hpUI.SetActive(false);
        speed = GameManager.Instance.speed;
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
        maxHp = hp;
    }
    public void TakeDamage(double damage, ElementType element = ElementType.Normal)
    {
        skeletonAnimation.AnimationState.SetAnimation(0, "hit", false);

        hp -= damage;
        hp = Mathf.Clamp((float)hp, 0, (float)maxHp);
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
            if (GameManager.Instance.isBoss)
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
            Color startColor = skeleton.skeleton.GetColor(); // 초기 색상
            Color targetColor = new Color(0f, 0f, 0f, 0f);  // 붉은 검정 + 알파 0

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
