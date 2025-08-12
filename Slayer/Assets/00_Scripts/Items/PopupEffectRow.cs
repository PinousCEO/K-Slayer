using UnityEngine;
using TMPro;

public class PopupEffectRow : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private TMP_Text value;

    public void Set(string labelText, string valueText)
    {
        if (label) label.text = labelText;
        if (value) value.text = valueText;
    }
}
