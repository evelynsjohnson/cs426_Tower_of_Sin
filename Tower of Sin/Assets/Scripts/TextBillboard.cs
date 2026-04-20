using UnityEngine;

public class BillboardToPlayer : MonoBehaviour
{
    public Transform playerTransform;
    public bool lockVertical = true;

    [Range(0f, 180f)]
    public float visibleAngle = 100f;

    [Range(0f, 89f)]
    public float maxBillboardTurn = 30f;
    public bool invertFacing = true;

    private Vector3 baseForward;
    private Quaternion baseRotation;

    private Renderer rend;

    void Awake()
    {
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
        }

        baseRotation = transform.rotation;
        baseForward = invertFacing ? -transform.forward : transform.forward;

        if (lockVertical)
        {
            baseForward.y = 0f;
            baseForward.Normalize();
        }

        rend = GetComponent<Renderer>();
    }

    void LateUpdate()
    {
        if (playerTransform == null || rend == null) return;

        Vector3 toPlayer = playerTransform.position - transform.position;

        if (lockVertical)
            toPlayer.y = 0f;

        if (toPlayer.sqrMagnitude < 0.0001f)
        {
            rend.enabled = false;
            return;
        }

        Vector3 toPlayerDir = toPlayer.normalized;

        float halfAngle = visibleAngle * 0.5f;
        float dotThreshold = Mathf.Cos(halfAngle * Mathf.Deg2Rad);
        float dot = Vector3.Dot(baseForward, toPlayerDir);

        bool insideCone = dot >= dotThreshold;

        rend.enabled = insideCone;

        if (!insideCone)
        {
            transform.rotation = baseRotation;
            return;
        }

        Vector3 clampedDir = Vector3.RotateTowards(
            baseForward,
            toPlayerDir,
            Mathf.Deg2Rad * maxBillboardTurn,
            0f
        );

        if (lockVertical)
        {
            clampedDir.y = 0f;
            clampedDir.Normalize();
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(clampedDir, Vector3.up) *
            Quaternion.Euler(0, 180f, 0);

        transform.rotation = targetRotation;
    }
}