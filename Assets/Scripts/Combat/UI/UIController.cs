using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    private const int ACTION_BUTTON_PIXELS_PER_UNIT = 36;
    
    [SerializeField]
    private TextMeshProUGUI actionPointLabel;
    [SerializeField]
    private Button endTurnButton;
    [SerializeField]
    private GameObject actionPanel;
    [SerializeField]
    private GameObject actionButtonPrefab;

    [SerializeField]
    private TextMeshProUGUI missionObjectiveLabel;

    [SerializeField] 
    private InitiativeOrderUIController initiativeOrderUIController;

    [SerializeField]
    private GameObject missionOutcomePanel;
    
    [SerializeField]
    private TextMeshProUGUI missionOutcomeLabel;

    [SerializeField]
    private AttackModeUIOverlay attackModeOverlay;
    
    public event EventHandler OnEndTurnButtonClicked;
    public event EventHandler<Weapon> OnAttackButtonClicked;

    public event EventHandler OnAttackModePreviousButtonClicked;
    public event EventHandler OnAttackModeNextButtonClicked;
    public event EventHandler OnFireButtonClicked;

    private void Start()
    {
        endTurnButton.onClick.AddListener(() => OnEndTurnButtonClicked?.Invoke(this, EventArgs.Empty));
    }

    public void SetActionPointLabel(int actionPoints)
    {
        actionPointLabel.text = "Action Points: " + actionPoints;
    }

    public void ResetInitiativeOrderUI(LinkedList<Unit> initiativeOrder)
    {
        initiativeOrderUIController.ResetInitiativeOrder(initiativeOrder);
    }

    public void OnTurnEnded()
    {
        initiativeOrderUIController.OnTurnEnded();
    }

    public void OnTurnStarted(Unit startingUnit, int roundCount)
    {
        actionPanel.SetActive(startingUnit.Team == Team.PLAYER);
        endTurnButton.gameObject.SetActive(startingUnit.Team == Team.PLAYER);
        
        if (startingUnit.Team == Team.PLAYER)
        {
            ResetActionPanel(startingUnit, roundCount);
        }
    }

    private void ResetActionPanel(Unit currentUnit, int roundCount)
    {
        foreach (Transform child in actionPanel.transform)
        {
            Destroy(child.gameObject);
        }

        foreach (var weapon in currentUnit.Weapons)
        {
            GameObject weaponButton = Instantiate(actionButtonPrefab, actionPanel.transform);
            Texture2D texture = weapon.ActionPanelButtonTexture;
            weaponButton.GetComponent<Image>().sprite = Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), ACTION_BUTTON_PIXELS_PER_UNIT);
            weaponButton.GetComponentInChildren<TextMeshProUGUI>().text = weapon.ActionPointCost.ToString();
            if (!currentUnit.CanUseWeapon(weapon, roundCount))
            {
                weaponButton.GetComponent<Button>().interactable = false;
            }
            else
            {
                weaponButton.GetComponent<Button>()?.onClick.AddListener(() => AttackButtonClicked(weapon));
            }
        }
    }

    public void OnPlayerActionTaken(int actionPointsRemaining, Unit unit, int roundCount)
    {
        SetActionPointLabel(actionPointsRemaining);
        ResetActionPanel(unit, roundCount);
    }

    private void AttackButtonClicked(Weapon weapon)
    {
        OnAttackButtonClicked?.Invoke(this, weapon);
    }

    public void DisableAttackModeOverlay()
    {
        attackModeOverlay.gameObject.SetActive(false);
        actionPanel.SetActive(true);
        endTurnButton.gameObject.SetActive(true);
    }
    
    public void InitializeAttackModeOverlay(List<Unit> targetableUnits, Unit currentlyTargetedUnit, Weapon weapon)
    {
        attackModeOverlay.gameObject.SetActive(true);
        actionPanel.SetActive(false);
        endTurnButton.gameObject.SetActive(false);
        attackModeOverlay.Initialize(targetableUnits, currentlyTargetedUnit, weapon);
    }

    public void UpdateTargetedUnit(Unit unit)
    {
        attackModeOverlay.UpdateTarget(unit);
    }

    public void AttackModeNextButtonClicked()
    {
        OnAttackModeNextButtonClicked?.Invoke(this, EventArgs.Empty);
    }
    
    public void AttackModePreviousButtonClicked()
    {
        OnAttackModePreviousButtonClicked?.Invoke(this, EventArgs.Empty);
    }

    public void AttackModeFireButtonClicked()
    {
        OnFireButtonClicked?.Invoke(this, EventArgs.Empty);
    }
    
    public void SetMissionObjectiveText(MissionObjective objective)
    {
        switch (objective)
        {
            case MissionObjective.KILL_ALL_ENEMIES:
                missionObjectiveLabel.text = "Kill All Enemies";
                break;
        }
    }

    public void SetEnabled(bool isEnabled)
    {
        foreach (Transform child in actionPanel.transform)
        {
            child.gameObject.GetComponent<Button>().interactable = isEnabled;
        }
        
        endTurnButton.interactable = isEnabled;
    }

    public void ShowOutcome(bool victory)
    {
        foreach (Transform child in gameObject.transform)
        {
            child.gameObject.SetActive(false);
        }
        
        missionOutcomePanel.SetActive(true);
        if (victory)
        {
            missionOutcomeLabel.text = "Victory";
        }
        else
        {
            missionOutcomeLabel.text = "Defeat";
        }
    }

    public void OnRestartButtonClicked()
    {
        StartCoroutine(LoadSceneAsync());
    }
    
    public void OnQuitButtonClicked()
    {
        Application.Quit();
    }
    
    IEnumerator LoadSceneAsync()
    {

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("Playtest");

        // Wait until the asynchronous scene fully loads
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }
}
