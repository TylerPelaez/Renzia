public class PlayerTurnState : TurnState
{
    public PlayerTurnState(MapController mapController, GameController gameController) : base(mapController, gameController, GameState.PLAYER_TURN) 
    {
        turnStateMachine.Add(new PlayerUnitUnselectedState(this));
        turnStateMachine.Add(new PlayerUnitSelectedState(this, mapController));
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
}