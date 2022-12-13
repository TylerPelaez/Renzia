using UnityEngine;

public class MovementIndicator : MonoBehaviour
{
    private static readonly float Z_VALUE = 2.5f;
    
    // Start is called before the first frame update
    void Start()
    {
        Vector3 position = transform.position;
        transform.position = new Vector3(position.x, position.y, Z_VALUE);
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 position = transform.position;
        transform.position = new Vector3(position.x, position.y, Z_VALUE);
    }
}
