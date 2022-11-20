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
    [field: SerializeField] public int Health { get; private set; } = 0;
    
    public void Start()
    {
        Health = MaxHealth;
    }

    public void TakeDamage(int amount)
    {
        Health -= amount;
        if (Health <= 0)
        {
            // TODO: stuff on death
            Destroy(gameObject);
        }
    }
}