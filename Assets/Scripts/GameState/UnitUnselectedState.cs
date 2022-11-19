public class UnitUnselectedState : State<TurnStates>
{
    protected TurnState turnFSM;

    public UnitUnselectedState(TurnState turnFSM) : base(TurnStates.UNIT_UNSELECTED)
    {
        this.turnFSM = turnFSM;
    }

    protected void SelectUnit(Unit unit)
    {
        turnFSM.OnUnitSelected(unit);
    }
}