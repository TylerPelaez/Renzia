using UnityEngine;

public class PlayerUnitUnselectedState : UnitUnselectedState
{
    public PlayerUnitUnselectedState(TurnState turnFSM) : base(turnFSM) {}
    
    public override void Update()
    {
        base.Update();
        if (Input.GetButtonDown("Select"))
        {
            Collider2D overlap = Physics2D.OverlapPoint(Camera.main.ScreenToWorldPoint(Input.mousePosition), LayerMask.GetMask("PlayerUnit"));
            if (overlap != null && overlap.gameObject != null)
            {
                Unit unit = overlap.gameObject.GetComponent<Unit>();
                if (unit == null)
                {
                    Debug.LogError("Player selected unit but there is no Unit Component!");
                }

                SelectUnit(unit);
            }
        }
    }
}
