using UnityEngine;

public class BillboardToPlayer : MonoBehaviour
{
    public Transform playerTransform;
    public bool lockVertical = true;

    void Awake()
    {
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
        }
    }

    void LateUpdate()
    {
        if (playerTransform == null) return;

        Vector3 targetPosition = playerTransform.position;

        if (lockVertical)
        {
            targetPosition.y = transform.position.y;
        }

        transform.LookAt(targetPosition);

        transform.Rotate(0, 180, 0);
    }
}