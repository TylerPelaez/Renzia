public class TurnState : State<GameState>
{
    protected MapController mapController;
    protected GameController gameController;
    
    public Unit CurrentUnit { get; private set; }

    public TurnState(MapController mapController, GameController gameController, GameState gameState) : base(gameState) 
    {
        this.mapController = mapController;
        this.gameController = gameController;
    }

    public override void Enter()
    {
        base.Enter();
        OnUnitTurnStarted(gameController.GetCurrentTurnUnit());
    }

    protected void OnUnitTurnStarted(Unit unit)
    {
        CurrentUnit = unit;
    }

    protected void OnUnitTurnFinished()
    {
        CurrentUnit = null;
        gameController.OnUnitTurnFinished();
    }
}