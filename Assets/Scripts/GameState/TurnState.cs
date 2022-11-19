public class TurnState : State<GameState>
{
    protected FiniteStateMachine<TurnStates> turnStateMachine = new FiniteStateMachine<TurnStates>();
    protected MapController mapController;
    protected GameController gameController;
    
    public Unit SelectedUnit { get; private set; }

    public TurnState(MapController mapController, GameController gameController, GameState gameState) : base(gameState) 
    {
        this.mapController = mapController;
        this.gameController = gameController;
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();
        turnStateMachine.FixedUpdate();
    }

    public override void Update()
    {
        base.Update();
        turnStateMachine.Update();
    }

    public void OnUnitSelected(Unit selected)
    {
        if (selected == null)
        {
            SelectedUnit = null;
            turnStateMachine.SetCurrentState(TurnStates.UNIT_UNSELECTED);
            return; // TODO: Deselect is working?
        }

        SelectedUnit = selected;
        turnStateMachine.SetCurrentState(TurnStates.UNIT_SELECTED);
    }

    public virtual void OnUnitTurnFinished()
    {
        SelectedUnit = null;
        turnStateMachine.SetCurrentState(TurnStates.UNIT_UNSELECTED);
    }
}