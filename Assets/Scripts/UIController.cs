using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI actionPointLabel;
    [SerializeField]
    private Button endTurnButton;
    [SerializeField]
    private Button attackButton;

    [SerializeField]
    private TextMeshProUGUI missionObjectiveLabel;

    [SerializeField] 
    private InitiativeOrderUIController initiativeOrderUIController;
    
    public event EventHandler OnEndTurnButtonClicked;
    public event EventHandler OnAttackButtonClicked;
    
    private void Start()
    {
        endTurnButton.onClick.AddListener(() => OnEndTurnButtonClicked?.Invoke(this, EventArgs.Empty));
        attackButton.onClick.AddListener(() => OnAttackButtonClicked?.Invoke(this, EventArgs.Empty));
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

    public void SetMissionObjectiveText(MissionObjective objective)
    {
        switch (objective)
        {
            case MissionObjective.KILL_ALL_ENEMIES:
                missionObjectiveLabel.text = "Kill All Enemies";
                break;
        }
    }
}
