﻿using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyTurnState : TurnState
{
    public EnemyTurnState(MapController mapController, GameController gameController) : base(mapController, gameController, GameState.ENEMY_TURN) {}
    
    public override void Update()
    {
        base.Update();
        Unit target = Move();
        if (target != null)
        {
            // TODO: weapon selection for enemies
            Attack(target, CurrentUnit.Weapons[0]);
        }
        
        // TODO: Animate stuff so enemy turn doesn't finish immediately.
        OnUnitTurnFinished();
    }

    private Unit Move()
    {
        
        Vector3Int unitPosition = mapController.WorldToCell(CurrentUnit.transform.position);
        GameObject[] playerUnits = GameObject.FindGameObjectsWithTag("PlayerUnit");


        List<Vector3Int> shortestPath = null;
        int shortestPathLength = Int32.MaxValue;
        Unit target = null;
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
                target = playerUnit.GetComponent<Unit>();
            }
        }

        // when shortestPath is null, path couldn't be found. When it's 2, the unit is standing next to the player unit anyway
        if (shortestPath == null || shortestPathLength == 2)
        {
            Debug.Log("Could not find path to unit!");
            return target;
        }
        
        Vector3Int targetPosition = shortestPath[0];
        for (int i = 0; i <= CurrentUnit.TotalMovement; i++)
        {
            targetPosition = shortestPath[i];
            // the last value in shortest path is the tile the player is standing on, second to last is where this unit should go
            if (i == shortestPathLength - 2)
            {
                break;
            }
        }

        CurrentUnit.transform.position = mapController.CellToWorld(targetPosition) + new Vector3(0, 0, 2);
        mapController.MoveUnit(CurrentUnit, targetPosition);
        
        return target;
    }

    private void Attack(Unit target, Weapon weaponUsed)
    {
        if (!mapController.CanUnitAttack(CurrentUnit, target, weaponUsed))
        {
            return;
        }
        
        CurrentUnit.Attack(weaponUsed, target, gameController.RoundCount);
    }
}