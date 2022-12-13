using TMPro;
using UnityEngine;

public class CostIndicator : MonoBehaviour
{
    [field: SerializeField] private TextMeshProUGUI textLabel;

    public void SetText(string text, Color color)
    {
        textLabel.text = text;
        textLabel.color = color;
    }
}
