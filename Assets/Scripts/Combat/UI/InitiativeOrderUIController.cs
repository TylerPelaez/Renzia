using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InitiativeOrderUIController : MonoBehaviour
{
    private const float PIXELS_PER_UNIT = 32;
    
    public GameObject initiativePortraitPrefab;

    private Vector2 defaultPortraitSize;
    
    public void ResetInitiativeOrder(LinkedList<Unit> initiativeOrder)
    {
        foreach (Transform child in gameObject.transform)
        {
            Destroy(child.gameObject);
        }
        
        foreach (var unit in initiativeOrder)
        {
            GameObject portraitBackground = Instantiate(initiativePortraitPrefab, gameObject.transform);
            Texture2D tex = unit.initiativeOrderPortrait;
            GameObject portrait = portraitBackground.transform.GetChild(0).gameObject;
            portrait.GetComponent<Image>().sprite = Sprite.Create(unit.initiativeOrderPortrait, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), PIXELS_PER_UNIT);
            
            RectTransform portraitRectTransform = portraitBackground.GetComponent<RectTransform>();
            Rect portraitRect = portraitRectTransform.rect;

            defaultPortraitSize = new Vector2(portraitRect.width, portraitRect.height);
            if (portraitBackground.transform.GetSiblingIndex() == 0)
            {
                SetRectTransformSize(portraitRectTransform, defaultPortraitSize + new Vector2(2, 2));
            }
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
    }

    private void SetRectTransformSize(RectTransform transform, Vector2 newSize)
    {
        transform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newSize.x);
        transform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newSize.y);

    }

    public void OnTurnEnded()
    {
        // Just need to rearrange children
        Transform turnEndedChild = gameObject.transform.GetChild(0);
        SetRectTransformSize(turnEndedChild.gameObject.GetComponent<RectTransform>(), defaultPortraitSize);
        
        turnEndedChild.SetSiblingIndex(gameObject.transform.childCount - 1);
        SetRectTransformSize(gameObject.transform.GetChild(0).GetComponent<RectTransform>(), defaultPortraitSize + new Vector2(2, 2));
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
        
    }
}
