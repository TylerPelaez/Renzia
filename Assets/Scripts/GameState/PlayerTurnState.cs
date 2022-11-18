using UnityEngine;

public class PlayerTurnState : State<GameState>
{
    private FiniteStateMachine<PlayerTurn> turnStateMachine = new FiniteStateMachine<PlayerTurn>();
    public Unit SelectedUnit { get; private set; }
    private MapController mapController; 

    public PlayerTurnState(MapController mapController) : base(GameState.PLAYER_TURN) 
    {
        this.mapController = mapController;
        turnStateMachine.Add(new UnitUnselectedState(this));
        turnStateMachine.Add(new UnitSelectedState(this, mapController));
        turnStateMachine.SetCurrentState(PlayerTurn.UNIT_UNSELECTED);
    }

    public override void Enter()
    {
        base.Enter();
    }

    public override void Exit()
    {
        base.Exit();
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
            turnStateMachine.SetCurrentState(PlayerTurn.UNIT_UNSELECTED);
            return; // TODO: Deselect is working?
        }

        SelectedUnit = selected;
        turnStateMachine.SetCurrentState(PlayerTurn.UNIT_SELECTED);
    }
}