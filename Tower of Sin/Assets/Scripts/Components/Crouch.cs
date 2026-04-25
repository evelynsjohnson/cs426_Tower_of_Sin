using System.Collections;
using UnityEngine;

public class Crouch : MonoBehaviour
{
    public KeyCode key = KeyCode.LeftControl;

    [Header("Movement")]
    public FirstPersonMovement movement;
    public float crouchSpeed = 2f;

    [Header("Camera Attached To Hips")]
    public Transform cameraTransform;
    public Vector3 crouchLocalOffset = new Vector3(0f, -0.08f, 0f);
    public float smoothTime = 0.12f;

    private Vector3 standingLocalPos;
    private Vector3 targetLocalPos;
    private Vector3 cameraVelocity;
    private bool hasSavedStandingPos;

    [Header("Collider")]
    public CapsuleCollider colliderToLower;
    public float standingHeight = 2f;
    public Vector3 standingCenter = new Vector3(0f, 1f, 0f);
    public float crouchHeight = 1.4f;
    public Vector3 crouchCenter = new Vector3(0f, 0.7f, 0f);

    public bool IsCrouched { get; private set; }
    public event System.Action CrouchStart, CrouchEnd;

    void Start()
    {
        StartCoroutine(SaveStandingCameraAfterFrame());
    }

    IEnumerator SaveStandingCameraAfterFrame()
    {
        yield return null;

        if (cameraTransform != null)
        {
            standingLocalPos = cameraTransform.localPosition;
            targetLocalPos = standingLocalPos;
            hasSavedStandingPos = true;
        }
    }

    void Update()
    {
        if (!hasSavedStandingPos || cameraTransform == null) return;

        bool wantsCrouch = Input.GetKey(key);

        if (wantsCrouch)
        {
            if (!IsCrouched)
            {
                IsCrouched = true;
                SetSpeedOverride(true);
                ApplyCrouchCollider();
                CrouchStart?.Invoke();
            }

            targetLocalPos = standingLocalPos + crouchLocalOffset;
        }
        else
        {
            if (IsCrouched)
            {
                IsCrouched = false;
                SetSpeedOverride(false);
                ApplyStandCollider();
                CrouchEnd?.Invoke();
            }

            targetLocalPos = standingLocalPos;
        }

        cameraTransform.localPosition = Vector3.SmoothDamp(
            cameraTransform.localPosition,
            targetLocalPos,
            ref cameraVelocity,
            smoothTime
        );
    }

    void ApplyCrouchCollider()
    {
        if (colliderToLower == null) return;
        colliderToLower.height = crouchHeight;
        colliderToLower.center = crouchCenter;
    }

    void ApplyStandCollider()
    {
        if (colliderToLower == null) return;
        colliderToLower.height = standingHeight;
        colliderToLower.center = standingCenter;
    }

    void SetSpeedOverride(bool crouched)
    {
        if (!movement) return;

        if (crouched)
        {
            if (!movement.speedOverrides.Contains(SpeedOverride))
                movement.speedOverrides.Add(SpeedOverride);
        }
        else
        {
            if (movement.speedOverrides.Contains(SpeedOverride))
                movement.speedOverrides.Remove(SpeedOverride);
        }
    }

    float SpeedOverride() => crouchSpeed;
}