using TMPro;
using UnityEngine;

public class HealthBarUI : MonoBehaviour
{
    public RectTransform healthBarGreen;
    public RectTransform healthBarRed;
    public TextMeshProUGUI healthLabel;
    
    public void SetHealth(int current, int max)
    {
        healthBarGreen.localScale = new Vector3((float) current / max, 1, 1);
        healthLabel.text = current + " / " + max;
    }
}
