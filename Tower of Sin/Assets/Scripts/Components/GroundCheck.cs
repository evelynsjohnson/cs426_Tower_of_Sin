using UnityEngine;

public class GroundCheck : MonoBehaviour
{
    [Tooltip("Maximum distance from the ground.")]
    public float distanceThreshold = 0.5f;

    [Tooltip("Layers counted as ground.")]
    public LayerMask groundMask = ~0;

    [Tooltip("Whether this transform is grounded now.")]
    public bool isGrounded = true;

    public event System.Action Grounded;

    private const float OriginOffset = 0.01f;

    Vector3 RaycastOrigin => transform.position + Vector3.up * OriginOffset;
    float RaycastDistance => distanceThreshold + OriginOffset;

    void Update()
    {
        bool wasGrounded = isGrounded;

        bool isGroundedNow = Physics.Raycast(
            RaycastOrigin,
            Vector3.down,
            out RaycastHit hit,
            RaycastDistance,
            groundMask,
            QueryTriggerInteraction.Ignore
        );

        if (isGroundedNow && !wasGrounded)
        {
            Grounded?.Invoke();
        }

        isGrounded = isGroundedNow;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = isGrounded ? Color.white : Color.red;
        Gizmos.DrawLine(RaycastOrigin, RaycastOrigin + Vector3.down * RaycastDistance);
    }
}