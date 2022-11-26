using System.Collections.Generic;
using UnityEngine;
using Util;

public class PlayerTurnState : TurnState
{
    private PlayerTurnInputState inputState;
    private bool hasMoved;
    
    private List<MapTile> tilesInMovementRange;
    private List<GameObject> movementIndicators;

    private List<Unit> targetableUnits;
    private Unit currentlyTargetedUnit;

    private readonly UIController uiController;
    private const int MAX_ACTION_POINTS = 4;
    private const int MOVEMENT_ACTION_POINT_COST = 1;
    
    public int ActionPoints { get; private set; }

    public PlayerTurnState(MapController mapController, GameController gameController) : base(mapController, gameController, GameState.PLAYER_TURN)
    {
        uiController = gameController.uiController;
        ActionPoints = MAX_ACTION_POINTS;
        uiController.SetActionPointLabel(ActionPoints);
        uiController.OnEndTurnButtonClicked += (caller, args) => OnUnitTurnFinished();
        uiController.OnAttackButtonClicked += (caller, args) => EnterAttackMode(0);
    }

    public override void Enter()
    {
        base.Enter();
        inputState = PlayerTurnInputState.DEFAULT;
        hasMoved = false;
        UpdateMovementIndicators();
    }

    public override void Exit()
    {
        base.Exit();
        foreach (var indicator in movementIndicators)
        {
            GameObject.Destroy(indicator);    
        }

        movementIndicators = null;
    }

    public override void Update()
    {
        base.Update();

        switch (inputState)
        {
            case PlayerTurnInputState.DEFAULT:
                DefaultState();
                break;
            case PlayerTurnInputState.ATTACK:
                AttackState();
                break;
        }
    }
    
    private bool CanSpendActionPoints(int points)
    {
        return points <= ActionPoints;
    }
    
    private void SpendActionPoints(int points)
    {
        if (points > ActionPoints)
        {
            Debug.LogError("Not enough action points!!");
            return;
        }

        ActionPoints -= points;
        uiController.SetActionPointLabel(ActionPoints);
    }
    private void UpdateMovementIndicators()
    {
        // TODO: Pooling? maybe not needed if this isn't the final method used for movement
        if (movementIndicators != null)
        {
            foreach (var indicator in movementIndicators)
            {
                GameObject.Destroy(indicator);
            }
        }

        Vector3 unitPos = mapController.CellToWorld(CurrentUnit.CurrentTile.GridPos);
        
        tilesInMovementRange = mapController.GetAllTilesInRange(unitPos, CurrentUnit.TotalMovement);

        Vector3 offset = 1.9f * Vector3.forward;
        movementIndicators = new List<GameObject>(tilesInMovementRange.Count);
        foreach (MapTile tile in tilesInMovementRange)
        {
            movementIndicators.Add(GameObject.Instantiate(AddressablesManager.Instance.Get("MovementIndicator"), mapController.CellToWorld(tile.GridPos) + offset, Quaternion.identity));
        }
    }

    private void EnterAttackMode(int weaponIndex)
    {
        Weapon weapon = CurrentUnit.Weapons[weaponIndex];
        if (!CanSpendActionPoints(weapon.ActionPointCost))
        {
            return;
        }
        
        GameObject[] enemyUnits = GameObject.FindGameObjectsWithTag("EnemyUnit");
        targetableUnits = new List<Unit>();
        foreach (var enemyUnit in enemyUnits)
        {
            Unit unit = enemyUnit.GetComponent<Unit>();
            if (unit == null)
            {
                Debug.LogError("EnemyUnit tagged game object doesn't have a Unit Component");
                continue;
            }

            if (mapController.CanUnitAttack(CurrentUnit, unit, weapon))
            {
                targetableUnits.Add(unit);
            }
        }

        if (targetableUnits.Count > 0)
        {
            inputState = PlayerTurnInputState.ATTACK;
            currentlyTargetedUnit = targetableUnits[0];
        }
    }

    private void DefaultState()
    {
          if (Input.GetButtonDown("End Turn"))
        {
            OnUnitTurnFinished();
        }
        else if (Input.GetButtonDown("Select"))
        {
            // if an enemy was clicked on, then see if attackable first
            Collider2D overlap = Physics2D.OverlapPoint(Camera.main.ScreenToWorldPoint(Input.mousePosition), LayerMask.GetMask("EnemyUnit"));
            if (overlap != null && overlap.gameObject != null)
            {
                Unit unit = overlap.gameObject.GetComponent<Unit>();
                if (unit == null)
                {
                    Debug.LogError("Enemy unit selected but there is no Unit Component!");
                    return;
                }

                // TODO: Debug purpose only - this will all be gone soon anyway
                Weapon weapon = CurrentUnit.Weapons[0];
                
                
                // Attack enemy
                if (!CanSpendActionPoints(weapon.ActionPointCost) )
                {
                    return;
                }

                if (mapController.CanUnitAttack(CurrentUnit, unit, weapon))
                {
                    CurrentUnit.Attack(weapon, unit, gameController.RoundCount);
                    SpendActionPoints(weapon.ActionPointCost);
                    UpdateMovementIndicators(); // Might've killed someone
                }

                return;
            }

            // First Move per turn is free, next one costs AP
            if (hasMoved && !CanSpendActionPoints(MOVEMENT_ACTION_POINT_COST))
            {
                return;
            }
            
            // otherwise, try moving to the place clicked on
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 1));
            Vector3Int cellPosition = mapController.WorldToCell(worldPos);
            foreach (var tile in tilesInMovementRange)
            {
                if (tile.GridPos == cellPosition)
                {
                    gameController.MoveUnit(CurrentUnit, tile.GridPos);
                    if (hasMoved)
                    {
                        SpendActionPoints(MOVEMENT_ACTION_POINT_COST);
                    }

                    hasMoved = true;
                    UpdateMovementIndicators();
                    return;
                }
            }
        }
    }
    
    private void AttackState()
    {
        // We have a list of targetable units. Now we just need to displa
    }

    private enum PlayerTurnInputState
    {
        DEFAULT,
        ATTACK
    }
}