using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillSlotUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Image cooldownMask;
    [SerializeField] private TextMeshProUGUI cooldownText; 

    public void SetIcon(Sprite icon)
    {
        if (iconImage != null)
            iconImage.sprite = icon;
    }

    public void SetCooldown(float fill, float remainingSec)
    {
        if (cooldownMask != null)
            cooldownMask.fillAmount = fill;

        if (cooldownText != null)
        {
            if (remainingSec > 0f)
            {
                cooldownText.text = string.Format("{0:0.0}", remainingSec);
                cooldownText.enabled = true;
            }
            else
            {
                cooldownText.text = "";
                cooldownText.enabled = false;
            }
        }
    }
}
