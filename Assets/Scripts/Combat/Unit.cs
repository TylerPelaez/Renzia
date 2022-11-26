using System;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    [field: SerializeField]
    public string Name { get; private set; }
    
    [field: SerializeField]
    public int TotalMovement { get; private set; } = 5;
    [field: SerializeField]
    public Weapon[] Weapons { get; private set; }

    private Dictionary<string, int> WeaponLastFiredRoundCount;

    [field: SerializeField]
    public int MaxHealth { get; private set; } = 5;
    [field: SerializeField]
    public int Health { get; private set; } = 0;
    
    [field: SerializeField]
    public int Initiative { get; private set; } = 10;
    
    [field: SerializeField]
    public Team Team { get; private set; } = Team.PLAYER;

    public MapTile CurrentTile { get; set; }

    public HealthBarUI healthBarUI;

    public Texture2D initiativeOrderPortrait;

    private Animator animator;

    private const float TOLERANCE = 0.05f;
    private const float MOVEMENT_SPEED_UNITS_PER_SECOND = 1.2f;
    private LinkedList<Vector3> movementList;
    private Vector3 movementCurrentPosition;
    private bool moving;
    private float currentTileMovementStartTime;
    private float currentTileMovementTotalTime;

    public event EventHandler OnDeath;
    public event EventHandler OnMovementComplete;
    
    public void Start()
    {
        Health = MaxHealth;
        healthBarUI.SetHealth(Health, MaxHealth);
        WeaponLastFiredRoundCount = new Dictionary<string, int>();
        animator = GetComponent<Animator>();
    }

    public void TakeDamage(int amount)
    {
        Health -= amount;
        healthBarUI.SetHealth(Health, MaxHealth);
        if (Health <= 0)
        {
            OnDeath?.Invoke(this, EventArgs.Empty);
            // TODO: stuff on death
            Destroy(gameObject);
        }
    }

    public void Attack(Weapon weapon, Unit target, int currentRoundCount)
    {
        target.TakeDamage(weapon.RollDamage());
        WeaponLastFiredRoundCount[weapon.Name] = currentRoundCount;
    }

    public bool CanUseWeapon(Weapon weapon, int currentRoundCount)
    {
        return !WeaponLastFiredRoundCount.ContainsKey(weapon.Name) || currentRoundCount - WeaponLastFiredRoundCount[weapon.Name] >= weapon.TurnCooldown;
    }

    public void StartMove(List<Vector3> path)
    {
        movementList = new LinkedList<Vector3>(path);
        moving = true;
        CurrentMovementFinished();
    }

    private void Update()
    {
        if (moving)
        {
            float t = (Time.time - currentTileMovementStartTime) / currentTileMovementTotalTime;
            bool currentMovementFinished = false;
            if (t >= 1f)
            {
                t = 1f;
                currentMovementFinished = true;
            }

            Vector3 interpolatedPosition = Vector3.Lerp(movementCurrentPosition, movementList.First.Value, t);
            transform.position = interpolatedPosition;

            if (currentMovementFinished)
            {
                CurrentMovementFinished();
            }
        }
    }


    private void CurrentMovementFinished()
    { 
        movementCurrentPosition = movementList.First.Value;
        movementList.RemoveFirst();
        if (movementList.Count == 0)
        {
            moving = false;
            animator.Play("Idle");
            OnMovementComplete?.Invoke(this, EventArgs.Empty);
            return;
        }

        Vector3 targetPosition = movementList.First.Value;
        float distance = Vector3.Distance(targetPosition, movementCurrentPosition);
        currentTileMovementTotalTime = distance / MOVEMENT_SPEED_UNITS_PER_SECOND;
        currentTileMovementStartTime = Time.time;
        
        // Change animation direction if needed
        string targetAnimatorStateName;
        if (Math.Abs(targetPosition.x - movementCurrentPosition.x) < TOLERANCE)
        {
            if (targetPosition.y > movementCurrentPosition.y)
            {
                targetAnimatorStateName = "Run7";
            }
            else
            {
                targetAnimatorStateName = "Run3";
            }
        }
        else if (Math.Abs(targetPosition.y - movementCurrentPosition.y) < TOLERANCE)
        {
            if (targetPosition.x > movementCurrentPosition.x)
            {
                targetAnimatorStateName = "Run1";
            }
            else
            {
                targetAnimatorStateName = "Run5";
            }
        }
        else if (targetPosition.x > movementCurrentPosition.x)
        {
            if (targetPosition.y > movementCurrentPosition.y)
            {
                targetAnimatorStateName = "Run0";
            }
            else
            {
                targetAnimatorStateName = "Run2";
            }
        }
        else
        {
            if (targetPosition.y > movementCurrentPosition.y)
            {
                targetAnimatorStateName = "Run6";
            }
            else
            {
                targetAnimatorStateName = "Run4";
            }
        }
        
        animator.Play(targetAnimatorStateName);
    }
}