using System.Collections.Generic;
using UnityEngine;
using Util;

public class PlayerUnitSelectedState : UnitSelectedState
{
    protected List<MapTile> tilesInMovementRange;
    private List<GameObject> movementIndicators;
    private List<Unit> targetableUnits;

    private bool hasMoved;
    
    public PlayerUnitSelectedState(PlayerTurnState turnFSM, MapController mapController) : base(turnFSM, mapController) {}

    public override void Enter()
    {
        base.Enter();
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
        targetableUnits = null;
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

        Vector3 unitPos = SelectedUnit.gameObject.transform.position;
        
        tilesInMovementRange = mapController.GetAllTilesInRange(unitPos, SelectedUnit.TotalMovement);

        Vector3 offset = 2 * Vector3.forward;
        movementIndicators = new List<GameObject>(tilesInMovementRange.Count);
        foreach (MapTile tile in tilesInMovementRange)
        {
            movementIndicators.Add(GameObject.Instantiate(AddressablesManager.Instance.Get("MovementIndicator"), mapController.CellToWorld(tile.GridPos) + offset, Quaternion.identity));
        }
    }
    
    public override void Update()
    {
        base.Update();

        if (Input.GetButtonDown("End Turn"))
        {
            turnFSM.OnUnitTurnFinished();
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
                
                // Attack enemy
                if (!turnFSM.CanSpendActionPoints(1) )
                {
                    return;
                }

                
                if (mapController.CanUnitAttack(SelectedUnit, unit))
                {
                    unit.TakeDamage(SelectedUnit.AttackDamage);
                    SpendActionPoints(1);
                    UpdateMovementIndicators(); // Might've killed someone
                }

                return;
            }

            // First Move per turn is free, next one costs 1 AP
            if (hasMoved && !turnFSM.CanSpendActionPoints(1))
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
                    SelectedUnit.gameObject.transform.position = mapController.CellToWorld(tile.GridPos) + new Vector3(0, 0, 2);
                    mapController.MoveUnit(SelectedUnit, tile.GridPos);
                    if (hasMoved)
                    {
                        SpendActionPoints(1);
                    }
                    else
                    {
                        hasMoved = true;
                        UpdateMovementIndicators();
                    }
                    return;
                }
            }
        }
    }

    private void SpendActionPoints(int amount)
    {
        turnFSM.SpendActionPoints(amount);
    }
}