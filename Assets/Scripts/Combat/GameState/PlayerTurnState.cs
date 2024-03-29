﻿using System;
using System.Collections.Generic;
using UnityEngine;
using Util;

public class PlayerTurnState : TurnState
{
    private const int STARTING_ACTION_POINTS = 3;
    private const int MAX_ACTION_POINTS = 4;
    private const int DASH_ACTION_POINT_COST = 1;

    private static readonly Color MOVEMENT_INDICATOR_COLOR =  new(0.714f, 0.631f, 0.278f, 1f);
    private static readonly Color PATH_MOVEMENT_INDICATOR_COLOR = new(0.447f, 0.647f, 0.694f, 1f);
    private static readonly Color DASH_MOVEMENT_INDICATOR_COLOR = new(0.8f, 0.35f, 0.23f, 1f);

    private PlayerTurnInputState inputState;
    
    private Dictionary<Vector3Int, MapTile> tilesInMovementRange;
    private Dictionary<Vector3Int, GameObject> movementIndicators;
    private Dictionary<Vector3Int, MapTile> tilesInDashRange;
    private Vector3Int currentlyHoveredTilePosition;
    private GameObject movementActionPointCostIndicator;
    private List<GameObject> movementActionPointCollectionIndicators;

    private Dictionary<Vector3Int, GameObject> attackRangeIndicators;

    // Attack Mode vars
    private List<Unit> targetableUnits;
    private Unit currentlyTargetedUnit;
    private Weapon currentlyUsingWeapon;

    private readonly UIController uiController;
    private readonly CameraController cameraController;
    private int actionPoints;

    private int movementPool;

    public int ActionPoints
    {
        get => actionPoints;
        private set => actionPoints = value > MAX_ACTION_POINTS ? MAX_ACTION_POINTS : value;
    }

    public PlayerTurnState(MapController mapController, UIController uiController, GameController gameController, CameraController cameraController) : base(mapController, gameController, GameState.PLAYER_TURN)
    {
        this.uiController = uiController;
        this.cameraController = cameraController;
        ActionPoints = STARTING_ACTION_POINTS;
        uiController.SetActionPointLabel(ActionPoints);
        uiController.OnEndTurnButtonClicked += (caller, args) => OnUnitTurnFinished();
        uiController.OnAttackButtonClicked += (caller, weapon) => EnterAttackMode(weapon);
        uiController.OnAttackModeNextButtonClicked += (caller, args) => TargetNextUnit();
        uiController.OnAttackModePreviousButtonClicked += (caller, args) => TargetPreviousUnit();
        uiController.OnFireButtonClicked += (caller, args) => AttackModeFire();
        uiController.OnAttackModeExited += (caller, args) => EnterDefaultState();
        uiController.OnAttackButtonHovered += (caller, weapon) => InstantiateAttackRangeIndicators(weapon);
        uiController.OnAttackButtonUnhovered += (caller, weapon) => ClearAttackRangeIndicators();
    }

    public override void Enter()
    {
        base.Enter();
        inputState = PlayerTurnInputState.DEFAULT;
        movementPool = CurrentUnit.TotalMovement;
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
        // TODO: Object Pooling?
        ClearMovementIndicators();

        Vector3 unitPos = mapController.CellToWorld(CurrentUnit.CurrentTile.GridPos);
        movementIndicators = new Dictionary<Vector3Int, GameObject>();
        if (movementPool > 0)
        {
            // NOTE: THIS IS IMPORTANT FOR MOVEMENT LOGIC IN GENERAL, NOT JUST INDICATORS.
            List<MapTile> allMapTilesInMoveRange = mapController.GetAllTilesInRange(unitPos, movementPool);
        
            foreach (MapTile tile in allMapTilesInMoveRange)
            {
                GameObject indicator = GameObject.Instantiate(AddressablesManager.Instance.Get("MovementIndicator"), mapController.CellToWorld(tile.GridPos), Quaternion.identity);
                movementIndicators[tile.GridPos] = indicator;
                tilesInMovementRange[tile.GridPos] = tile;
            
                SpriteRenderer renderer = indicator.GetComponent<SpriteRenderer>();
                renderer.color = MOVEMENT_INDICATOR_COLOR;
            }
        }
      

        // Check if the player has AP to spend on dashing
        if (!CanSpendActionPoints(DASH_ACTION_POINT_COST))
        {
            return;
        }
        
        // Player can afford to dash the unit
        
        List<MapTile> allMapTilesInDashRange = mapController.GetAllTilesInRange(unitPos, movementPool + CurrentUnit.TotalMovement);
        foreach (MapTile tile in allMapTilesInDashRange)
        {
            if (tilesInMovementRange.ContainsKey(tile.GridPos))
            {
                continue;
            }

            GameObject indicator = GameObject.Instantiate(AddressablesManager.Instance.Get("MovementIndicator"), mapController.CellToWorld(tile.GridPos), Quaternion.identity);
            movementIndicators[tile.GridPos] = indicator;
            tilesInDashRange[tile.GridPos] = tile;
            
            SpriteRenderer renderer = indicator.GetComponent<SpriteRenderer>();
            renderer.color = DASH_MOVEMENT_INDICATOR_COLOR;
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
        tilesInMovementRange = new Dictionary<Vector3Int, MapTile>();
        tilesInDashRange = new Dictionary<Vector3Int, MapTile>();
        
        if (movementActionPointCostIndicator != null)
        {
            GameObject.Destroy(movementActionPointCostIndicator);
        }

        if (movementActionPointCollectionIndicators != null)
        {
            foreach (var indicator in movementActionPointCollectionIndicators)
            {
                GameObject.Destroy(indicator);
            }
        }

        movementActionPointCollectionIndicators = new List<GameObject>();

        currentlyHoveredTilePosition = new Vector3Int(Int32.MaxValue, Int32.MaxValue, Int32.MaxValue);
    }

    private void DefaultState()
    {
        if (InputUtil.IsPointerOverUIElement())
        {
            return;
        }
        
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 1));
        Vector3Int mouseCellPosition = mapController.WorldToCell(mouseWorldPosition);

        MapTile mouseHoveredTile = tilesInMovementRange.ContainsKey(mouseCellPosition) ? tilesInMovementRange[mouseCellPosition] 
                                 : tilesInDashRange.ContainsKey(mouseCellPosition) ? tilesInDashRange[mouseCellPosition] : null;
        // Player isn't hovering over a tile we can move to -- abort!
        if (mouseHoveredTile == null)
        {
            return;
        }
        
        bool needsToDash = tilesInDashRange.ContainsKey(mouseCellPosition);

        if (mouseCellPosition != currentlyHoveredTilePosition)
        {
            currentlyHoveredTilePosition = mouseCellPosition;
            if (movementActionPointCostIndicator != null)
            {
                GameObject.Destroy(movementActionPointCostIndicator);
            }

            if (movementActionPointCollectionIndicators != null)
            {
                foreach (var indicator in movementActionPointCollectionIndicators)
                {
                    GameObject.Destroy(indicator);
                }
            }

            movementActionPointCollectionIndicators = new List<GameObject>();

            List<Vector3Int> pathToHoveredTile = mapController.GetShortestPath(CurrentUnit.CurrentTile.GridPos, mouseCellPosition);
            Dictionary<Vector3Int, int> tilesInPath = new Dictionary<Vector3Int, int>(pathToHoveredTile.Count);

            bool foundFirstDashPosition = false;
            Vector3Int firstDashPosition = Vector3Int.zero;
            Vector3Int lastMovementTileBeforeDash = Vector3Int.zero;

            Dictionary<Vector3Int, Vector3Int> firstCollectedPositionsBySourcePositions = new Dictionary<Vector3Int, Vector3Int>();

            for (int i = 0; i < pathToHoveredTile.Count; i++)
            {
                Vector3Int cellPosition = pathToHoveredTile[i];
                tilesInPath[cellPosition] = i;
                if (!foundFirstDashPosition && tilesInDashRange.ContainsKey(cellPosition))
                {
                    firstDashPosition = cellPosition;
                    // at i == 0, it's the unit's current position, which should not be in tilesInDashRange, so we can't get indexoutofbounds
                    lastMovementTileBeforeDash = pathToHoveredTile[i - 1];
                    foundFirstDashPosition = true;
                }

                MapTile tile = mapController.GetTileAtGridCellPosition(cellPosition);
                if (tile.ActionPointsGainedOnEntry > 0 && tile.ActionPointSource != null && !firstCollectedPositionsBySourcePositions.ContainsKey(tile.ActionPointSource.GridPos))
                {
                    firstCollectedPositionsBySourcePositions[tile.ActionPointSource.GridPos] = cellPosition;
                }
            }
            
            if (needsToDash && foundFirstDashPosition)
            {
                Vector3 position = Vector3.Lerp(mapController.CellToWorld(lastMovementTileBeforeDash), mapController.CellToWorld(firstDashPosition), 0.5f);
                movementActionPointCostIndicator = GameObject.Instantiate(AddressablesManager.Instance.Get("MovementCostIndicator"), position , Quaternion.identity);
                movementActionPointCostIndicator.GetComponent<CostIndicator>().SetText("-" + DASH_ACTION_POINT_COST + " AP", Color.red);
            }

            foreach (var (sourcePosition, firstEncounterPos) in firstCollectedPositionsBySourcePositions)
            {
                GameObject indicator = GameObject.Instantiate(AddressablesManager.Instance.Get("MovementCostIndicator"), mapController.CellToWorld(firstEncounterPos), Quaternion.identity);
                CostIndicator text = indicator.GetComponent<CostIndicator>();
                text.SetText(
                    "+" + mapController.GetTileAtGridCellPosition(firstEncounterPos).ActionPointsGainedOnEntry + " AP",
                    Color.white);
                
                movementActionPointCollectionIndicators.Add(indicator);
            }
            
            foreach (var pair in movementIndicators)
            {
                SpriteRenderer renderer = pair.Value.GetComponent<SpriteRenderer>();
                renderer.color = tilesInDashRange.ContainsKey(pair.Key) ? DASH_MOVEMENT_INDICATOR_COLOR : MOVEMENT_INDICATOR_COLOR;
                if (tilesInPath.ContainsKey(pair.Key))
                {
                    renderer.color = PATH_MOVEMENT_INDICATOR_COLOR;
                }
            }
        }
        
        
        if (Input.GetButtonDown("Select"))
        {
            // This shouldn't happen, but just in case
            if (needsToDash && !CanSpendActionPoints(DASH_ACTION_POINT_COST))
            {
                Debug.LogError("Player Attempted to dash but had insufficient action points -- The movement indicators should never have been created!");
                return;
            }
            
            List<Vector3Int> path = mapController.GetShortestPath(CurrentUnit.CurrentTile.GridPos, mouseCellPosition);
            int pathLength = path.Count - 1; // path includes starting position;
            if (needsToDash)
            {
                SpendActionPoints(DASH_ACTION_POINT_COST);
                movementPool += CurrentUnit.TotalMovement;
            }
            
            movementPool -= pathLength;
            
            gameController.MoveUnit(CurrentUnit, mouseHoveredTile.GridPos, OnUnitMovementComplete);

            EnterWaitingForMovementState();
        }
    }

    private void OnUnitMovementComplete()
    {
        EnterDefaultState();
    }
    
    private void EnterAttackMode(Weapon weapon)
    {
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
            ClearAttackRangeIndicators();
            ClearMovementIndicators();
            uiController.InitializeAttackModeOverlay(targetableUnits, targetableUnits[0], weapon);
            SetCurrentlyTargetedUnit(targetableUnits[0]);
        }
    }

    private void EnterDefaultState()
    {
        inputState = PlayerTurnInputState.DEFAULT;
        targetableUnits = null;
        cameraController.SmoothMoveToThenUnlock(CurrentUnit);
        uiController.DisableAttackModeOverlay();
        uiController.SetInteractablesEnabled(true, CurrentUnit, gameController.RoundCount);
        UpdateMovementIndicators();
    }

    private void EnterWaitingForMovementState()
    {
        inputState = PlayerTurnInputState.WAITING_FOR_MOVE;
        targetableUnits = null;
        cameraController.FollowUnit(CurrentUnit);
        uiController.DisableAttackModeOverlay();
        ClearMovementIndicators();
        uiController.SetInteractablesEnabled(false, CurrentUnit, gameController.RoundCount);
    }

    private void EnterWaitingForAttackState(Unit target)
    {
        inputState = PlayerTurnInputState.WAITING_FOR_ATTACK;
        targetableUnits = null;
        cameraController.FollowUnit(target);
        uiController.DisableAttackModeOverlay();
        ClearMovementIndicators();
        uiController.SetInteractablesEnabled(false, CurrentUnit, gameController.RoundCount);
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

    private void InstantiateAttackRangeIndicators(Weapon weapon)
    {
        ClearAttackRangeIndicators();

        List<MapTile> allMapTilesInRange = mapController.GetAllTilesInAttackRange(CurrentUnit.CurrentTile.GridPos, weapon.Range);
        
        Vector3 offset = 2.6f * Vector3.forward;
        foreach (MapTile tile in allMapTilesInRange)
        {
            if (mapController.HasLineOfSight(CurrentUnit.CurrentTile.GridPos, tile.GridPos))
            {
                attackRangeIndicators[tile.GridPos] = GameObject.Instantiate(AddressablesManager.Instance.Get("AttackIndicator"), mapController.CellToWorld(tile.GridPos) + offset, Quaternion.identity);
            }
        }

        String reason = null;
        
        if (!CurrentUnit.CanUseWeapon(weapon, gameController.RoundCount))
        {
            reason = "Weapon On Cooldown";
        } else if (!CanSpendActionPoints(weapon.ActionPointCost))
        {
            reason = "Insufficient Action Points";
        }
        else
        {
            GameObject[] enemyUnits = GameObject.FindGameObjectsWithTag("EnemyUnit");
            bool unitsInRange = false;
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
                    unitsInRange = true;
                    break;
                }
            }

            if (!unitsInRange)
            {
                reason = "No Enemies In Range";
            }
        }
        
        if (reason != null)
        {
            uiController.SetAttackImpossibleLabelActive(true);
            uiController.SetAttackImpossibleReason(reason);
        }
    }

    private void ClearAttackRangeIndicators()
    {
        if (attackRangeIndicators != null)
        {
            foreach (var indicator in attackRangeIndicators)
            {
                GameObject.Destroy(indicator.Value);
            }
        }

        attackRangeIndicators = new Dictionary<Vector3Int, GameObject>();
        
        
        uiController.SetAttackImpossibleLabelActive(false);
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
        SpendActionPoints(currentlyUsingWeapon.ActionPointCost);
        CurrentUnit.StartAttack(currentlyUsingWeapon, currentlyTargetedUnit, gameController.RoundCount, OnAttackFinished);
        EnterWaitingForAttackState(currentlyTargetedUnit);
    }

    private void OnAttackFinished()
    {
        EnterDefaultState();
    }

    public override void OnUnitDeath(Unit unit)
    {
        base.OnUnitDeath(unit);
        if (unit.Team == Team.ENEMY)
        {
            ActionPoints++;
            gameController.RunAfterDelay(
                () => uiController.SpawnTextPopupAtPosition("+1 AP",
                    CurrentUnit.transform.position + new Vector3(0f, 0.8f, 0f), Color.white), 0.8f);
            uiController.OnPlayerActionTaken(actionPoints, CurrentUnit, gameController.RoundCount);
        }
    }

    public void AddActionPoints(int amount)
    {
        ActionPoints += amount;
        if (CurrentUnit == null)
        {
            uiController.SetActionPointLabel(ActionPoints);
        }
        else
        {
            uiController.OnPlayerActionTaken(ActionPoints, CurrentUnit, gameController.RoundCount);
        }
    }

    private enum PlayerTurnInputState
    {
        DEFAULT,
        WAITING_FOR_MOVE,
        ATTACK,
        WAITING_FOR_ATTACK,
    }
}