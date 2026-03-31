using UnityEngine;
using UnityEngine.UI;

public class HeadStabilizer : MonoBehaviour
{
    public Transform headBone;
    public Slider stabilizationSlider;

    [Range(0f, 1f)]
    public float stabilizationFactor = 0.5f;

    private Quaternion initialLocalRotation;

    void Start()
    {
        if (headBone != null)
        {
            initialLocalRotation = headBone.localRotation;
        }

        if (stabilizationSlider != null)
        {
            stabilizationSlider.minValue = 0f;
            stabilizationSlider.maxValue = 1f;

            stabilizationSlider.value = stabilizationFactor;

            stabilizationSlider.onValueChanged.AddListener(UpdateStabilizationFactor);
        }
    }

    public void UpdateStabilizationFactor(float newValue)
    {
        stabilizationFactor = newValue;
    }

    void LateUpdate()
    {
        if (headBone == null) return;

        headBone.localRotation = Quaternion.Slerp(headBone.localRotation, initialLocalRotation, stabilizationFactor);
    }
    void OnDestroy()
    {
        if (stabilizationSlider != null)
        {
            stabilizationSlider.onValueChanged.RemoveListener(UpdateStabilizationFactor);
        }
    }
}