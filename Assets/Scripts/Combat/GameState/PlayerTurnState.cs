﻿using System.Collections.Generic;
using UnityEngine;
using Util;

public class PlayerTurnState : TurnState
{
    private PlayerTurnInputState inputState;
    private bool hasMoved;
    
    private Dictionary<Vector3Int, MapTile> tilesInMovementRange;
    private Dictionary<Vector3Int, GameObject> movementIndicators;
    private Vector3Int currentlyHoveredTilePosition;
    private GameObject movementActionPointCostIndicator;

    // Attack Mode vars
    private List<Unit> targetableUnits;
    private Unit currentlyTargetedUnit;
    private Weapon currentlyUsingWeapon;

    private readonly UIController uiController;
    private readonly CameraController cameraController;
    
    private const int MAX_ACTION_POINTS = 4;
    private const int MOVEMENT_ACTION_POINT_COST = 1;
    
    public int ActionPoints { get; private set; }

    public PlayerTurnState(MapController mapController, UIController uiController, GameController gameController, CameraController cameraController) : base(mapController, gameController, GameState.PLAYER_TURN)
    {
        this.uiController = uiController;
        this.cameraController = cameraController;
        ActionPoints = MAX_ACTION_POINTS;
        uiController.SetActionPointLabel(ActionPoints);
        uiController.OnEndTurnButtonClicked += (caller, args) => OnUnitTurnFinished();
        uiController.OnAttackButtonClicked += (caller, args) => EnterAttackMode(0);
        uiController.OnAttackModeNextButtonClicked += (caller, args) => TargetNextUnit();
        uiController.OnAttackModePreviousButtonClicked += (caller, args) => TargetPreviousUnit();
        uiController.OnFireButtonClicked += (caller, args) => AttackModeFire();
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
        ClearMovementIndicators();
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
        uiController.OnPlayerActionTaken(ActionPoints, CurrentUnit, gameController.RoundCount);
    }
    private void UpdateMovementIndicators()
    {
        // TODO: Pooling? maybe not needed if this isn't the final method used for movement
        ClearMovementIndicators();

        Vector3 unitPos = mapController.CellToWorld(CurrentUnit.CurrentTile.GridPos);
        
        // NOTE: THIS IS IMPORTANT FOR MOVEMENT LOGIC IN GENERAL, NOT JUST INDICATORS.
        List<MapTile> allMapTilesInRange = mapController.GetAllTilesInRange(unitPos, CurrentUnit.TotalMovement);
        tilesInMovementRange = new Dictionary<Vector3Int, MapTile>();
        
        Vector3 offset = 1.5f * Vector3.forward;
        movementIndicators = new Dictionary<Vector3Int, GameObject>(allMapTilesInRange.Count);
        foreach (MapTile tile in allMapTilesInRange)
        {
            movementIndicators[tile.GridPos] = GameObject.Instantiate(AddressablesManager.Instance.Get("MovementIndicator"), mapController.CellToWorld(tile.GridPos) + offset, Quaternion.identity);
            tilesInMovementRange[tile.GridPos] = tile;
        }
    }

    private void ClearMovementIndicators()
    {
        if (movementIndicators != null)
        {
            foreach (var indicator in movementIndicators)
            {
                GameObject.Destroy(indicator.Value);
            }
        }
    }

    private void DefaultState()
    {
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 1));
        Vector3Int mouseCellPosition = mapController.WorldToCell(mouseWorldPosition);
        
        if (mouseCellPosition != currentlyHoveredTilePosition && tilesInMovementRange.ContainsKey(mouseCellPosition))
        {
            currentlyHoveredTilePosition = mouseCellPosition;
            if (movementActionPointCostIndicator != null)
            {
                GameObject.Destroy(movementActionPointCostIndicator);
            }

            if (hasMoved)
            {
                movementActionPointCostIndicator = GameObject.Instantiate(
                    AddressablesManager.Instance.Get("MovementCostIndicator"), mapController.CellToWorld(mouseCellPosition), Quaternion.identity);
            }

            List<Vector3Int> pathToHoveredTile = mapController.GetShortestPath(CurrentUnit.CurrentTile.GridPos, mouseCellPosition);
            HashSet<Vector3Int> tilesInPath = new HashSet<Vector3Int>(pathToHoveredTile);

            foreach (var pair in movementIndicators)
            {
                SpriteRenderer renderer = pair.Value.GetComponent<SpriteRenderer>();
                Color newColor = tilesInPath.Contains(pair.Key) ? Color.yellow : Color.white;
                newColor.a = renderer.color.a;
                renderer.color = newColor;
            }
        }
        
        
        if (Input.GetButtonDown("Select"))
        {
            // First Move per turn is free, next one costs AP
            if (hasMoved && !CanSpendActionPoints(MOVEMENT_ACTION_POINT_COST))
            {
                return;
            }
            
            // otherwise, try moving to the place clicked on
            foreach (var positionAndTile in tilesInMovementRange)
            {
                MapTile tile = positionAndTile.Value;
                if (tile.GridPos == mouseCellPosition)
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
            currentlyUsingWeapon = weapon;
            ClearMovementIndicators();
            uiController.InitializeAttackModeOverlay(targetableUnits, targetableUnits[0], weapon);
            SetCurrentlyTargetedUnit(targetableUnits[0]);
        }
    }

    private void EnterDefaultState()
    {
        inputState = PlayerTurnInputState.DEFAULT;
        targetableUnits = null;
        cameraController.Unlock();
        cameraController.MoveTo(CurrentUnit.transform.position);
        uiController.DisableAttackModeOverlay();
        UpdateMovementIndicators();
    }

    private void TargetNextUnit()
    {
        int currentUnitIndex = 0;
        for (int i = 0; i < targetableUnits.Count; i++)
        {
            if (targetableUnits[i] == currentlyTargetedUnit)
            {
                currentUnitIndex = i;
                break;
            }
        }

        currentUnitIndex++;
        if (currentUnitIndex >= targetableUnits.Count)
        {
            currentUnitIndex = 0;
        }
        
        SetCurrentlyTargetedUnit(targetableUnits[currentUnitIndex]);
    }

    private void TargetPreviousUnit()
    {
        int currentUnitIndex = 0;
        for (int i = 0; i < targetableUnits.Count; i++)
        {
            if (targetableUnits[i] == currentlyTargetedUnit)
            {
                currentUnitIndex = i;
                break;
            }
        }

        currentUnitIndex--;
        if (currentUnitIndex < 0)
        {
            currentUnitIndex = targetableUnits.Count - 1;
        }
        
        SetCurrentlyTargetedUnit(targetableUnits[currentUnitIndex]);
    }
    
    private void SetCurrentlyTargetedUnit(Unit unit)
    {
        currentlyTargetedUnit = unit;
        cameraController.FollowUnit(unit);
        uiController.UpdateTargetedUnit(unit);
    }
    
    private void AttackState()
    {
        if (Input.GetButtonDown("Next Selection"))
        {
            TargetNextUnit();
        }
        else if (Input.GetButtonDown("Previous Selection"))
        {
            TargetPreviousUnit();
        }
        else if (Input.GetButtonDown("Confirm"))
        {
            AttackModeFire();
        }
        else if (Input.GetButtonDown("Cancel"))
        {
            EnterDefaultState();
        }
    }

    private void AttackModeFire()
    {
        CurrentUnit.Attack(currentlyUsingWeapon, currentlyTargetedUnit, gameController.RoundCount);
        SpendActionPoints(currentlyUsingWeapon.ActionPointCost);
        EnterDefaultState();
    }

    private enum PlayerTurnInputState
    {
        DEFAULT,
        ATTACK
    }
}