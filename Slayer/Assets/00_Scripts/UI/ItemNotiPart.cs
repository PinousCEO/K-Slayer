using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
public class ItemNotiPart : MonoBehaviour
{
    [SerializeField] private Image IconImage;
    [SerializeField] private TextMeshProUGUI MainText;
    private float fadeDuration = 0.5f;
    private float displayDuration = 1.0f;
    CanvasGroup canvasGroup;

    public void Init(ItemNoti noti, ItemType type, double count)
    {
        IconImage.sprite = Utils.GetAtlas(type.ToString());
        MainText.text = "+" + count.ToString();

        canvasGroup = GetComponent<CanvasGroup>();

        canvasGroup.alpha = 1f;
        canvasGroup.DOKill();

        Sequence seq = DOTween.Sequence();
        seq.AppendInterval(displayDuration);
        seq.Append(canvasGroup.DOFade(0f, fadeDuration))
            .OnComplete(() =>
            {
                noti.DestroyNextPart();
            });
    }
}
