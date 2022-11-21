using System;
using UnityEngine;

public class Unit : MonoBehaviour
{
    [field: SerializeField]
    public int TotalMovement { get; private set; } = 5;
    [field: SerializeField]
    public int AttackRange { get; private set; } = 10;
    [field: SerializeField]
    public int AttackDamage { get; private set; } = 5;

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
    
    public event EventHandler OnDeath;
    
    public void Start()
    {
        Health = MaxHealth;
        healthBarUI.SetHealth(Health, MaxHealth);
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
}