using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.WSA;

public class Main_UI : MonoBehaviour
{
    [Header("UI Prefabs")]
    [SerializeField] private SkillSlotUI skillSlotUIPrefab;

    [Header("Layout Parent")]
    [SerializeField] private Transform skillSlotUIParent; // Horizontal Layout Group 대상

    private List<SkillSlotUI> activeSkillUIs = new();

    [SerializeField] private TextMeshProUGUI PlayerCoinText;
    [SerializeField] private TextMeshProUGUI TitleText;
    [SerializeField] private Image roundContentFill;
    [SerializeField] private Image bossContentFill;
    [SerializeField] private Image bossTimerFill;
    [SerializeField] private Image fadeImage;    
    private void Awake()
    {
        SkillManager.Instance.OnSkillEquipment += OnSkillEquipped;
        StatManager.OnCoinChanged += RefreshCoin;
        GameManager.Instance.RoundUpAction += RoundFill;
        GameManager.Instance.RegisterStateAction(Game_State.BOSS, OnBoss);
        GameManager.Instance.RegisterStateAction(Game_State.GAMECLEAR, FadeInAndOut);
        GameManager.Instance.RegisterStateAction(Game_State.DungeonBoss, OnDungeon);
    }
    private void Start()
    {
        foreach (Transform child in skillSlotUIParent)
        {
            Destroy(child.gameObject);
        }
      
        RefreshCoin(StatManager.m_PlayerStatData.gold);

        SkillManager.Instance.EquipSkill(0, Resources.Load<SkillData>("Skills/Skill01"));
        SkillManager.Instance.EquipSkill(1, Resources.Load<SkillData>("Skills/Skill02"));
        SkillManager.Instance.EquipSkill(2, Resources.Load<SkillData>("Skills/Skill03"));
        SkillManager.Instance.EquipSkill(3, Resources.Load<SkillData>("Skills/Skill04"));
        SkillManager.Instance.EquipSkill(4, Resources.Load<SkillData>("Skills/Skill05"));
        SkillManager.Instance.EquipSkill(5, Resources.Load<SkillData>("Skills/Skill06"));
        SkillManager.Instance.EquipSkill(6, Resources.Load<SkillData>("Skills/Skill07"));
        SkillManager.Instance.EquipSkill(7, Resources.Load<SkillData>("Skills/Skill08"));
    }

    private void OnDestroy()
    {
        SkillManager.Instance.OnSkillEquipment -= OnSkillEquipped;
        StatManager.OnCoinChanged -= RefreshCoin;
        GameManager.Instance.RoundUpAction -= RoundFill;
        GameManager.Instance.UnregisterStateAction(Game_State.BOSS, OnBoss);
        GameManager.Instance.UnregisterStateAction(Game_State.GAMECLEAR, FadeInAndOut);
        GameManager.Instance.UnregisterStateAction(Game_State.DungeonBoss, OnDungeon);
    }

    public void FadeInAndOut()
    {
        StopAllCoroutines();
        StartCoroutine(FadeInAndOutCoroutine());
    }

    private IEnumerator FadeInAndOutCoroutine()
    {
        yield return new WaitForSeconds(1.5f);

        yield return StartCoroutine(Fade(0f, 1f, 1.0f));

        yield return new WaitForSeconds(1.0f);

        yield return StartCoroutine(Fade(1f, 0f, 1.0f));
        OnChangeFillAmount(false);
        TitleText.text = string.Format("WAVE {0}-{1}", GameManager.Instance.CurrentRound.Stage, GameManager.Instance.CurrentRound.Wave);
        GameManager.Instance.Game_StateChange(Game_State.MOVE);
    }

    private IEnumerator Fade(float from, float to, float duration, Action action = null)
    {
        float elapsed = 0f;
        Color color = fadeImage.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            color.a = Mathf.Lerp(from, to, t);
            fadeImage.color = color;
            Debug.Log(fadeImage.color);
            yield return null;
        }

        if (action != null)
            action?.Invoke();
        
        color.a = to;
        fadeImage.color = color;
    }

    private void OnDungeon()
    {
        OnBoss();
        FindFirstObjectByType<Player>().AnimationChange("IDLE", true);
        GameManager.Instance.ClearAllMonsters();
        UIManager.Instance.ShowPage(BottomBar.Default);
        StartCoroutine(DungeonFade());
    }
    private IEnumerator DungeonFade()
    {
        yield return StartCoroutine(Fade(0f, 1f, 1.0f));
        yield return new WaitForSeconds(1.0f);
        TitleText.text = "시공의 우물";
        yield return StartCoroutine(Fade(1f, 0f, 1.0f));
        yield return new WaitForSeconds(1.5f);
        StartCoroutine(BossTimerCoroutine());
        GameManager.Instance.Game_StateChange(Game_State.DungeonStart);
    }
    private void OnBoss()
    {
        OnChangeFillAmount(true);
        bossContentFill.fillAmount = 1.0f;
        bossTimerFill.fillAmount = 1.0f;
    }

    private void OnChangeFillAmount(bool isBoss)
    {
        roundContentFill.transform.parent.gameObject.SetActive(!isBoss);
        bossContentFill.transform.parent.parent.gameObject.SetActive(isBoss);
    }
    private IEnumerator BossTimerCoroutine()
    {
        float current = 0.0f;
        float percent = 0.0f;
        float start = 1.0f;
        float end = 0.0f;
        while(percent < 1.0f)
        {
            current += Time.deltaTime;
            percent = current / 30.0f;
            float LerpPos = Mathf.Lerp(start, end, percent);
            bossTimerFill.fillAmount = LerpPos;
            yield return null;
        }
    }
    public void OnBossRoundFill(double currentBoosHP, double currentMaxBossHP)
    {
        StopAllCoroutines();
        StartCoroutine(BossFillCoroutine(currentBoosHP, currentMaxBossHP));
    }
    IEnumerator BossFillCoroutine(double currentBoosHP, double currentMaxBossHP)
    {
        float targetFill = Mathf.Clamp01((float)currentBoosHP / (float)currentMaxBossHP);
        float currentFill = bossContentFill.fillAmount;

        while (Mathf.Abs(currentFill - targetFill) > 0.001f)
        {
            currentFill = Mathf.Lerp(currentFill, targetFill, Time.deltaTime * 2.0f);
            bossContentFill.fillAmount = currentFill;
            yield return null;
        }

        bossContentFill.fillAmount = targetFill;
    }
    private void RoundFill(int currentRound)
    {
        StopAllCoroutines();
        TitleText.text = string.Format("WAVE {0}-{1}", GameManager.Instance.CurrentRound.Stage, GameManager.Instance.CurrentRound.Wave);
        StartCoroutine(RoundFillCoroutine(currentRound, GameManager.Instance.CurrentRound.MaxWave));
    }

    IEnumerator RoundFillCoroutine(int currentRound, int maxRound)
    {
        float targetFill = Mathf.Clamp01((float)currentRound / maxRound);
        float currentFill = roundContentFill.fillAmount;

        while (Mathf.Abs(currentFill - targetFill) > 0.001f)
        {
            currentFill = Mathf.Lerp(currentFill, targetFill, Time.deltaTime * 2.0f);
            roundContentFill.fillAmount = currentFill;
            yield return null;
        }

        roundContentFill.fillAmount = targetFill;
    }
    public void RefreshCoin(double value)
    {
        PlayerCoinText.text = value.ToString();
    }
    private void OnSkillEquipped(SkillData data)
    {
        CreateSkillUI(data);
    }

    private void CreateSkillUI(SkillData data)
    {
        SkillSlotUI ui = Instantiate(skillSlotUIPrefab, skillSlotUIParent);
        ui.transform.SetAsLastSibling();
        ui.SetIcon(data.skillIcon);
        ui.SetCooldown(0f, 0f); // 쿨타임 초기화
        activeSkillUIs.Add(ui);
    }

    private void Update()
    {
        for (int i = 0; i < activeSkillUIs.Count; i++)
        {
            var slot = SkillManager.Instance.GetSlots()[i];
            if (slot.skillScript == null) continue;

            float ratio = slot.skillScript.IsReady ? 0f : slot.skillScript.RemainingCooldown / slot.skillScript.skillData.cooldown;
            float remaining = slot.skillScript.RemainingCooldown;

            activeSkillUIs[i].SetCooldown(ratio, remaining);
        }
    }
}
