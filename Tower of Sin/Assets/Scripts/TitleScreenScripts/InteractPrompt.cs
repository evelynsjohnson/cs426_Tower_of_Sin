using UnityEngine;

public class InteractPrompt : MonoBehaviour
{
    [Header("Settings")]
    public Transform player;
    public float activationDistance = 3f;
    public bool lockYAxis = true;

    [Header("Visibility Cone")]
    [Range(0f, 180f)]
    public float visibleAngle = 100f;

    [Header("Billboard")]
    [Range(0f, 89f)]
    public float maxBillboardTurn = 35f; // max degrees it can rotate from its original facing
    public bool invertFacing = true;

    private SpriteRenderer spriteRenderer;
    private Vector3 baseForward;
    private Quaternion baseRotation;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (player == null && Camera.main != null)
            player = Camera.main.transform;

        baseRotation = transform.rotation;
        baseForward = invertFacing ? -transform.forward : transform.forward;

        if (lockYAxis)
        {
            baseForward.y = 0f;
            if (baseForward.sqrMagnitude > 0.0001f)
                baseForward.Normalize();
        }
    }

    void LateUpdate()
    {
        if (player == null || spriteRenderer == null) return;

        Vector3 toPlayer = player.position - transform.position;

        if (lockYAxis)
            toPlayer.y = 0f;

        float distance = toPlayer.magnitude;

        if (distance > activationDistance || toPlayer.sqrMagnitude < 0.0001f)
        {
            spriteRenderer.enabled = false;
            transform.rotation = baseRotation;
            return;
        }

        Vector3 toPlayerDir = toPlayer.normalized;

        float halfAngle = visibleAngle * 0.5f;
        float dotThreshold = Mathf.Cos(halfAngle * Mathf.Deg2Rad);
        float dot = Vector3.Dot(baseForward, toPlayerDir);

        bool insideCone = dot >= dotThreshold;
        spriteRenderer.enabled = insideCone;

        if (!insideCone)
        {
            transform.rotation = baseRotation;
            return;
        }

        ApplyLimitedBillboard(toPlayerDir);
    }

    void ApplyLimitedBillboard(Vector3 toPlayerDir)
    {
        Vector3 clampedDir = Vector3.RotateTowards(
            baseForward,
            toPlayerDir,
            Mathf.Deg2Rad * maxBillboardTurn,
            0f
        );

        Quaternion targetRotation;

        if (lockYAxis)
        {
            clampedDir.y = 0f;
            if (clampedDir.sqrMagnitude < 0.0001f)
            {
                transform.rotation = baseRotation;
                return;
            }

            clampedDir.Normalize();
            targetRotation = Quaternion.LookRotation(clampedDir, Vector3.up) * Quaternion.Euler(0, 180f, 0);
        }
        else
        {
            targetRotation = Quaternion.LookRotation(clampedDir);
        }

        transform.rotation = targetRotation;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, activationDistance);

        Vector3 forward = Application.isPlaying
            ? baseForward
            : (invertFacing ? -transform.forward : transform.forward);

        if (lockYAxis)
        {
            forward.y = 0f;
            if (forward.sqrMagnitude > 0.0001f)
                forward.Normalize();
        }

        float halfAngle = visibleAngle * 0.5f;
        Vector3 left = Quaternion.Euler(0f, -halfAngle, 0f) * forward;
        Vector3 right = Quaternion.Euler(0f, halfAngle, 0f) * forward;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + left * activationDistance);
        Gizmos.DrawLine(transform.position, transform.position + right * activationDistance);
    }
}