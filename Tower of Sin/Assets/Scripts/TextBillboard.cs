using UnityEngine;

public class TextBillboard : MonoBehaviour
{
    public Transform camTransform;
    public bool lockYAxis = true;

    void Start()
    {
        if (camTransform == null && Camera.main != null)
        {
            camTransform = Camera.main.transform;
        }
    }

    void LateUpdate()
    {
        if (camTransform == null) return;

        if (lockYAxis)
        {
            Vector3 targetPosition = transform.position + camTransform.forward;
            targetPosition.y = transform.position.y;
            transform.LookAt(targetPosition);
        }
        else
        {
            transform.LookAt(transform.position + camTransform.forward);
        }
    }
}