using System.Collections.Generic;
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioSource))]
public class FirstPersonMovement : MonoBehaviour
{
    public float speed = 5;

    public bool canRun = true;
    public bool IsRunning { get; private set; }
    public float runSpeed = 9;
    public KeyCode runningKey = KeyCode.LeftShift;

    public Animator animator;
    public KeyCode crouchKey = KeyCode.LeftControl;
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode slashKey = KeyCode.Mouse0;

    public float slash1BaseDamage = 20f;
    public float slash2BaseDamage = 40f;
    public float chargeTimeRequired = .5f;

    private float currentHorizontalInput = 0f;
    private float currentVerticalInput = 0f;

    [Range(0f, 100f)]
    public float critChance = 15f;

    public float attackRange = 2.5f;
    public Transform cameraTransform;

    public float slash1AnimDuration = 1.05f;
    public float slash1HitDelay = 0.20f;

    public float slash2AnimDuration = 1.30f;
    public float slash2HitDelay = 0.30f;

    public AudioClip swordSwing1;
    public AudioClip swordSwing2;
    public AudioClip swordWoosh1;
    public AudioClip swordWoosh2;

    Rigidbody rigidbody;
    AudioSource audioSource;

    // Combat tracking
    private float nextSlashTime = 0f;
    private float holdTimer = 0f;
    private bool isCharging = false;

    public List<System.Func<float>> speedOverrides = new List<System.Func<float>>();

    // Set to true by LustAI charm projectile — inverts WASD for N seconds
    public bool invertControls = false;

    [Header("Audio Offsets")]
    public float slash1AudioOffset = 0.2f;
    public float slash2AudioOffset = 0.5f; // Starts 0.5 seconds in
    void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();

        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        HandleInput();

        currentHorizontalInput = Input.GetAxisRaw("Horizontal");
        currentVerticalInput = Input.GetAxisRaw("Vertical");
        IsRunning = canRun && Input.GetKey(runningKey);

        UpdateAnimationStates(currentHorizontalInput, currentVerticalInput);
    }

    void HandleInput()
    {
        if (animator == null) return;

        if (Input.GetKeyDown(jumpKey)) animator.SetTrigger("isJumping");

        if (Time.time >= nextSlashTime)
        {
            if (Input.GetKey(slashKey))
            {
                holdTimer += Time.deltaTime;

                if (holdTimer >= chargeTimeRequired)
                {
                    PerformSlash(2);
                    holdTimer = 0f;
                }
            }
            else if (Input.GetKeyUp(slashKey))
            {
                if (holdTimer > 0f)
                {
                    PerformSlash(1);
                    holdTimer = 0f;
                }
            }
            else
            {
                holdTimer = 0f;
            }
        }
        else
        {
            holdTimer = 0f;
        }
    }

    void PerformSlash(int slashChoice)
    {
        animator.ResetTrigger("isSlashing");
        animator.SetInteger("slashType", slashChoice);
        animator.SetTrigger("isSlashing");

        float currentDelay = (slashChoice == 1) ? slash1HitDelay : slash2HitDelay;
        StartCoroutine(DealDamageAfterDelay(currentDelay, slashChoice));

        float animLength = (slashChoice == 1) ? slash1AnimDuration : slash2AnimDuration;
        nextSlashTime = Time.time + animLength;
    }

    private IEnumerator DealDamageAfterDelay(float delayTime, int slashChoice)
    {
        yield return new WaitForSeconds(delayTime);

        bool hitEnemy = false;
        float finalDamage = (slashChoice == 1) ? slash1BaseDamage : slash2BaseDamage;

        if (Random.Range(0f, 100f) <= critChance) finalDamage *= 2f;

        if (cameraTransform != null)
        {
            Vector3 hitCenter = cameraTransform.position + (cameraTransform.forward * (attackRange * 0.5f));

            // Make hit bubble
            float hitRadius = (slashChoice == 1) ? 1.0f : 1.5f;

            Collider[] hitColliders = Physics.OverlapSphere(hitCenter, hitRadius);

            foreach (Collider hitCol in hitColliders)
            {
                if (hitCol.CompareTag("Enemy"))
                {
                    hitEnemy = true;
                    PrisonZombieAI zombie = hitCol.GetComponentInParent<PrisonZombieAI>();
                    if (zombie != null) zombie.TakeDamage(finalDamage, slashChoice);

                    GluttonyAI gluttony = hitCol.GetComponentInParent<GluttonyAI>();
                    if (gluttony != null) gluttony.TakeDamage(finalDamage, slashChoice);

                    EyebatTrapAI eye = hitCol.GetComponentInParent<EyebatTrapAI>();
                    if (eye != null) eye.TakeDamage(1);

                    WrathAI wrath = hitCol.GetComponentInParent<WrathAI>();
                    if (wrath != null) wrath.TakeDamage(finalDamage);

                    EnvyAI envy = hitCol.GetComponentInParent<EnvyAI>();
                    if (envy != null) envy.TakeDamage(finalDamage, slashChoice);

                    PrideAI pride = hitCol.GetComponentInParent<PrideAI>();
                    if (pride != null) pride.TakeDamage(finalDamage);

                    SlothAI sloth = hitCol.GetComponentInParent<SlothAI>();
                    if (sloth != null) sloth.TakeDamage(finalDamage);

                    LustAI lust = hitCol.GetComponentInParent<LustAI>();
                    if (lust != null) lust.TakeDamage(finalDamage);

                    GreedAI greed = hitCol.GetComponentInParent<GreedAI>();
                    if (greed != null) greed.TakeDamage(finalDamage);
                }

                TargetDummy dummy = hitCol.GetComponentInParent<TargetDummy>();
                if (dummy != null)
                {
                    hitEnemy = true;
                    dummy.TakeDamage(finalDamage);
                }



            }
        }

        AudioClip clipToPlay = (slashChoice == 1)
            ? (hitEnemy ? swordSwing1 : swordWoosh1)
            : (hitEnemy ? swordSwing2 : swordWoosh2);

        if (clipToPlay != null)
        {
            audioSource.clip = clipToPlay;
            audioSource.pitch = 1.5f;

            float offsetToUse = (slashChoice == 1) ? slash1AudioOffset : slash2AudioOffset;

            if (offsetToUse < clipToPlay.length)
            {
                audioSource.time = offsetToUse;
            }
            else
            {
                audioSource.time = 0f;
            }

            // Play the clip
            audioSource.Play();
            audioSource.pitch = 1.0f;
        }
    }
    void FixedUpdate()
    {
        float targetMovingSpeed = IsRunning ? runSpeed : speed;
        if (speedOverrides.Count > 0) targetMovingSpeed = speedOverrides[speedOverrides.Count - 1]();

        float hInput = invertControls ? -currentHorizontalInput : currentHorizontalInput;
        float vInput = invertControls ? -currentVerticalInput   : currentVerticalInput;

        Vector2 targetVelocity = new Vector2(hInput * targetMovingSpeed, vInput * targetMovingSpeed);

        rigidbody.linearVelocity = transform.rotation * new Vector3(targetVelocity.x, rigidbody.linearVelocity.y, targetVelocity.y);
    }

    void UpdateAnimationStates(float horizontalInput, float verticalInput)
    {
        if (animator == null) return;

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
    }
}