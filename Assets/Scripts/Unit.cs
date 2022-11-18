using UnityEngine;

public class Unit : MonoBehaviour
{
    public int totalMovement = 5;
    private int remainingMovement;
    

    // Use this for initialization
    void Start()
    {
        Reset();
    }

    private void Reset()
    {
        remainingMovement = totalMovement;
    }
}