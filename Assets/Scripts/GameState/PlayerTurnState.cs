using TMPro;
using UnityEngine;

public class PlayerTurnState : TurnState
{
    private TextMeshProUGUI actionPointLabel;
    
    private int maxActionPoints = 4;
    public int ActionPoints { get; private set; }
    
    public PlayerTurnState(MapController mapController, GameController gameController) : base(mapController, gameController, GameState.PLAYER_TURN)
    {
        actionPointLabel = gameController.actionPointLabel;
        turnStateMachine.Add(new PlayerUnitSelectedState(this, mapController));
        ActionPoints = maxActionPoints;
        SetActionPointLabel();
    }

    public override bool CanSpendActionPoints(int points)
    {
        return points <= ActionPoints;
    }
    
    public override void SpendActionPoints(int points)
    {
        if (points > ActionPoints)
        {
            Debug.LogError("Not enough action points!!");
            return;
        }

        ActionPoints -= points;
        SetActionPointLabel();
    }

    private void SetActionPointLabel()
    {
        actionPointLabel.text = "Action Points: " + ActionPoints;
    }
}