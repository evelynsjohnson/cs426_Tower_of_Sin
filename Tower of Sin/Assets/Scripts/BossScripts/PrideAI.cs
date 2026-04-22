using UnityEngine;
using UnityEngine.AI;
using TMPro;
using UnityEngine.UI;
using System.Collections;

// ─────────────────────────────────────────────────────────────────────────────
// PrideAI — Pride Boss (Sin #0, appears every 5 floors)
//
// Sin mechanic: Pride is untouchable at full power.
//   Phase 1 (above 75% HP): Damage REFLECT shield — player takes 50% of their
//                            own hit back. Boss takes no damage.
//   Phase 2 (50–75% HP):    Shield shatters. Enraged — moves faster, attacks faster.
//   Phase 3 (below 50% HP): Begins summoning mirror clone decoys periodically.
//
// AI: Explicit FSM — Idle → Chase → Attack, with phase transitions.
// ─────────────────────────────────────────────────────────────────────────────

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(AudioSource))]
public class PrideAI : MonoBehaviour
{
    // ── Health & Phases ───────────────────────────────────────────────────────
    public float maxHealth          = 400f;
    private float currentHealth;
    private int   currentPhase      = 1;
    private bool  phase2Triggered   = false;
    private bool  phase3Triggered   = false;
    public float  phase2Threshold   = 0.75f;  // shield shatters at 75% HP
    public float  phase3Threshold   = 0.50f;  // clones start at 50% HP
    [SerializeField] private int currentFloor = 5;
    [SerializeField] private float baseHealthAtFloor5 = 480f;
    [SerializeField] private float healthPerFiveFloors = 100f;

    // ── Pride — Reflect Shield ────────────────────────────────────────────────
    private bool  shieldActive      = true;
    public float  reflectPercent    = 0.50f;  // player takes 50% of their hit back

    // ── Pride — Phase 2 Boost ─────────────────────────────────────────────────
    public float  phase2SpeedBoost  = 1.4f;
    public float  phase2CooldownMult = 0.7f;  // multiply attackCooldown by this

    // ── Pride — Phase 3 Clones ────────────────────────────────────────────────
    public GameObject clonePrefab;            // visual-only decoy, no AI/collider
    public int        maxClones     = 3;
    public float      cloneInterval = 8f;
    private float     cloneTimer    = 0f;

    // ── Damage ────────────────────────────────────────────────────────────────
    public float  damageToPlayer    = 22f;
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
    [SerializeField] private Color bossLightColor = new Color(0.9f, 0.65f, 1f);
    [SerializeField] private float lightIntensityMultiplier = 1.2f;

    // ── Movement ──────────────────────────────────────────────────────────────
    public float walkSpeed      = 3.5f;
    public float aggroRadius    = 18f;
    public float attackRadius   = 2.5f;
    public float attackCooldown = 3.2f;  // keep >= attack anim length
    public float attackDmgDelay = 1.05f; // damage lands closer to visible hand swing
    public float turnSpeedDegPerSec = 540f;

    // ── Audio ─────────────────────────────────────────────────────────────────
    public AudioClip bossMusic;
    public AudioClip hitSound;
    public AudioClip missSound;
    public AudioClip idleSound;
    public AudioClip walkSound;
    public AudioClip roarSound;
    public AudioClip shieldBreakSound;

    // ── Loot ──────────────────────────────────────────────────────────────────
    public GameObject healthPotionPrefab;
    public float      healthPotChance = 50f;  // pride boss = bigger reward

    // ── Components ────────────────────────────────────────────────────────────
    public Animator animator;

    private Transform    player;
    private Transform    mainCamera;
    private NavMeshAgent agent;
    private PlayerHealth playerHealthScript;
    private AudioSource  sfxSource;
    private AudioSource  walkSource;
    private AudioSource  musicSource;
    private bool warnedNavMeshMissing = false;
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
        // Let NavMeshAgent handle turning while moving.
        agent.updateRotation = true;
        agent.angularSpeed = Mathf.Max(agent.angularSpeed, turnSpeedDegPerSec);
        EnsureAgentOnNavMesh(12f);

        if (animator == null)
            animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        sfxSource = GetComponent<AudioSource>();
        sfxSource.spatialBlend = 1f;
        sfxSource.rolloffMode  = AudioRolloffMode.Linear;
        sfxSource.minDistance  = 2f;
        sfxSource.maxDistance  = 14f;

        walkSource = gameObject.AddComponent<AudioSource>();
        walkSource.spatialBlend = 1f;
        walkSource.rolloffMode  = AudioRolloffMode.Linear;
        walkSource.minDistance  = 2f;
        walkSource.maxDistance  = 20f;
        walkSource.clip         = walkSound;
        walkSource.loop         = true;

        GameObject pObj = GameObject.FindGameObjectWithTag("Player");
        if (pObj != null)
        {
            player             = pObj.transform;
            playerHealthScript = pObj.GetComponent<PlayerHealth>();
        }

        if (Camera.main != null) mainCamera = Camera.main.transform;
        if (bossHealthUIRoot != null) bossHealthUIRoot.SetActive(true);
        if ((bossHealthBarFill != null || bossHealthText != null || bossHealthUIRoot != null) && uiCanvasObject != null)
            uiCanvasObject.SetActive(false);
        SetHealthBarFillImmediate(1f);

        if (bossMusic != null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.clip         = bossMusic;
            musicSource.loop         = true;
            musicSource.spatialBlend = 0f;
            musicSource.volume       = 0.6f;
            musicSource.Play();
        }

        shieldActive   = true;
        idleAudioTimer = Random.Range(3f, 7f);
        UpdateHealthUI();
    }

    void Update()
    {
        UpdateHealthBar();
        if (isDead || player == null) return;

        if (!EnsureAgentOnNavMesh())
        {
            animator?.SetBool("isWalking", false);
            walkSource?.Pause();
            return;
        }

        HandleAudio();

        if (!hasSeenPlayer && FlatDist(transform.position, player.position) <= aggroRadius)
            hasSeenPlayer = true;

        if (!hasSeenPlayer) return;

        // Phase 3 clone spawning
        if (phase3Triggered)
        {
            cloneTimer -= Time.deltaTime;
            if (cloneTimer <= 0f) { SpawnClone(); cloneTimer = cloneInterval; }
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
                Vector3 chaseTarget = player.position;
                if (NavMesh.SamplePosition(player.position, out NavMeshHit playerHit, 2.5f, NavMesh.AllAreas))
                    chaseTarget = playerHit.position;
                agent.SetDestination(chaseTarget);
                animator?.SetBool("isWalking", true);
                if (!walkSource.isPlaying) walkSource.Play();
            }
            else
            {
                agent.isStopped = true;
                animator?.SetBool("isWalking", false);
                walkSource.Pause();
            }
        }
        else
        {
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
    //  Phase transitions
    // ─────────────────────────────────────────────────────────────────────────

    private void CheckPhases()
    {
        float hpPct = currentHealth / maxHealth;

        if (!phase2Triggered && hpPct <= phase2Threshold)
        {
            phase2Triggered = true;
            currentPhase    = 2;

            shieldActive = false;
            if (shieldBreakSound != null) sfxSource.PlayOneShot(shieldBreakSound);
            else if (roarSound   != null) sfxSource.PlayOneShot(roarSound);

            agent.speed     = walkSpeed * phase2SpeedBoost;
            attackCooldown *= phase2CooldownMult;
            damageMultiplier *= 1.3f;

            animator?.SetTrigger("roar");
        }

        if (!phase3Triggered && hpPct <= phase3Threshold)
        {
            phase3Triggered = true;
            currentPhase    = 3;
            cloneTimer      = 0f;   // spawn first clone immediately
        }
    }

    private void SpawnClone()
    {
        if (clonePrefab == null) return;

        GameObject[] existing = GameObject.FindGameObjectsWithTag("BossClone");
        if (existing.Length >= maxClones) return;

        Vector3 offset = new Vector3(Random.Range(-3f, 3f), 0f, Random.Range(-3f, 3f));
        GameObject clone = Instantiate(clonePrefab, transform.position + offset, transform.rotation);
        clone.tag = "BossClone";
        Destroy(clone, 10f);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Attack
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator AttackRoutine()
    {
        isAttacking     = true;
        agent.isStopped = true;
        FacePlayer();

        animator?.SetTrigger("attack");

        yield return new WaitForSeconds(Mathf.Max(0f, attackDmgDelay));

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

        // Cooldown starts after the attack resolves; do not shorten by windup delay.
        nextAttackTime = Time.time + Mathf.Max(0.1f, attackCooldown);
        isAttacking    = false;
        if (agent != null) agent.isStopped = false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Damage — reflect shield in phase 1
    // ─────────────────────────────────────────────────────────────────────────

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        if (shieldActive)
        {
            // Reflect — player eats 50% of their own hit, boss takes nothing
            if (playerHealthScript != null)
                playerHealthScript.TakeDamage(amount * reflectPercent);
            return;
        }

        currentHealth -= amount;
        currentHealth  = Mathf.Max(0f, currentHealth);
        UpdateHealthUI();
        CheckPhases();

        if (currentHealth <= 0f) Die();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Death
    // ─────────────────────────────────────────────────────────────────────────

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        // Destroy any lingering clones
        foreach (GameObject clone in GameObject.FindGameObjectsWithTag("BossClone"))
            Destroy(clone);

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

        // While chasing, let NavMesh steering own rotation.
        if (agent != null && agent.enabled && !agent.isStopped)
            return;

        Vector3 dir = player.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude <= 0.0001f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRot,
            turnSpeedDegPerSec * Time.deltaTime
        );
    }

    private float FlatDist(Vector3 a, Vector3 b)
        => Vector3.Distance(new Vector3(a.x, 0f, a.z), new Vector3(b.x, 0f, b.z));

    private bool EnsureAgentOnNavMesh(float sampleRadius = 6f)
    {
        if (agent == null || !agent.enabled)
            return false;

        if (agent.isOnNavMesh)
            return true;

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
            return agent.isOnNavMesh;
        }

        if (!warnedNavMeshMissing)
        {
            Debug.LogWarning($"{name}: PrideAI could not find NavMesh near current position.");
            warnedNavMeshMissing = true;
        }

        return false;
    }

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
