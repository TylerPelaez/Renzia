using UnityEngine.AddressableAssets;
using UnityEngine;
using System.Collections.Generic;

public class UnitSelectedState : State<PlayerTurn>
{
    private PlayerTurnState turnFSM;
    private MapController mapController;
    private GameObject movementIndicatorPrefab;

    public Unit SelectedUnit { get; private set; }

    public UnitSelectedState(PlayerTurnState turnFSM, MapController mapController) : base(PlayerTurn.UNIT_SELECTED)
    {
        this.turnFSM = turnFSM;
        this.mapController = mapController;
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
        SelectedUnit = turnFSM.SelectedUnit;
        Vector3 unitPos = SelectedUnit.gameObject.transform.position;

        // TODO: Fiz Z coord here!
        Vector3Int cellPos = mapController.WorldToCell(new Vector3(unitPos.x, unitPos.y, unitPos.z - 2));
        List<MapTile> movementPositions = mapController.GetAllTilesInRange(cellPos, SelectedUnit.totalMovement, false);

        Vector3 offset = 2 * Vector3.forward;
        foreach (MapTile tile in movementPositions)
        {
            GameObject indicator = GameObject.Instantiate(movementIndicatorPrefab, mapController.CellToWorld(tile.GridPos) + offset, Quaternion.identity);
        }
    }

    public override void Update()
    {
        base.Update();
    }
}
