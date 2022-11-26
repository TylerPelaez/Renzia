using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InitiativeOrderUIController : MonoBehaviour
{
    private const float PIXELS_PER_UNIT = 64;
    
    public GameObject initiativePortraitPrefab;

    public void ResetInitiativeOrder(LinkedList<Unit> initiativeOrder)
    {
        foreach (Transform child in gameObject.transform)
        {
            Destroy(child.gameObject);
        }
        
        foreach (var unit in initiativeOrder)
        {
            var initiativePortrait = Instantiate(initiativePortraitPrefab, gameObject.transform);
            var tex = unit.initiativeOrderPortrait;
            initiativePortrait.GetComponent<Image>().sprite = Sprite.Create(unit.initiativeOrderPortrait, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), PIXELS_PER_UNIT);
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
    }

    public void OnTurnEnded()
    {
        // Just need to rearrange children
        gameObject.transform.GetChild(0).SetSiblingIndex(gameObject.transform.childCount - 1);
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
    }
}
