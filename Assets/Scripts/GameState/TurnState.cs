public class TurnState : State<GameState>
{
    protected FiniteStateMachine<TurnStates> turnStateMachine = new FiniteStateMachine<TurnStates>();
    protected MapController mapController;
    private GameController gameController;
    
    public Unit SelectedUnit { get; private set; }

    public TurnState(MapController mapController, GameController gameController, GameState gameState) : base(gameState) 
    {
        this.mapController = mapController;
        this.gameController = gameController;
        turnStateMachine.Add(new State<TurnStates>(TurnStates.TRANSITION));
    }

    public override void Enter()
    {
        base.Enter();
        OnUnitSelected(gameController.GetCurrentTurnUnit());
    }

    public override void Exit()
    {
        base.Exit();
        turnStateMachine.SetCurrentState(TurnStates.TRANSITION);
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
        SelectedUnit = selected;
        turnStateMachine.SetCurrentState(TurnStates.UNIT_SELECTED);
    }

    public void OnUnitTurnFinished()
    {
        SelectedUnit = null;
        gameController.OnUnitTurnFinished();
    }

    
    public virtual bool CanSpendActionPoints(int amount)
    {
        return false;
    }
    public virtual void SpendActionPoints(int amount)
    {
        return;
    }
}