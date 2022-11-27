﻿using System;
using System.Collections.Generic;
using UnityEngine;
using Object = System.Object;

public class EnemyTurnState : TurnState
{
    private float waitTimer = 0.5f;
    private float waitTimeStarted;

    private bool isMoving;
    private bool hasMoved;
    private bool hasAttacked;
    private Unit currentTarget;
    
    
    public EnemyTurnState(MapController mapController, GameController gameController) : base(mapController, gameController, GameState.ENEMY_TURN) {}

    public override void Enter()
    {
        base.Enter();
        waitTimeStarted = Time.time;
        isMoving = false;
        hasMoved = false;
        hasAttacked = false;
        CurrentUnit.OnMovementComplete += OnUnitMovementComplete;
        currentTarget = null;
    }

    public override void Update()
    {
        base.Update();
        if (Time.time - waitTimer < waitTimeStarted)
        {
            return;
        }

        if (isMoving)
        {
            return;
        }

            // TODO: Allow this to happen in a different order
        if (!hasMoved)
        {
            Move();
            return;
        }

        // TODO: Select Weapon for enemy attack?
        // TODO: Animate Attack
        if (!hasAttacked && currentTarget != null)
        {
            Attack(currentTarget, CurrentUnit.Weapons[0]);
            return;
        }

        OnUnitTurnFinished();
    }

    private void Move()
    {
        Vector3Int unitPosition = CurrentUnit.CurrentTile.GridPos;
        GameObject[] playerUnits = GameObject.FindGameObjectsWithTag("PlayerUnit");


        List<Vector3Int> shortestPath = null;
        int shortestPathLength = Int32.MaxValue;
        // Find closest player unit to attack
        foreach (var playerUnit in playerUnits)
        {
            Vector3Int playerUnitPosition = playerUnit.GetComponent<Unit>().CurrentTile.GridPos;
            List<Vector3Int> path = mapController.GetShortestPath(unitPosition, playerUnitPosition);
            
            // TODO: Better logic for determining which player unit to target... I.e importance, distance, whether other enemies are handling it, etc.
            if (path != null && path.Count < shortestPathLength)
            {
                shortestPath = path;
                shortestPathLength = path.Count;
                currentTarget = playerUnit.GetComponent<Unit>();
            }
        }

        // when shortestPath is null, path couldn't be found. When it's 2, the unit is standing next to the player unit anyway
        if (shortestPath == null || shortestPathLength == 2)
        {
            Debug.Log("Could not find path to unit!");
            return;
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

        gameController.MoveUnit(CurrentUnit, targetPosition);
        isMoving = true;
    }

    private void Attack(Unit target, Weapon weaponUsed)
    {
        if (!mapController.CanUnitAttack(CurrentUnit, target, weaponUsed))
        {
            return;
        }
        
        CurrentUnit.Attack(weaponUsed, target, gameController.RoundCount);
        // TODO: Animate attack and set this when animation is complete
        hasAttacked = true;
    }

    private void OnUnitMovementComplete(Object caller, EventArgs args)
    {
        CurrentUnit.OnMovementComplete -= OnUnitMovementComplete;
        hasMoved = true;
        isMoving = false;
    }
}