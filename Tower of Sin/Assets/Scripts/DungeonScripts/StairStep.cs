using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class StairStep : MonoBehaviour
{
    public float stepHeight = 0.7f; 
    public float stepSmooth = 0.5f;      // how fast we step up
    public float rayDistance = 1f;     // how far forward to check

    public Transform lowerRayOrigin;
    public Transform upperRayOrigin;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        TryStep(Vector3.forward);
        TryStep((Vector3.forward + Vector3.right).normalized);
        TryStep((Vector3.forward - Vector3.right).normalized);
    }

    void TryStep(Vector3 direction)
    {
        Vector3 worldDir = transform.TransformDirection(direction);

        RaycastHit lowerHit;
        RaycastHit upperHit;

        // LOW RAY (detect obstacle)
        if (Physics.Raycast(lowerRayOrigin.position, worldDir, out lowerHit, rayDistance))
        {
            if (lowerHit.collider.CompareTag("Staircase"))
            {
                // HIGH RAY (check clearance)
                if (!Physics.Raycast(upperRayOrigin.position, worldDir, out upperHit, rayDistance))
                {
                    rb.position += new Vector3(0f, stepSmooth, 0f);
                }
            }
        }
    }
}