using UnityEngine;
using UnityEngine.AI;
using TMPro;
using UnityEngine.UI;
using System.Collections;

// ─────────────────────────────────────────────────────────────────────────────
// LustAI — Lust Boss (Sin #6, final boss in the cycle, most dangerous)
//
// Sin mechanic: Lust corrupts the player's controls.
//   Phase 1: High mobility. Fires homing charm projectiles that, on hit,
//            INVERT the player's movement controls for 3 seconds.
//   Phase 2 (below 50% HP): Desperate — teleports behind the player periodically.
//   Throughout: Lowest HP of all bosses (compensated by control disruption).
//
// AI: FSM — Idle → Chase → Attack/Charm → Teleport (phase 2)
//
// Requires: FirstPersonMovement.invertControls  (public bool field added to FPM)
// ─────────────────────────────────────────────────────────────────────────────

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(AudioSource))]
public class LustAI : MonoBehaviour
{
    // ── Health & Phase ────────────────────────────────────────────────────────
    public float maxHealth          = 200f;   // 75% of typical — offset by mechanics
    private float currentHealth;
    private int   currentPhase      = 1;
    private bool  phase2Triggered   = false;
    public float  phase2Threshold   = 0.50f;
    [SerializeField] private int currentFloor = 5;
    [SerializeField] private float baseHealthAtFloor5 = 273f;
    [SerializeField] private float healthPerFiveFloors = 60f;

    // ── Lust — Charm Projectile ───────────────────────────────────────────────
    public GameObject charmProjectilePrefab;
    public float      charmCooldown       = 4f;
    public float      charmDamage         = 10f;
    public float      charmInvertDuration = 3f;
    public float      charmSpeed          = 8f;
    private float     charmTimer          = 0f;

    // ── Lust — Phase 2 Teleport ───────────────────────────────────────────────
    public float teleportCooldown    = 6f;
    public float teleportBehindDist  = 2f;
    private float teleportTimer      = 0f;

    // ── Damage ────────────────────────────────────────────────────────────────
    public float  damageToPlayer    = 14f;
    private float damageMultiplier  = 1f;

    // ── UI ────────────────────────────────────────────────────────────────────
    public GameObject  uiCanvasObject;
    public TMP_Text    healthText;
    public Image       healthBarFill;
    public float       healthDrainSpeed       = 5f;
    public float       deathAnimationDuration = 2.5f;

    [Header("Shared Boss UI (Optional)")]
    [SerializeField] private Image bossHealthBarFill;
    [SerializeField] private TMP_Text bossHealthText;
    [SerializeField] private GameObject bossHealthUIRoot;

    [Header("Arena Death Effects")]
    [SerializeField] private Color bossLightColor = new Color(1f, 0.4f, 0.8f);
    [SerializeField] private float lightIntensityMultiplier = 1.2f;

    // ── Movement ──────────────────────────────────────────────────────────────
    public float walkSpeed      = 3.5f;   // slower chase speed for clearer walk readability
    public float aggroRadius    = 18f;
    public float attackRadius   = 2.5f;
    public float attackCooldown = 1.8f;
    public float attackDmgDelay = 0.35f;
    public float repathInterval = 0.2f;
    public float repathDistanceThreshold = 0.75f;

    // ── Audio ─────────────────────────────────────────────────────────────────
    public AudioClip hitSound;
    public AudioClip missSound;
    public AudioClip idleSound;
    public AudioClip walkSound;
    public AudioClip roarSound;
    public AudioClip teleportSound;

    // ── Loot ──────────────────────────────────────────────────────────────────
    public GameObject healthPotionPrefab;
    public float      healthPotChance = 45f;

    // ── Components ────────────────────────────────────────────────────────────
    public Animator animator;

    private Transform    player;
    private Transform    mainCamera;
    private NavMeshAgent agent;
    private PlayerHealth playerHealthScript;
    private FirstPersonMovement playerMovement;
    private AudioSource  sfxSource;
    private AudioSource  walkSource;
    private Light[] arenaLights = new Light[0];
    private Color[] originalLightColors = new Color[0];
    private float[] originalLightIntensities = new float[0];
    private Transform basementDoorLeftRef;
    private Transform basementDoorRightRef;
    private AudioSource gateAudioSourceRef;
    private AudioClip largeGateClipRef;
    private GameObject bossChestPrefabRef;
    private Transform bossChestSpawnPointRef;
    private float doorMoveDistanceZRef = 1f;
    private float doorMoveDurationRef = 3f;

    // ── State ─────────────────────────────────────────────────────────────────
    private bool  isDead         = false;
    private bool  isAttacking    = false;
    private bool  hasSeenPlayer  = false;
    private float nextAttackTime = 0f;
    private float idleAudioTimer = 0f;
    private float nextRepathTime = 0f;
    private Vector3 lastChaseTarget;
    private bool hasChaseTarget = false;

    public void SetFloor(int floor)
    {
        currentFloor = Mathf.Max(1, floor);
    }

    private float GetScaledHealthForFloor(int floor)
    {
        int bonusSteps = Mathf.Max(0, floor / 5 - 1);
        return baseHealthAtFloor5 + (bonusSteps * healthPerFiveFloors);
    }

    public void SetupArenaReferences(Image sharedHealthBarFill, TMP_Text sharedHealthText, GameObject sharedHealthUIRoot)
    {
        SetupArenaReferences(
            sharedHealthBarFill,
            sharedHealthText,
            sharedHealthUIRoot,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            1f,
            3f
        );
    }

    public void SetupArenaReferences(
        Image sharedHealthBarFill,
        TMP_Text sharedHealthText,
        GameObject sharedHealthUIRoot,
        Light[] lights,
        Transform basementDoorLeft,
        Transform basementDoorRight,
        AudioSource gateAudioSource,
        AudioClip largeGateClip,
        GameObject bossChestPrefab,
        Transform bossChestSpawnPoint,
        float doorMoveDistanceZ,
        float doorMoveDuration)
    {
        bossHealthBarFill = sharedHealthBarFill;
        bossHealthText = sharedHealthText;
        bossHealthUIRoot = sharedHealthUIRoot;
        basementDoorLeftRef = basementDoorLeft;
        basementDoorRightRef = basementDoorRight;
        gateAudioSourceRef = gateAudioSource;
        largeGateClipRef = largeGateClip;
        bossChestPrefabRef = bossChestPrefab;
        bossChestSpawnPointRef = bossChestSpawnPoint;
        doorMoveDistanceZRef = Mathf.Abs(doorMoveDistanceZ);
        doorMoveDurationRef = Mathf.Max(0.1f, doorMoveDuration);

        arenaLights = lights ?? new Light[0];
        originalLightColors = new Color[arenaLights.Length];
        originalLightIntensities = new float[arenaLights.Length];
        for (int i = 0; i < arenaLights.Length; i++)
        {
            if (arenaLights[i] == null) continue;
            originalLightColors[i] = arenaLights[i].color;
            originalLightIntensities[i] = arenaLights[i].intensity;
        }

        if (bossHealthUIRoot != null)
            bossHealthUIRoot.SetActive(true);

        // When shared top-of-screen boss UI is used, hide any world-space HP UI.
        if ((bossHealthBarFill != null || bossHealthText != null || bossHealthUIRoot != null) && uiCanvasObject != null)
            uiCanvasObject.SetActive(false);

        ApplyBossLightState();
        SetHealthBarFillImmediate(1f);
        UpdateHealthUI();
    }

    // ─────────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (currentFloor <= 0)
            currentFloor = Mathf.Max(1, FloorTextController.floorNumber);

        maxHealth     = GetScaledHealthForFloor(currentFloor);
        currentHealth = maxHealth;

        agent       = GetComponent<NavMeshAgent>();
        agent.speed = walkSpeed;

        sfxSource = GetComponent<AudioSource>();
        sfxSource.spatialBlend = 1f;
        sfxSource.rolloffMode  = AudioRolloffMode.Linear;
        sfxSource.minDistance  = 2f;
        sfxSource.maxDistance  = 12f;

        walkSource = gameObject.AddComponent<AudioSource>();
        walkSource.spatialBlend = 1f;
        walkSource.rolloffMode  = AudioRolloffMode.Linear;
        walkSource.minDistance  = 2f;
        walkSource.maxDistance  = 18f;
        walkSource.clip         = walkSound;
        walkSource.loop         = true;

        GameObject pObj = GameObject.FindGameObjectWithTag("Player");
        if (pObj != null)
        {
            player             = pObj.transform;
            playerHealthScript = pObj.GetComponent<PlayerHealth>();
            playerMovement     = pObj.GetComponent<FirstPersonMovement>();
            if (playerMovement == null) playerMovement = pObj.GetComponentInChildren<FirstPersonMovement>();
        }

        if (Camera.main != null) mainCamera = Camera.main.transform;
        if (bossHealthUIRoot != null) bossHealthUIRoot.SetActive(true);
        if ((bossHealthBarFill != null || bossHealthText != null || bossHealthUIRoot != null) && uiCanvasObject != null)
            uiCanvasObject.SetActive(false);
        SetHealthBarFillImmediate(1f);

        idleAudioTimer = Random.Range(3f, 7f);
        UpdateHealthUI();
    }

    void Update()
    {
        UpdateHealthBar();
        if (isDead || player == null) return;

        HandleAudio();

        if (!hasSeenPlayer && FlatDist(transform.position, player.position) <= aggroRadius)
            hasSeenPlayer = true;

        if (!hasSeenPlayer) return;

        if (!phase2Triggered && currentHealth / maxHealth <= phase2Threshold)
            TriggerPhase2();

        float dt = Time.deltaTime;

        // Charm projectile — fires regardless of attack state
        charmTimer += dt;
        if (charmTimer >= charmCooldown) { charmTimer = 0f; FireCharm(); }

        // Phase 2 teleport
        if (currentPhase >= 2)
        {
            teleportTimer += dt;
            if (teleportTimer >= teleportCooldown) { teleportTimer = 0f; TeleportBehindPlayer(); }
        }

        if (isAttacking) return;

        float dist = FlatDist(transform.position, player.position);

        if (dist <= aggroRadius)
        {
            FacePlayer();

            if (dist <= attackRadius && Time.time >= nextAttackTime)
                StartCoroutine(AttackRoutine());
            else if (dist > attackRadius)
            {
                agent.isStopped = false;
                if (Time.time >= nextRepathTime)
                {
                    Vector3 chaseTarget = player.position;
                    float thresholdSqr = repathDistanceThreshold * repathDistanceThreshold;
                    if (!hasChaseTarget || (chaseTarget - lastChaseTarget).sqrMagnitude >= thresholdSqr)
                    {
                        agent.SetDestination(chaseTarget);
                        lastChaseTarget = chaseTarget;
                        hasChaseTarget = true;
                    }
                    nextRepathTime = Time.time + repathInterval;
                }
                animator?.SetBool("isWalking", true);
                if (!walkSource.isPlaying) walkSource.Play();
            }
            else
            {
                agent.isStopped = true;
                nextRepathTime = 0f;
                animator?.SetBool("isWalking", false);
                walkSource.Pause();
            }
        }
        else
        {
            nextRepathTime = 0f;
            hasChaseTarget = false;
            agent.isStopped = true;
            animator?.SetBool("isWalking", false);
            walkSource.Pause();
        }
    }

    void LateUpdate()
    {
        if (bossHealthBarFill != null)
            return;

        if (uiCanvasObject != null && mainCamera != null)
            uiCanvasObject.transform.LookAt(uiCanvasObject.transform.position + mainCamera.forward);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Phase 2
    // ─────────────────────────────────────────────────────────────────────────

    private void TriggerPhase2()
    {
        phase2Triggered = true;
        currentPhase    = 2;
        teleportTimer   = 1f;              // first teleport comes quickly
        attackCooldown *= 0.7f;            // attacks faster in desperation
        damageMultiplier *= 1.2f;
        animator?.SetTrigger("roar");
        if (roarSound != null) sfxSource.PlayOneShot(roarSound);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Charm projectile
    // ─────────────────────────────────────────────────────────────────────────

    private void FireCharm()
    {
        if (charmProjectilePrefab == null || player == null) return;

        Vector3 spawnPos = transform.position + Vector3.up * 1.2f;
        Vector3 dir      = (player.position - spawnPos).normalized;

        GameObject proj = Instantiate(charmProjectilePrefab, spawnPos, Quaternion.LookRotation(dir));

        LustCharmProjectile cp = proj.AddComponent<LustCharmProjectile>();
        cp.damage         = charmDamage;
        cp.invertDuration = charmInvertDuration;
        cp.speed          = charmSpeed;
        cp.target         = player;
        cp.playerMovement = playerMovement;

        Destroy(proj, 6f);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Teleport behind player (phase 2)
    // ─────────────────────────────────────────────────────────────────────────

    private void TeleportBehindPlayer()
    {
        if (player == null) return;

        Vector3 behindPlayer = player.position - player.forward * teleportBehindDist;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(behindPlayer, out hit, 5f, NavMesh.AllAreas))
        {
            transform.position = hit.position;
            if (agent != null) agent.Warp(hit.position);
            hasChaseTarget = false;
            nextRepathTime = 0f;
        }

        if (teleportSound != null) sfxSource.PlayOneShot(teleportSound);

        FacePlayer();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Melee attack
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator AttackRoutine()
    {
        isAttacking     = true;
        agent.isStopped = true;
        FacePlayer();

        animator?.SetTrigger("attack");

        yield return new WaitForSeconds(attackDmgDelay);

        if (!isDead && player != null)
        {
            if (FlatDist(transform.position, player.position) <= attackRadius + 0.5f)
            {
                if (playerHealthScript != null)
                    playerHealthScript.TakeDamage(damageToPlayer * damageMultiplier);
                if (hitSound != null) sfxSource.PlayOneShot(hitSound);
            }
            else
            {
                if (missSound != null) sfxSource.PlayOneShot(missSound);
            }
        }

        nextAttackTime = Time.time + (attackCooldown - attackDmgDelay);
        isAttacking    = false;
        if (agent != null) agent.isStopped = false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Damage
    // ─────────────────────────────────────────────────────────────────────────

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        currentHealth  = Mathf.Max(0f, currentHealth);
        UpdateHealthUI();

        if (currentHealth <= 0f) Die();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Death — make sure controls are restored if she dies mid-inversion
    // ─────────────────────────────────────────────────────────────────────────

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        // Restore player controls if charm was active
        if (playerMovement != null) playerMovement.invertControls = false;

        StopAllCoroutines();
        animator?.SetTrigger("die");
        if (agent.enabled) agent.enabled = false;
        GetComponent<Collider>().enabled  = false;

        TMP_Text targetText = bossHealthText != null ? bossHealthText : healthText;
        if (targetText != null) targetText.text = "";
        walkSource?.Stop();
        sfxSource?.Stop();

        if (healthPotionPrefab != null && Random.Range(0f, 100f) < healthPotChance)
            Instantiate(healthPotionPrefab, transform.position + Vector3.up * 0.2f, Quaternion.identity);

        HandleArenaDefeatRewards();
        StartCoroutine(HideUIAfterDeath());
    }

    private IEnumerator HideUIAfterDeath()
    {
        yield return new WaitForSeconds(deathAnimationDuration);
        if (bossHealthUIRoot != null) bossHealthUIRoot.SetActive(false);
        else if (uiCanvasObject != null) uiCanvasObject.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private void FacePlayer()
    {
        if (player == null) return;
        Vector3 dir = player.position - transform.position;
        dir.y = 0f;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 10f);
    }

    private float FlatDist(Vector3 a, Vector3 b)
        => Vector3.Distance(new Vector3(a.x, 0f, a.z), new Vector3(b.x, 0f, b.z));

    private void HandleAudio()
    {
        float vertDist  = Mathf.Abs(player.position.y - transform.position.y);
        bool  sameFloor = vertDist < 2.5f;
        sfxSource.mute  = !sameFloor;
        walkSource.mute = !sameFloor;

        idleAudioTimer -= Time.deltaTime;
        if (idleAudioTimer <= 0f)
        {
            if (idleSound != null && sameFloor) sfxSource.PlayOneShot(idleSound);
            idleAudioTimer = Random.Range(4f, 8f);
        }
    }

    private void UpdateHealthBar()
    {
        Image targetFill = bossHealthBarFill != null ? bossHealthBarFill : healthBarFill;
        if (targetFill == null) return;

        targetFill.fillAmount = Mathf.Lerp(
            targetFill.fillAmount,
            currentHealth / maxHealth,
            Time.deltaTime * healthDrainSpeed
        );
    }

    private void SetHealthBarFillImmediate(float value)
    {
        Image targetFill = bossHealthBarFill != null ? bossHealthBarFill : healthBarFill;
        if (targetFill != null)
            targetFill.fillAmount = value;
    }

    private void UpdateHealthUI()
    {
        TMP_Text targetText = bossHealthText != null ? bossHealthText : healthText;
        if (targetText != null)
            targetText.text = (int)currentHealth + "/" + (int)maxHealth;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aggroRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadius);
    }

    private void HandleArenaDefeatRewards()
    {
        RestoreArenaLights();

        if (bossChestPrefabRef != null && bossChestSpawnPointRef != null)
            Instantiate(bossChestPrefabRef, bossChestSpawnPointRef.position, bossChestSpawnPointRef.rotation);

        if (gateAudioSourceRef != null)
        {
            if (largeGateClipRef != null) gateAudioSourceRef.PlayOneShot(largeGateClipRef);
            else gateAudioSourceRef.Play();
        }

        if (basementDoorLeftRef != null)
            StartCoroutine(MoveDoorZ(basementDoorLeftRef, -doorMoveDistanceZRef, doorMoveDurationRef));
        if (basementDoorRightRef != null)
            StartCoroutine(MoveDoorZ(basementDoorRightRef, doorMoveDistanceZRef, doorMoveDurationRef));
    }

    private void ApplyBossLightState()
    {
        if (arenaLights == null) return;
        for (int i = 0; i < arenaLights.Length; i++)
        {
            if (arenaLights[i] == null) continue;
            arenaLights[i].color = bossLightColor;
            float baseIntensity = (originalLightIntensities != null && i < originalLightIntensities.Length)
                ? originalLightIntensities[i]
                : arenaLights[i].intensity;
            arenaLights[i].intensity = baseIntensity * lightIntensityMultiplier;
        }
    }

    private void RestoreArenaLights()
    {
        if (arenaLights == null) return;
        for (int i = 0; i < arenaLights.Length; i++)
        {
            if (arenaLights[i] == null) continue;
            if (originalLightColors != null && i < originalLightColors.Length)
                arenaLights[i].color = originalLightColors[i];
            if (originalLightIntensities != null && i < originalLightIntensities.Length)
                arenaLights[i].intensity = originalLightIntensities[i];
        }
    }

    private IEnumerator MoveDoorZ(Transform door, float zOffset, float duration)
    {
        if (door == null) yield break;

        Vector3 start = door.position;
        Vector3 end = start + new Vector3(0f, 0f, zOffset);
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.1f, duration);

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            door.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        door.position = end;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// LustCharmProjectile — homing projectile, inverts player movement on hit
// Self-contained, spawned by LustAI.FireCharm()
// ─────────────────────────────────────────────────────────────────────────────
public class LustCharmProjectile : MonoBehaviour
{
    public float  damage;
    public float  invertDuration;
    public float  speed;
    public Transform target;
    public FirstPersonMovement playerMovement;

    void Update()
    {
        if (target == null) { Destroy(gameObject); return; }

        Vector3 dir = (target.position + Vector3.up * 1f - transform.position).normalized;
        transform.position += dir * speed * Time.deltaTime;
        transform.rotation  = Quaternion.LookRotation(dir);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerHealth ph = other.GetComponent<PlayerHealth>();
        if (ph == null) ph = other.GetComponentInParent<PlayerHealth>();
        if (ph != null) ph.TakeDamage(damage);

        if (playerMovement != null)
            playerMovement.StartCoroutine(InvertRoutine());

        Destroy(gameObject);
    }

    private System.Collections.IEnumerator InvertRoutine()
    {
        playerMovement.invertControls = true;
        yield return new WaitForSeconds(invertDuration);
        playerMovement.invertControls = false;
    }
}
