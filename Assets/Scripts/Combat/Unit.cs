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

    private Dictionary<string, int> weaponLastFiredRoundCount;

    [field: SerializeField]
    public int MaxHealth { get; private set; } = 5;
    [field: SerializeField]
    public int Health { get; private set; } = 0;
    
    [field: SerializeField]
    public int Initiative { get; private set; } = 10;
    
    [field: SerializeField]
    public Team Team { get; private set; } = Team.PLAYER;

    public MapTile CurrentTile { get; set; }

    public Texture2D initiativeOrderPortrait;

    public GameObject deathEffect;
    
    private Animator animator;

    public float movementSpeedUnitsPerSecond = 1.2f;
    private LinkedList<Vector3> movementList;
    private Vector3 movementCurrentPosition;
    private float currentTileMovementStartTime;
    private float currentTileMovementTotalTime;
    private int lastMovementDirection = -1;

    private UnitState state = UnitState.DEFAULT;
    private Action onMovementCompleteCallback;

    private Action onAttackCallback;
    private Action onAttackCompleteCallback;
    
    public EventHandler<HealthChangedEventArgs> OnHealthChanged;
    public EventHandler<Vector3> OnTileEntered;

    public event EventHandler OnDeath;
    
    public void Awake()
    {
        Health = MaxHealth;
        weaponLastFiredRoundCount = new Dictionary<string, int>();
        animator = GetComponent<Animator>();
    }
    
    public void TakeDamage(int amount, bool wasCrit)
    {
        int startingHealth = Health;
        Health = Mathf.Max(0, startingHealth - amount);
        OnHealthChanged?.Invoke(this, new HealthChangedEventArgs
        {
            ChangeAmount = startingHealth - Health,
            NewValue = Health,
            MaxHealth = MaxHealth,
            WasCrit = wasCrit
        });
        if (Health <= 0)
        {
            OnDeath?.Invoke(this, EventArgs.Empty);
            GameObject deathEffect = Instantiate(this.deathEffect, transform.position, Quaternion.identity);
            deathEffect.GetComponent<SpriteRenderer>().sprite = GetComponent<SpriteRenderer>().sprite;
            Destroy(gameObject);
        }
        else
        {
            animator.Play("Hurt");
        }
    }

    public void StartAttack(Weapon weapon, Unit target, int currentRoundCount, Action completionCallback)
    {
        weaponLastFiredRoundCount[weapon.Name] = currentRoundCount;
        int animationDirection = DirectionUtil.GetAnimationSuffixForDirection(transform.position, target.transform.position);
        
        onAttackCallback = () => DoAttack(weapon, target, completionCallback);
        onAttackCompleteCallback = () =>
        {
            completionCallback?.Invoke();
            animator.Play("Idle" + animationDirection);
        };

        animator.Play("Attack" + animationDirection);
    }

    public void Attack()
    {
        onAttackCallback?.Invoke();
    }

    public void AttackAnimationComplete()
    {
        onAttackCompleteCallback?.Invoke();
    }
    
    private void DoAttack(Weapon weapon, Unit target, Action completionCallback)
    {
        bool wasCrit;
        target.TakeDamage(weapon.RollDamage(out wasCrit), wasCrit );
    }

    public bool CanUseWeapon(Weapon weapon, int currentRoundCount)
    {
        return !weaponLastFiredRoundCount.ContainsKey(weapon.Name) || currentRoundCount - weaponLastFiredRoundCount[weapon.Name] >= weapon.TurnCooldown;
    }

    public void StartMove(List<Vector3> path, Action onComplete)
    {
        movementList = new LinkedList<Vector3>(path);
        state = UnitState.MOVING;
        onMovementCompleteCallback = onComplete;
        CurrentMovementFinished();
    }

    private void Update()
    {
        switch (state)
        {
            case UnitState.MOVING:
                MoveState();
                break;
            case UnitState.ATTACKING:
                AttackState();
                break;
        }
    }

    private void MoveState()
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

    private void AttackState() {}

    private void CurrentMovementFinished()
    { 
        movementCurrentPosition = movementList.First.Value;
        OnTileEntered?.Invoke(this, movementCurrentPosition);
        
        movementList.RemoveFirst();
        if (movementList.Count == 0)
        {
            state = UnitState.DEFAULT;
            animator.Play("Idle" + (lastMovementDirection < 0 ? 3 : lastMovementDirection));
            onMovementCompleteCallback?.Invoke();
            return;
        }

        Vector3 targetPosition = movementList.First.Value;
        float distance = Vector3.Distance(targetPosition, movementCurrentPosition);
        currentTileMovementTotalTime = distance / movementSpeedUnitsPerSecond;
        currentTileMovementStartTime = Time.time;
        
        // Change animation direction if needed
        lastMovementDirection = DirectionUtil.GetAnimationSuffixForDirection(movementCurrentPosition, targetPosition);
        string targetAnimatorStateName = "Run" + lastMovementDirection;
        animator.Play(targetAnimatorStateName);
    }
    
    private enum UnitState
    {
        MOVING,
        ATTACKING,
        DEFAULT,
    }

    public class HealthChangedEventArgs : EventArgs
    {
        public int ChangeAmount { get; set; }
        public int NewValue { get; set;  }
        public int MaxHealth { get; set;  }
        
        public bool WasCrit { get; set; }
    }
}