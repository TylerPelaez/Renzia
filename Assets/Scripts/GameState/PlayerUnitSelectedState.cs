using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class PlayerUnitSelectedState : UnitSelectedState
{
    private GameObject movementIndicatorPrefab;
    protected List<MapTile> tilesInMovementRange;
    private List<GameObject> movementIndicators;


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
        
        tilesInMovementRange = mapController.GetAllTilesInRange(unitPos, SelectedUnit.totalMovement, false);

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

        movementIndicators = new List<GameObject>();
    }

    public override void Update()
    {
        base.Update();
        
        if (Input.GetButtonDown("Select"))
        {
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