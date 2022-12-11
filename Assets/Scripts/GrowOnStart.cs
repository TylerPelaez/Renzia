using UnityEngine;

public class GrowOnStart : MonoBehaviour
{
    public AnimationCurve curve;
    
    public float growTime = 0.2f;
    
    private float creationTime;
    
    // Start is called before the first frame update
    void Start()
    {
        transform.localScale = Vector3.one * curve.Evaluate(0);
        creationTime = Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        float t = (Time.time - creationTime) / growTime;
        transform.localScale = Vector3.one * curve.Evaluate(t);
        if (t >= 1)
        {
            Destroy(this);
        }
    }
}
