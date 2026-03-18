using UnityEngine;

public class InvManager : MonoBehaviour
{
    public MonoBehaviour playerMovementScript;
    public MonoBehaviour cameraMovementScript;
    public Rigidbody playerRigidbody;
    public Animator playerAnimator;

    void OnEnable()
    {
        if (playerMovementScript != null) playerMovementScript.enabled = false;
        if (cameraMovementScript != null) cameraMovementScript.enabled = false;

        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }

        // Freeze the animation in place
        if (playerAnimator != null)
        {
            playerAnimator.speed = 0f;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void OnDisable()
    {
        if (playerMovementScript != null) playerMovementScript.enabled = true;
        if (cameraMovementScript != null) cameraMovementScript.enabled = true;

        // Resume the animation
        if (playerAnimator != null)
        {
            playerAnimator.speed = 1f;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}