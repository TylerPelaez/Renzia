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
    
    public event EventHandler OnEndTurnButtonClicked;
    public event EventHandler OnAttackButtonClicked;
    
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

    public void OnTurnStarted(Unit startingUnit)
    {
        actionPanel.SetActive(startingUnit.Team == Team.PLAYER);
        endTurnButton.gameObject.SetActive(startingUnit.Team == Team.PLAYER);
        
        if (startingUnit.Team == Team.PLAYER)
        {
            foreach (Transform child in actionPanel.transform)
            {
                Destroy(child.gameObject);
            }

            foreach (var weapon in startingUnit.Weapons)
            {
                GameObject weaponButton = Instantiate(actionButtonPrefab, actionPanel.transform);
                Texture2D texture = weapon.ActionPanelButtonTexture;
                weaponButton.GetComponent<Image>().sprite = Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), ACTION_BUTTON_PIXELS_PER_UNIT);
                
                weaponButton.GetComponent<Button>()?.onClick.AddListener(() => OnAttackButtonClicked?.Invoke(this, EventArgs.Empty));
            }
        }
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
