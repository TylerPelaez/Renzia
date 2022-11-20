using UnityEngine;

public class PlayerTurnState : TurnState
{
    private int maxActionPoints = 4;
    public int ActionPoints { get; private set; }
    
    public PlayerTurnState(MapController mapController, GameController gameController) : base(mapController, gameController, GameState.PLAYER_TURN) 
    {
        turnStateMachine.Add(new PlayerUnitUnselectedState(this));
        turnStateMachine.Add(new PlayerUnitSelectedState(this, mapController));
        ActionPoints = maxActionPoints;
    }

    public override void Enter()
    {
        base.Enter();
        // Debug
        turnStateMachine.SetCurrentState(TurnStates.UNIT_UNSELECTED);
    }

    public override void OnUnitTurnFinished()
    {
        base.OnUnitTurnFinished();
        gameController.OnPlayerTurnFinished();
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
        return;
    }
}