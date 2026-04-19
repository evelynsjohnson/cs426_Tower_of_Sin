using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;

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

    public KeyCode toggleCursorKey = KeyCode.Escape;
    public bool uiMode = true;

    [Header("Control State")]
    public bool canControl = true;
    public bool canAttack = true;

    [Header("Debug / Safety")]
    public bool enableDebugLogs = true;
    public bool enableContinuousPositionLogs = false;
    public float positionLogInterval = 0.5f;
    public float maxAllowedJumpVelocity = 8f;
    public float minAllowedYBeforeReset = -50f;
    public Vector3 emergencyResetPosition = new Vector3(0f, 3f, 0f);

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

    public GroundCheck groundCheck;
    public float jumpHeight = 2f;
    public float jumpCooldown = 0.1f;

    private bool jumpQueued = false;
    private float lastJumpTime = -999f;

    public AudioClip swordSwing1;
    public AudioClip swordSwing2;
    public AudioClip swordWoosh1;
    public AudioClip swordWoosh2;

    private Rigidbody rigidbody;
    private AudioSource audioSource;

    private float nextSlashTime = 0f;
    private float holdTimer = 0f;

    public List<System.Func<float>> speedOverrides = new List<System.Func<float>>();

    public bool invertControls = false;

    public float slash1AudioOffset = 0.2f;
    public float slash2AudioOffset = 0.5f;

    private float nextPositionLogTime = 0f;
    private bool hasLoggedBlueScreenHint = false;

    public TextMeshProUGUI finalAttackText;

    void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();

        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
    }

    void Start()
    {
        SetUIMode(false);

        if (enableDebugLogs)
        {
            Debug.Log($"[FPM] Start scene={SceneManager.GetActiveScene().name}");
            Debug.Log($"[FPM] Player start pos={transform.position}");
            if (cameraTransform != null)
                Debug.Log($"[FPM] Camera start pos={cameraTransform.position}");
        }
    }

    void Update()
    {
        if (!canControl)
        {
            currentHorizontalInput = 0f;
            currentVerticalInput = 0f;
            IsRunning = false;
            holdTimer = 0f;
            jumpQueued = false;
            UpdateAnimationStates(0f, 0f);
            return;
        }

        if (Input.GetKeyDown(toggleCursorKey))
        {
            SetUIMode(!uiMode);

            if (enableDebugLogs)
                Debug.Log($"[FPM] UI mode toggled. uiMode={uiMode}, timeScale={Time.timeScale}");
        }

        if (enableContinuousPositionLogs && Time.time >= nextPositionLogTime)
        {
            nextPositionLogTime = Time.time + positionLogInterval;
            DebugPosition("Periodic");
        }

        CheckForInvalidState();

        if (Time.timeScale == 0f) return;

        if (uiMode)
        {
            currentHorizontalInput = 0f;
            currentVerticalInput = 0f;
            IsRunning = false;
            holdTimer = 0f;
            UpdateAnimationStates(0f, 0f);
            return;
        }

        HandleInput();

        currentHorizontalInput = Input.GetAxisRaw("Horizontal");
        currentVerticalInput = Input.GetAxisRaw("Vertical");
        IsRunning = canRun && Input.GetKey(runningKey);

        if (Input.GetKeyDown(jumpKey) &&
            groundCheck != null &&
            groundCheck.isGrounded &&
            Time.time > lastJumpTime + jumpCooldown)
        {
            jumpQueued = true;
            lastJumpTime = Time.time;

            if (enableDebugLogs)
            {
                Debug.Log("[FPM] JUMP QUEUED");
                Debug.Log($"[FPM] Scene={SceneManager.GetActiveScene().name}");
                Debug.Log($"[FPM] Player pos={transform.position}");
                if (cameraTransform != null)
                    Debug.Log($"[FPM] Camera pos={cameraTransform.position}");
                Debug.Log($"[FPM] Rigidbody vel before jump={rigidbody.linearVelocity}");
                Debug.Log($"[FPM] grounded={(groundCheck != null ? groundCheck.isGrounded.ToString() : "null groundCheck")}");
            }
        }

        UpdateAnimationStates(currentHorizontalInput, currentVerticalInput);
    }

    void SetUIMode(bool enabled)
    {
        uiMode = enabled;
        Cursor.lockState = enabled ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = enabled;
        Time.timeScale = enabled ? 0f : 1f;
    }

    void HandleInput()
    {
        if (!canAttack) return;
        if (animator == null) return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

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
        if (!canAttack || !canControl) return;

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

        if (!canAttack || !canControl) yield break;

        bool hitEnemy = false;
        float finalDamage = (slashChoice == 1) ? slash1BaseDamage : slash2BaseDamage;

        if (Random.Range(0f, 100f) <= critChance) finalDamage *= 2f;

        if (cameraTransform != null)
        {
            Vector3 hitCenter = cameraTransform.position + (cameraTransform.forward * (attackRange * 0.5f));
            float hitRadius = (slashChoice == 1) ? 1.0f : 1.5f;

            Collider[] hitColliders = Physics.OverlapSphere(hitCenter, hitRadius);

            foreach (Collider hitCol in hitColliders)
            {
                if (hitCol.CompareTag("Enemy"))
                {
                    hitEnemy = true;

                    PrisonZombieAI zombie = hitCol.GetComponentInParent<PrisonZombieAI>();
                    if (zombie != null) zombie.TakeDamage(finalDamage, slashChoice);

                    EnvyAI envy = hitCol.GetComponentInParent<EnvyAI>();
                    if (envy != null) envy.TakeDamage(finalDamage, slashChoice);

                    GluttonyAI gluttony = hitCol.GetComponentInParent<GluttonyAI>();
                    if (gluttony != null) gluttony.TakeDamage(finalDamage, slashChoice);

                    EyebatTrapAI eye = hitCol.GetComponentInParent<EyebatTrapAI>();
                    if (eye != null) eye.TakeDamage(1);

                    WrathAI wrath = hitCol.GetComponentInParent<WrathAI>();
                    if (wrath != null) wrath.TakeDamage(finalDamage);

                    PrideAI pride = hitCol.GetComponentInParent<PrideAI>();
                    if (pride != null) pride.TakeDamage(finalDamage);

                    SlothAI sloth = hitCol.GetComponentInParent<SlothAI>();
                    if (sloth != null) sloth.TakeDamage(finalDamage);

                    LustAI lust = hitCol.GetComponentInParent<LustAI>();
                    if (lust != null) lust.TakeDamage(finalDamage);

                    GreedAI greed = hitCol.GetComponentInParent<GreedAI>();
                    if (greed != null) greed.TakeDamage(finalDamage);

                    TentacleBossUnit greedTentacle = hitCol.GetComponentInParent<TentacleBossUnit>();
                    if (greedTentacle != null) greedTentacle.TakeDamage(finalDamage);
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
            audioSource.time = (offsetToUse < clipToPlay.length) ? offsetToUse : 0f;

            audioSource.Play();
            audioSource.pitch = 1.0f;
        }
    }

    void FixedUpdate()
    {
        if (!canControl)
        {
            rigidbody.linearVelocity = Vector3.zero;
            return;
        }

        if (uiMode)
        {
            rigidbody.linearVelocity = new Vector3(0f, rigidbody.linearVelocity.y, 0f);
            return;
        }

        float targetMovingSpeed = IsRunning ? runSpeed : speed;
        if (speedOverrides.Count > 0)
            targetMovingSpeed = speedOverrides[speedOverrides.Count - 1]();

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 moveDir = (right * currentHorizontalInput + forward * currentVerticalInput).normalized;
        Vector3 velocity = moveDir * targetMovingSpeed;

        float yVelocity = rigidbody.linearVelocity.y;
        Physics.gravity = new Vector3(0, -20f, 0);

        if (jumpQueued)
        {
            float jumpVelocity = Mathf.Sqrt(2f * Physics.gravity.magnitude * jumpHeight);
            jumpVelocity = Mathf.Min(jumpVelocity, maxAllowedJumpVelocity);
            yVelocity = jumpVelocity;
            jumpQueued = false;

            if (enableDebugLogs)
            {
                Debug.Log($"[FPM] Applying jump velocity={jumpVelocity}");
                Debug.Log($"[FPM] Rigidbody vel pre-apply={rigidbody.linearVelocity}");
                DebugPosition("JumpApply");
            }
        }

        rigidbody.linearVelocity = new Vector3(velocity.x, yVelocity, velocity.z);

        if (enableDebugLogs && (float.IsNaN(rigidbody.linearVelocity.x) ||
                                float.IsNaN(rigidbody.linearVelocity.y) ||
                                float.IsNaN(rigidbody.linearVelocity.z)))
        {
            Debug.LogError("[FPM] Rigidbody velocity became NaN!");
        }
    }

    void UpdateAnimationStates(float horizontalInput, float verticalInput)
    {
        if (animator == null) return;

        bool isMovingForward = verticalInput > 0.1f;
        bool isMovingBackward = verticalInput < -0.1f;
        bool isStrafingLeft = horizontalInput < -0.1f;
        bool isStrafingRight = horizontalInput > 0.1f;
        bool isCrouching = Input.GetKey(crouchKey) && !uiMode && canControl;

        animator.SetBool("isWalkingForward", isMovingForward && !IsRunning);
        animator.SetBool("isRunningForward", isMovingForward && IsRunning);
        animator.SetBool("isWalkingBackward", isMovingBackward && !IsRunning);
        animator.SetBool("isRunningBackward", isMovingBackward && IsRunning);
        animator.SetBool("isStrafingLeft", isStrafingLeft);
        animator.SetBool("isStrafingRight", isStrafingRight);
        animator.SetBool("isCrouching", isCrouching);
    }

    private void CheckForInvalidState()
    {
        if (transform.position.y < minAllowedYBeforeReset)
        {
            Debug.LogError($"[FPM] Player fell below safe Y ({transform.position.y}). Resetting to {emergencyResetPosition}");
            EmergencyReset();
            return;
        }

        if (HasInvalidVector(transform.position))
        {
            Debug.LogError("[FPM] Player position became invalid. Resetting.");
            EmergencyReset();
            return;
        }

        if (cameraTransform != null && HasInvalidVector(cameraTransform.position))
        {
            Debug.LogError("[FPM] Camera position became invalid. Resetting player.");
            EmergencyReset();
            return;
        }

        if (!hasLoggedBlueScreenHint && cameraTransform != null)
        {
            Vector3 vp = Camera.main != null ? Camera.main.WorldToViewportPoint(transform.position) : Vector3.zero;
            if (vp.z < 0f)
            {
                hasLoggedBlueScreenHint = true;
                Debug.LogWarning("[FPM] Player is behind the active camera. This can break UI/world-to-screen calculations.");
                DebugPosition("BehindCamera");
            }
        }
    }

    private void EmergencyReset()
    {
        rigidbody.linearVelocity = Vector3.zero;
        transform.position = emergencyResetPosition;

        if (cameraTransform != null)
        {
            Debug.Log($"[FPM] After reset player pos={transform.position}, camera pos={cameraTransform.position}");
        }
    }

    private bool HasInvalidVector(Vector3 v)
    {
        return float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) ||
               float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z);
    }

    private void DebugPosition(string label)
    {
        if (!enableDebugLogs) return;

        string camPos = cameraTransform != null ? cameraTransform.position.ToString() : "null";
        Debug.Log($"[FPM:{label}] scene={SceneManager.GetActiveScene().name} playerPos={transform.position} camPos={camPos} vel={rigidbody.linearVelocity}");
    }

    public void UpdateAttack()
    {
        float damage = float.Parse(finalAttackText.text);
        slash1BaseDamage = damage;
        slash2BaseDamage = damage * 1.5f;
    }

    public void DisableControlOnDeath()
    {
        canControl = false;
        canAttack = false;
        currentHorizontalInput = 0f;
        currentVerticalInput = 0f;
        IsRunning = false;
        holdTimer = 0f;
        jumpQueued = false;
        nextSlashTime = 0f;

        if (rigidbody != null)
            rigidbody.linearVelocity = Vector3.zero;

        UpdateAnimationStates(0f, 0f);
    }

    public void ResetPlayerForNewRun()
    {
        canControl = true;
        canAttack = true;

        holdTimer = 0f;
        jumpQueued = false;
        IsRunning = false;
        nextSlashTime = 0f;

        currentHorizontalInput = 0f;
        currentVerticalInput = 0f;

        if (rigidbody != null)
            rigidbody.linearVelocity = Vector3.zero;

        UpdateAttack();
        SetUIMode(false);
        UpdateAnimationStates(0f, 0f);
    }
}