using UnityEngine;
using UnityEngine.UI;
using Object = System.Object;

public class HealthBarUI : MonoBehaviour
{
    private static readonly Vector2 HEALTH_BAR_OFFSET = new (0, 20);
    
    public GameObject healthBarItemPrefab;

    private Unit target;
    private CanvasScaler canvasScaler;
    private RectTransform rectTransform;
    private RectTransform canvasTransform;
    
    private void Update()
    {
        if (target != null)
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(Camera.main, target.transform.position) / canvasScaler.scaleFactor;
            rectTransform.anchoredPosition = screenPoint - (canvasTransform.sizeDelta / 2.0f) + HEALTH_BAR_OFFSET;
        }
    }

    public void Initialize(Unit unit, CanvasScaler scaler, RectTransform canvas)
    {
        target = unit;
        target.OnHealthChanged += OnHealthChanged;
        rectTransform = GetComponent<RectTransform>();
        canvasScaler = scaler;
        canvasTransform = canvas;
        SetHealth(target.Health, target.MaxHealth);
    }

    private void OnHealthChanged(Object caller, Unit.HealthChangedEventArgs healthChangedEvent)
    {
        SetHealth(healthChangedEvent.NewValue, healthChangedEvent.MaxHealth);
    }
    
    private void SetHealth(int current, int max)
    {
        if (current <= 0)
        {
            Destroy(gameObject);
            return;
        }
        
        foreach (Transform child in gameObject.transform)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < current; i++)
        {
            Instantiate(healthBarItemPrefab, transform);
        }
        
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
    }

    private void OnDestroy()
    {
        if (target)
        {
            target.OnHealthChanged -= OnHealthChanged;
        }
    }
}
