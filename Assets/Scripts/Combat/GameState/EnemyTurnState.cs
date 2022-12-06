using System;
using System.Collections.Generic;
using UnityEngine;
using Object = System.Object;

public class EnemyTurnState : TurnState
{
    private float turnStartDelayTimerTime = 0.5f;
    private float turnStartDelayTimerStartTime;

    private bool turnEndDelayTimerRunning;
    private float turnEndDelayTimerTime = 0.5f;
    private float turnEndDelayTimerStartTime;

    private bool isMoving;
    private bool hasMoved;
    private bool hasAttacked;
    private bool isAttacking;
    private Unit currentTarget;
    
    
    public EnemyTurnState(MapController mapController, GameController gameController) : base(mapController, gameController, GameState.ENEMY_TURN) {}

    public override void Enter()
    {
        base.Enter();
        turnStartDelayTimerStartTime = Time.time;
        turnEndDelayTimerRunning = false;
        isMoving = false;
        hasMoved = false;
        hasAttacked = false;
        isAttacking = false;
        CurrentUnit.OnMovementComplete += OnUnitMovementComplete;
        currentTarget = null;
    }

    public override void Update()
    {
        base.Update();
        if (Time.time - turnStartDelayTimerTime < turnStartDelayTimerStartTime)
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

        if (isAttacking)
        {
            return;
        }

        // TODO: Select Weapon for enemy attack?
        // TODO: Animate Attack
        if (!hasAttacked && currentTarget != null && mapController.CanUnitAttack(CurrentUnit, currentTarget, CurrentUnit.Weapons[0]))
        {
            Attack(currentTarget, CurrentUnit.Weapons[0]);
            return;
        }

        if (!turnEndDelayTimerRunning)
        {
            turnEndDelayTimerRunning = true;
            turnEndDelayTimerStartTime = Time.time;
        }

        if (Time.time - turnEndDelayTimerStartTime > turnEndDelayTimerTime)
        {
            OnUnitTurnFinished();
        }
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
            List<MapTile> candidateTiles = mapController.GetAllTilesInRange(playerUnit.transform.position, CurrentUnit.Weapons[0].Range);
            MapTile bestTile = null;
            float lowestTravelDistance = Single.MaxValue;
            float lowestPlayerDistance = Single.MaxValue;
            foreach (var tile in candidateTiles)
            {
                if (tile == null || !tile.Walkable || tile.CurrentUnit != null || !mapController.HasLineOfSight(tile.GridPos, playerUnitPosition))
                {
                    continue;
                }

                float distanceToPlayer = Vector3.Distance(tile.GridPos, playerUnitPosition);
                float travelDistance = Vector3.Distance(tile.GridPos, unitPosition);
                
                
                if (bestTile == null || distanceToPlayer < lowestPlayerDistance || (distanceToPlayer == lowestPlayerDistance && travelDistance < lowestTravelDistance))
                {
                    bestTile = tile;
                    lowestPlayerDistance = distanceToPlayer;
                    lowestTravelDistance = travelDistance;
                }
            }

            // Trying to attack this unit is fucked, try something else....
            if (bestTile == null)
            {
                continue;
            }
            
            
            List<Vector3Int> path = mapController.GetShortestPath(unitPosition, bestTile.GridPos);
            
            // TODO: Better logic for determining which player unit to target... I.e importance, distance, whether other enemies are handling it, etc.
            if (path != null && path.Count < shortestPathLength)
            {
                shortestPath = path;
                shortestPathLength = path.Count;
                currentTarget = playerUnit.GetComponent<Unit>();
            }
        }

        // when shortestPath is null, path couldn't be found. 
        if (shortestPath == null)
        {
            Debug.Log("Could not find path to unit!");
            return;
        }

        // When path length is 2, the unit is standing next to the player unit. Movement is not necessary, so just say we've moved.
        if (shortestPathLength == 2)
        {
            hasMoved = true;
            return;
        }
        
        
        Vector3Int targetPosition = shortestPath[0];
        for (int i = 0; i <= CurrentUnit.TotalMovement; i++)
        {
            targetPosition = shortestPath[i];
            // the last value in shortest path is the tile the player is standing on, second to last is where this unit should go
            if (i == shortestPathLength - 1)
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