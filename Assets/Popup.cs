using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Popup : MonoBehaviour
{
    private Vector2 initialWorldPosition;
    
    public Vector2 positionOffset;

    private RectTransform canvasTransform;
    private RectTransform rectTransform;
    private CanvasScaler canvasScaler;
    
    public void Initialize(string text, Vector3 startingWorldPosition, CanvasScaler scaler, RectTransform canvas, Color color)
    {
        TextMeshProUGUI textMesh = GetComponent<TextMeshProUGUI>();
        textMesh.text = text;
        textMesh.color = color;
        initialWorldPosition = startingWorldPosition;
        canvasScaler = scaler;
        canvasTransform = canvas;
        rectTransform = GetComponent<RectTransform>();
        
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(Camera.main, startingWorldPosition) / canvasScaler.scaleFactor;
        rectTransform.anchoredPosition = screenPoint - (canvasTransform.sizeDelta / 2.0f);
    }

    private void Update()
    {
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(Camera.main, initialWorldPosition) / canvasScaler.scaleFactor;
        rectTransform.anchoredPosition = screenPoint - (canvasTransform.sizeDelta / 2.0f) + positionOffset;
    }

    public void OnPopupAnimationComplete()
    {
        Destroy(gameObject);
    }
    
}
