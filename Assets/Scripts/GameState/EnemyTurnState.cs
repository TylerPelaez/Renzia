public class EnemyTurnState : TurnState
{
    public EnemyTurnState(MapController mapController, GameController gameController) : base(mapController, gameController, GameState.ENEMY_TURN) 
    {
        turnStateMachine.Add(new EnemyUnitUnselectedState(this));
        turnStateMachine.Add(new EnemyUnitSelectedState(this, mapController));
    }

    public override void Enter()
    {
        base.Enter();
        // Debug
        turnStateMachine.SetCurrentState(TurnStates.UNIT_UNSELECTED);
    }
}