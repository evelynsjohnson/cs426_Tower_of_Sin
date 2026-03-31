using UnityEngine;

public class HeadStabilizer : MonoBehaviour
{
    public Transform headBone;

    [Range(0f, 1f)]
    public float stabilizationFactor = 0.5f;

    private Quaternion initialLocalRotation;

    void Start()
    {
        if (headBone != null)
        {
            initialLocalRotation = headBone.localRotation;
        }
    }

    void LateUpdate()
    {
        if (headBone == null) return;

        headBone.localRotation = Quaternion.Slerp(headBone.localRotation, initialLocalRotation, stabilizationFactor);
    }
}