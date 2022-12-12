using UnityEngine;

public class Effect : MonoBehaviour
{
    public void OnComplete()
    {
        Destroy(gameObject);
    }
}
