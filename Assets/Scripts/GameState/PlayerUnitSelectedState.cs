using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class PlayerUnitSelectedState : UnitSelectedState
{
    private GameObject movementIndicatorPrefab;
    protected List<MapTile> tilesInMovementRange;
    private List<GameObject> movementIndicators;
    private List<Unit> targetableUnits;


    public PlayerUnitSelectedState(PlayerTurnState turnFSM, MapController mapController) : base(turnFSM, mapController)
    {
        Addressables.LoadAssetAsync<GameObject>("MovementIndicator").Completed += OnLoadDone;
    }
    
    private void OnLoadDone(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<GameObject> obj)
    {
        if (obj.Result == null)
        {
            Debug.LogError("Unable to Load Movement Indicator");
        }
        movementIndicatorPrefab = obj.Result;
    }

    public override void Enter()
    {
        base.Enter();
        Vector3 unitPos = SelectedUnit.gameObject.transform.position;
        
        tilesInMovementRange = mapController.GetAllTilesInRange(unitPos, SelectedUnit.TotalMovement, false);

        Vector3 offset = 2 * Vector3.forward;
        movementIndicators = new List<GameObject>(tilesInMovementRange.Count);
        foreach (MapTile tile in tilesInMovementRange)
        {
            movementIndicators.Add(GameObject.Instantiate(movementIndicatorPrefab, mapController.CellToWorld(tile.GridPos) + offset, Quaternion.identity));
        }
    }

    public override void Exit()
    {
        base.Exit();
        foreach (var indicator in movementIndicators)
        {
            GameObject.Destroy(indicator);    
        }

        movementIndicators = null;
        targetableUnits = null;
    }

    public override void Update()
    {
        base.Update();
        
        if (Input.GetButtonDown("Select"))
        {
            // if an enemy was clicked on, then see if attackable first
            Collider2D overlap = Physics2D.OverlapPoint(Camera.main.ScreenToWorldPoint(Input.mousePosition), LayerMask.GetMask("EnemyUnit"));
            if (overlap != null && overlap.gameObject != null)
            {
                Unit unit = overlap.gameObject.GetComponent<Unit>();
                if (unit == null)
                {
                    Debug.LogError("Enemy selected unit but there is no Unit Component!");
                    return;
                }
                
                // Attack enemy
                if (!turnFSM.CanSpendActionPoints(1) )
                {
                    return;
                }

                
                if (mapController.CanUnitAttack(SelectedUnit, unit))
                {
                    unit.TakeDamage(SelectedUnit.AttackDamage);
                    turnFSM.SpendActionPoints(1);
                    turnFSM.OnUnitTurnFinished();
                }

                return;
            }
            
            // otherwise, try moving to the place clicked on
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 1));
            Vector3Int cellPosition = mapController.WorldToCell(worldPos);
            foreach (var tile in tilesInMovementRange)
            {
                if (tile.GridPos == cellPosition)
                {
                    // valid position, move the unit there and end selected state for testing
                    SelectedUnit.gameObject.transform.position = mapController.CellToWorld(tile.GridPos) + new Vector3(0, 0, 2);
                    turnFSM.OnUnitTurnFinished();
                }
            }
        }
    }
}