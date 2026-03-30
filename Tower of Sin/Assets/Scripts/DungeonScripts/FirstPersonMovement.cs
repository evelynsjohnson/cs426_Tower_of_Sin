using System.Collections.Generic;
using UnityEngine;
using System.Collections;

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
    public Animator animator;
    public KeyCode crouchKey = KeyCode.LeftControl;
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode slashKey = KeyCode.Mouse0;

    [Header("Combat Stats")]
    public float slash1BaseDamage = 10f;
    public float slash2BaseDamage = 20f;

    [Tooltip("Percentage chance to deal double damage (0 to 100)")]
    [Range(0f, 100f)]
    public float critChance = 15f;

    [Header("Combat Setup & Audio")]
    public float attackRange = 2.5f;
    public Transform cameraTransform;

    public float slash1AnimDuration = 1.05f;
    public float slash1HitDelay = 0.20f;

    public float slash2AnimDuration = 1.30f;
    public float slash2HitDelay = 0.30f;

    public AudioClip swordSwing1; // Hit enemy (PlayerSlash)
    public AudioClip swordSwing2; // Hit enemy (PlayerSlash2)
    public AudioClip swordWoosh1; // Miss (PlayerSlash)
    public AudioClip swordWoosh2; // Miss (PlayerSlash2)

    Rigidbody rigidbody;
    AudioSource audioSource;

    // Combat tracking
    private float nextSlashTime = 0f;
    private int swingCount = 0;

    public List<System.Func<float>> speedOverrides = new List<System.Func<float>>();

    void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

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

            if (Input.GetKeyDown(slashKey) && Time.time >= nextSlashTime)
            {
                PerformSlash();
            }
        }
    }

    void PerformSlash()
    {
        swingCount++;
        int slashChoice = 1; // Default to standard slash

        if (swingCount >= 3)
        {
            slashChoice = 2;
            swingCount = 0;
        }

        animator.ResetTrigger("isSlashing");
        animator.SetInteger("slashType", slashChoice);
        animator.SetTrigger("isSlashing");

        float currentDelay = (slashChoice == 1) ? slash1HitDelay : slash2HitDelay;

        StartCoroutine(DealDamageAfterDelay(currentDelay, slashChoice));

        float animLength = (slashChoice == 1) ? slash1AnimDuration : slash2AnimDuration;
        nextSlashTime = Time.time + animLength + 1.0f;
    }

    private IEnumerator DealDamageAfterDelay(float delayTime, int slashChoice)
    {
        yield return new WaitForSeconds(delayTime);

        bool hitEnemy = false;

        // --- NEW DAMAGE & CRIT CALCULATION ---
        float finalDamage = (slashChoice == 1) ? slash1BaseDamage : slash2BaseDamage;

        // Roll a number between 0 and 100. If it's less than our crit chance, it's a crit!
        bool isCrit = Random.Range(0f, 100f) <= critChance;
        if (isCrit)
        {
            finalDamage *= 2f; // Double the damage
        }

        if (cameraTransform != null)
        {
            RaycastHit hit;
            float swordThickness = 0.5f;

            if (Physics.SphereCast(cameraTransform.position, swordThickness, cameraTransform.forward, out hit, attackRange))
            {
                if (hit.collider.CompareTag("Enemy"))
                {
                    hitEnemy = true;

                    PrisonZombieAI zombie = hit.collider.GetComponentInParent<PrisonZombieAI>();
                    if (zombie != null)
                    {
                        zombie.TakeDamage(finalDamage);

                        // testing
                        if (isCrit) Debug.Log("<color=orange>CRITICAL HIT! Dealt " + finalDamage + " damage!</color>");
                        else Debug.Log("Dealt " + finalDamage + " damage!");
                    }
                }
            }
        }

        AudioClip clipToPlay = null;

        if (slashChoice == 1)
        {
            clipToPlay = hitEnemy ? swordSwing1 : swordWoosh1;
        }
        else if (slashChoice == 2)
        {
            clipToPlay = hitEnemy ? swordSwing2 : swordWoosh2;
        }

        if (clipToPlay != null)
        {
            audioSource.pitch = 1.5f;
            audioSource.PlayOneShot(clipToPlay);
            audioSource.pitch = 1.0f;
        }
    }

    void FixedUpdate()
    {
        IsRunning = canRun && Input.GetKey(runningKey);

        float targetMovingSpeed = IsRunning ? runSpeed : speed;
        if (speedOverrides.Count > 0)
        {
            targetMovingSpeed = speedOverrides[speedOverrides.Count - 1]();
        }

        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        Vector2 targetVelocity = new Vector2(horizontalInput * targetMovingSpeed, verticalInput * targetMovingSpeed);
        rigidbody.linearVelocity = transform.rotation * new Vector3(targetVelocity.x, rigidbody.linearVelocity.y, targetVelocity.y);

        if (animator != null)
        {
            bool isMovingForward = verticalInput > 0.1f;
            bool isMovingBackward = verticalInput < -0.1f;
            bool isStrafingLeft = horizontalInput < -0.1f;
            bool isStrafingRight = horizontalInput > 0.1f;
            bool isCrouching = Input.GetKey(crouchKey);

            animator.SetBool("isWalkingForward", isMovingForward && !IsRunning);
            animator.SetBool("isRunningForward", isMovingForward && IsRunning);
            animator.SetBool("isWalkingBackward", isMovingBackward && !IsRunning);
            animator.SetBool("isRunningBackward", isMovingBackward && IsRunning);
            animator.SetBool("isStrafingLeft", isStrafingLeft);
            animator.SetBool("isStrafingRight", isStrafingRight);
            animator.SetBool("isCrouching", isCrouching);
            animator.SetBool("IsRunning", IsRunning);
        }
    }
}