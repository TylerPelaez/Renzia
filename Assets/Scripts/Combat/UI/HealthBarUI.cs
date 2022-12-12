using System;
using TMPro;
using UnityEngine;

public class HealthBarUI : MonoBehaviour
{
    public RectTransform healthBarGreen;
    public RectTransform healthBarRed;
    public TextMeshProUGUI healthLabel;

    private const float LERP_TIME_LENGTH = 0.5f;
    private Vector3 lerpStartScale;
    private Vector3 lerpTargetScale;
    private float lerpStartTime;
    private bool lerpActive;


    private void Update()
    {
        if (lerpActive)
        {
            float t = Mathf.Min((Time.time - lerpStartTime) / LERP_TIME_LENGTH, 1);
            healthBarGreen.localScale = Vector3.Lerp(lerpStartScale, lerpTargetScale, t);
            if (t >= 1)
            {
                lerpActive = false;
            }
        }
    }

    public void SetHealth(int current, int max)
    {
        lerpStartTime = Time.time;
        lerpStartScale = healthBarGreen.localScale;
        lerpTargetScale = new Vector3((float) current / max, 1, 1);
        healthLabel.text = current + " / " + max;
        lerpActive = true;
    }
}
