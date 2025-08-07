using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class UpgradeFramePart : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI valueLabel;
    [SerializeField] private TextMeshProUGUI costLabel;
    [SerializeField] private TextMeshProUGUI levelLabel;

    [Header("Stat Config")]
    public StatType statType;

    private bool isHolding = false;
    private float holdDelay = 0.03f;
    private Coroutine holdCoroutine;

    private void OnEnable()
    {
        StatManager.OnCoinChanged += Refresh;
        Refresh(StatManager.m_PlayerStatData.gold);
    }

    public void Refresh(double value)
    {
        int level = StatManager.GetLevel(statType);
        float current = StatManager.GetStatValue(statType, level);
        float next = StatManager.GetStatValue(statType, level + 1);
        int cost = StatManager.GetUpgradeCost(statType, level);

        valueLabel.text = $"{current} ¡æ {next}";
        costLabel.text = $"{cost:N0} Gold";
        levelLabel.text = $"Lv.{level+1}";

        costLabel.color = value < cost ? Color.red : Color.white;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isHolding = true;
        Upgrade();
        holdCoroutine = StartCoroutine(HoldUpgrade());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isHolding = false;
        if (holdCoroutine != null)
        {
            StopCoroutine(holdCoroutine);
            holdCoroutine = null;
        }
    }

    IEnumerator HoldUpgrade()
    {
        yield return new WaitForSeconds(0.5f);

        while (isHolding)
        {
            Upgrade();
            yield return new WaitForSeconds(holdDelay);
        }
    }

    public void Upgrade()
    {
        var success = StatManager.UpgradeStat(statType);
    }
}
