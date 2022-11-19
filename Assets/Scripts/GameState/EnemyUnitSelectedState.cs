using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyUnitSelectedState : UnitSelectedState
{
    public EnemyUnitSelectedState(TurnState turnFSM, MapController mapController) : base(turnFSM, mapController) {}

    public override void Update()
    {
        base.Update();


        Vector3Int unitPosition = mapController.WorldToCell(SelectedUnit.transform.position);
        GameObject[] playerUnits = GameObject.FindGameObjectsWithTag("PlayerUnit");


        List<Vector3Int> shortestPath = null;
        int shortestPathLength = Int32.MaxValue;
        // Find closest player unit to attack
        foreach (var playerUnit in playerUnits)
        {
            Vector3Int playerUnitPosition = mapController.WorldToCell(playerUnit.transform.position);
            List<Vector3Int> path = mapController.GetShortestPath(unitPosition, playerUnitPosition);
            
            // TODO: Better logic for determining which player unit to target... I.e importance, distance, whether other enemies are handling it, etc.
            if (path != null && path.Count < shortestPathLength)
            {
                shortestPath = path;
                shortestPathLength = path.Count;
            }
        }

        // when shortestPath is null, path couldn't be found. When it's 2, the unit is standing next to the player unit anyway
        if (shortestPath == null || shortestPathLength == 2)
        {
            Debug.Log("Could not find path to unit!");
            turnFSM.OnUnitTurnFinished();
            return;
        }
        
        Vector3Int targetPosition = shortestPath[0];
        for (int i = 0; i <= SelectedUnit.totalMovement; i++)
        {
            targetPosition = shortestPath[i];
            // the last value in shortest path is the tile the player is standing on, second to last is where this unit should go
            if (i == shortestPathLength - 2)
            {
                break;
            }
        }

        SelectedUnit.transform.position = mapController.CellToWorld(targetPosition) + new Vector3(0, 0, 2);
        turnFSM.OnUnitTurnFinished();
    }
}