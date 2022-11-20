public class UnitSelectedState : State<TurnStates>
{
    protected TurnState turnFSM;
    protected MapController mapController;

    public Unit SelectedUnit { get; private set; }

    public UnitSelectedState(TurnState turnFSM, MapController mapController) : base(TurnStates.UNIT_SELECTED)
    {
        this.turnFSM = turnFSM;
        this.mapController = mapController;
    }

    public override void Enter()
    {
        base.Enter();
        SelectedUnit = turnFSM.SelectedUnit;
    }
}
