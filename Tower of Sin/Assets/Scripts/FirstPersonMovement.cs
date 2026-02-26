using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioSource))]
public class FirstPersonMovement : MonoBehaviour
{
    public float speed = 5;

    [Header("Running")]
    public bool canRun = true;
    public bool IsRunning { get; private set; }
    public float runSpeed = 9;
    public KeyCode runningKey = KeyCode.LeftShift;

    [Header("Animation Controls")]
    [Tooltip("Drag your character model here if the script can't find the Animator automatically.")]
    public Animator animator;
    public KeyCode crouchKey = KeyCode.LeftControl;
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode slashKey = KeyCode.Mouse0;

    [Header("Combat & Audio")]
    public float attackRange = 2.5f;
    [Tooltip("Assign your Main Camera here so the raycast shoots from your view.")]
    public Transform cameraTransform;

    [Tooltip("How long is your PlayerSlash animation in seconds?")]
    public float slash1AnimDuration = 1.0f;
    [Tooltip("How long is your PlayerSlash2 animation in seconds?")]
    public float slash2AnimDuration = 1.0f;

    public AudioClip swordSwing1; // Hit enemy (PlayerSlash)
    public AudioClip swordSwing2; // Hit enemy (PlayerSlash2)
    public AudioClip swordWoosh1; // Miss (PlayerSlash)
    public AudioClip swordWoosh2; // Miss (PlayerSlash2)

    Rigidbody rigidbody;
    AudioSource audioSource;

    // Timer to track when we can attack again
    private float nextSlashTime = 0f;

    /// <summary> Functions to override movement speed. Will use the last added override. </summary>
    public List<System.Func<float>> speedOverrides = new List<System.Func<float>>();

    void Awake()
    {
        // Get components
        rigidbody = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();

        // Get the animator if it wasn't manually assigned in the Inspector
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
            if (animator == null)
            {
                Debug.LogWarning("No Animator found! Please drag your character model into the Animator slot in the Inspector.");
            }
        }

        // Fallback to main camera if not assigned
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    void Update()
    {
        if (animator != null)
        {
            if (Input.GetKeyDown(jumpKey))
            {
                animator.SetTrigger("isJumping");
            }

            // Only allow slashing if the current time is greater than the next allowed slash time
            if (Input.GetKeyDown(slashKey) && Time.time >= nextSlashTime)
            {
                PerformSlash();
            }
        }
    }

    void PerformSlash()
    {
        // 1. Pick a random slash (1 or 2)
        int slashChoice = Random.Range(1, 3);

        // Clear any old stuck triggers before setting a new one
        animator.ResetTrigger("isSlashing");

        // Tell the animator which slash to use, then trigger it
        animator.SetInteger("slashType", slashChoice);
        animator.SetTrigger("isSlashing");

        // 2. Check if we hit an enemy
        bool hitEnemy = false;

        if (cameraTransform != null)
        {
            RaycastHit hit;
            // Shoot a ray forward from the camera
            if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out hit, attackRange))
            {
                if (hit.collider.CompareTag("Enemy"))
                {
                    hitEnemy = true;
                }
            }
        }

        // 3. Play the correct audio
        AudioClip clipToPlay = null;

        if (slashChoice == 1)
        {
            clipToPlay = hitEnemy ? swordSwing1 : swordWoosh1;
        }
        else if (slashChoice == 2)
        {
            clipToPlay = hitEnemy ? swordSwing2 : swordWoosh2;
        }

        float clipLength = 0f;

        if (clipToPlay != null)
        {
            audioSource.PlayOneShot(clipToPlay);
            clipLength = clipToPlay.length;
        }

        // 4. Calculate Cooldown
        // Find out which animation duration to use
        float animLength = (slashChoice == 1) ? slash1AnimDuration : slash2AnimDuration;

        // Find whichever is longer: the animation or the audio
        float longestDuration = Mathf.Max(clipLength, animLength);

        // Set the cooldown: Current Time + Longest Duration + 2 Second Delay
        nextSlashTime = Time.time + longestDuration + 1.0f;
    }

    void FixedUpdate()
    {
        // Update IsRunning from input.
        IsRunning = canRun && Input.GetKey(runningKey);

        // Get targetMovingSpeed.
        float targetMovingSpeed = IsRunning ? runSpeed : speed;
        if (speedOverrides.Count > 0)
        {
            targetMovingSpeed = speedOverrides[speedOverrides.Count - 1]();
        }

        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        // Get targetVelocity from input.
        Vector2 targetVelocity = new Vector2(horizontalInput * targetMovingSpeed, verticalInput * targetMovingSpeed);

        // Apply movement.
        rigidbody.linearVelocity = transform.rotation * new Vector3(targetVelocity.x, rigidbody.linearVelocity.y, targetVelocity.y);

        // --- ANIMATION LOGIC ---
        if (animator != null)
        {
            bool isMovingForward = verticalInput > 0.1f;
            bool isMovingBackward = verticalInput < -0.1f;
            bool isStrafingLeft = horizontalInput < -0.1f;
            bool isStrafingRight = horizontalInput > 0.1f;
            bool isCrouching = Input.GetKey(crouchKey);

            animator.SetBool("isWalkingFoward", isMovingForward && !IsRunning);
            animator.SetBool("isWalkingBackward", isMovingBackward);
            animator.SetBool("isStrafingLeft", isStrafingLeft);
            animator.SetBool("isStrafingRight", isStrafingRight);
            animator.SetBool("isCrouching", isCrouching);
            animator.SetBool("IsRunning", isMovingForward && IsRunning);
        }
    }
}