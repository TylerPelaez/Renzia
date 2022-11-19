using UnityEngine;

public class EnemyUnitUnselectedState : UnitUnselectedState
{
    public EnemyUnitUnselectedState(TurnState turnFSM) : base(turnFSM) {}

    public override void Update()
    {
        base.Update();
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("EnemyUnit");
        foreach (var enemy in enemies)
        {
            // Just grab first enemy, TODO: initiative order anyway???
            Unit unit = enemy.GetComponent<Unit>();
            if (unit != null)
            {
                SelectUnit(unit);
                return;
            }
        }
    }
}