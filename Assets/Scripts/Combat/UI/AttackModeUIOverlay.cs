using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AttackModeUIOverlay : MonoBehaviour
{    
    private List<Unit> targetableUnits;
    private Unit currentlyTargetedUnit;
    private Weapon attackingWeapon;
    
    [SerializeField]
    private RectTransform canvasTransform;

    [SerializeField] private TextMeshProUGUI damageBoundsLabel;
    [SerializeField] private TextMeshProUGUI critChanceLabel;
    [SerializeField] private TextMeshProUGUI actionPointCostLabel;
    
    [SerializeField]
    private RectTransform damageOverlay;
    
    public void Initialize(List<Unit> targets, Unit currentTarget, Weapon weapon)
    {
        targetableUnits = targets;
        currentlyTargetedUnit = currentTarget;
        attackingWeapon = weapon;

        damageBoundsLabel.text = "DMG: <color=\"red\">" + weapon.MinDamage + "-" + weapon.MaxDamage;
        critChanceLabel.text = "Crit: <color=\"red\">" + (int)(weapon.CritChance * 100f) + "%";
        actionPointCostLabel.text = "AP Cost: <color=\"red\">" + weapon.ActionPointCost;
    }
    
    private void Update()
    {
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(Camera.main, currentlyTargetedUnit.transform.position);
        damageOverlay.anchoredPosition = screenPoint - (canvasTransform.sizeDelta / 2.0f);
    }

    public void UpdateTarget(Unit targetedUnit)
    {
        currentlyTargetedUnit = targetedUnit;
    }
}
