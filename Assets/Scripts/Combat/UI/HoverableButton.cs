
using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class HoverableButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public EventHandler OnHoverEnter;
    public EventHandler OnHoverExit;
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        OnHoverEnter?.Invoke(this, EventArgs.Empty);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnHoverExit?.Invoke(this, EventArgs.Empty);
    }
}
