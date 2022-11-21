public class EnemyTurnState : TurnState
{
    public EnemyTurnState(MapController mapController, GameController gameController) : base(mapController, gameController, GameState.ENEMY_TURN) 
    {
        turnStateMachine.Add(new EnemyUnitSelectedState(this, mapController));
    }
}