using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AttackModeUIOverlay : MonoBehaviour
{
    
    private List<Unit> targetableUnits;
    private Unit currentlyTargetedUnit;
    private Weapon attackingWeapon;
    
    [SerializeField]
    private RectTransform canvasTransform;

    [SerializeField] private CanvasScaler canvasScaler;

    [SerializeField] private TextMeshProUGUI damageBoundsLabel;
    [SerializeField] private TextMeshProUGUI critChanceLabel;
    [SerializeField] private TextMeshProUGUI actionPointCostLabel;
    [SerializeField] private GameObject nextButton;
    [SerializeField] private GameObject previousButton;

    [SerializeField] private Vector2Int overlayOffset;

    
    [SerializeField]
    private RectTransform damageOverlay;
    
    public void Initialize(List<Unit> targets, Unit currentTarget, Weapon weapon)
    {
        targetableUnits = targets;
        currentlyTargetedUnit = currentTarget;
        attackingWeapon = weapon;
        
        nextButton.SetActive(targetableUnits.Count != 1);
        previousButton.SetActive(targetableUnits.Count != 1);

        damageBoundsLabel.text = "DMG: <color=\"red\">" + weapon.MinDamage + "-" + weapon.MaxDamage;
        critChanceLabel.text = "Crit: <color=\"red\">" + (int)(weapon.CritChance * 100f) + "%";
        actionPointCostLabel.text = "AP Cost: <color=\"red\">" + weapon.ActionPointCost;
    }
    
    private void Update()
    {
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(Camera.main, currentlyTargetedUnit.transform.position) / canvasScaler.scaleFactor;
        damageOverlay.anchoredPosition = screenPoint - (canvasTransform.sizeDelta / 2.0f) + overlayOffset;
    }

    public void UpdateTarget(Unit targetedUnit)
    {
        currentlyTargetedUnit = targetedUnit;
    }
}
