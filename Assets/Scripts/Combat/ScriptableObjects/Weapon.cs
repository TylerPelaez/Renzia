using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/Weapon", order = 1)]
public class Weapon : ScriptableObject
{
    [field: SerializeField]
    public string Name { get; private set; }
    
    [field: SerializeField]
    public int MinDamage { get; private set; }
    
    [field: SerializeField]
    public int MaxDamage { get; private set; }

    [field: SerializeField] public int Range { get; private set; } = 10;
    
    [field: Range(0f, 1f)]
    [field: SerializeField]
    public float CritChance { get; private set; }

    [field: SerializeField] public int ActionPointCost { get; private set; } = 1;

    [field: SerializeField] public int TurnCooldown { get; private set; } = 1;
    
    [field: SerializeField] public Texture2D ActionPanelButtonTexture { get; private set; }

    public int RollDamage()
    {
        var random = new System.Random();
        int damage = random.Next(MinDamage, MaxDamage + 1);
        bool crit = random.NextDouble() <= CritChance;
        return crit ? (int) (damage * 1.5f) : damage;
    }
}
