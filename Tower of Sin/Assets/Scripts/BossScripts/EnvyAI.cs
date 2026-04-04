using UnityEngine;
using UnityEngine.AI;
using TMPro;
using UnityEngine.UI;
using System.Collections;

// ─────────────────────────────────────────────────────────────────────────────
// EnvyAI — Envy Boss (Sin #1)
//
// Sin mechanic: Envy copies the player's fighting style.
//   Phase 1: Watches the player — records whether quick or heavy attacks land
//            more often, then mirrors the dominant style back.
//   Phase 2 (below 60% HP): Mimic mode active — attack damage multiplied,
//            cooldown cut, plays roar.
//   Throughout: Moves faster when the player is moving (jealous of agility).
//
// AI: FSM with probabilistic phase transitions.
//     States: Idle → Chase → Attack → Mimic (phase 2)
// ─────────────────────────────────────────────────────────────────────────────

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(AudioSource))]
public class EnvyAI : MonoBehaviour
{
    // ── Health & Phase ────────────────────────────────────────────────────────
    public float maxHealth          = 250f;
    private float currentHealth;
    private int   currentPhase      = 1;
    private bool  phase2Triggered   = false;
    public float  phase2Threshold   = 0.60f;

    // ── Envy — Copy Mechanic ──────────────────────────────────────────────────
    // Tracks how many quick vs heavy hits the player has landed
    private int   quickHitsReceived = 0;
    private int   heavyHitsReceived = 0;

    // Once mimic is active, dominant attack style determines damage/cooldown
    private bool  mimicActive           = false;
    public float  mimicDamageMultiplier = 1.3f;
    public float  mimicAttackCooldown   = 1.2f;

    // ── Envy — Jealous Speed ──────────────────────────────────────────────────
    public float  jealousSpeedBoost = 1.5f;
    private bool  playerIsMoving    = false;
    private Vector3 lastPlayerPos   = Vector3.zero;

    // ── Damage ────────────────────────────────────────────────────────────────
    public float  damageToPlayer    = 18f;
    private float damageMultiplier  = 1f;

    // ── UI ────────────────────────────────────────────────────────────────────
    public GameObject  uiCanvasObject;
    public TMP_Text    healthText;
    public Image       healthBarFill;
    public float       healthDrainSpeed       = 5f;
    public float       deathAnimationDuration = 2.5f;

    // ── Movement ──────────────────────────────────────────────────────────────
    public float walkSpeed      = 4.0f;
    public float aggroRadius    = 18f;
    public float attackRadius   = 2.5f;
    public float attackCooldown = 2.0f;
    public float attackDmgDelay = 0.4f;

    // ── Audio ─────────────────────────────────────────────────────────────────
    public AudioClip hitSound;
    public AudioClip missSound;
    public AudioClip idleSound;
    public AudioClip walkSound;
    public AudioClip roarSound;

    // ── Loot ──────────────────────────────────────────────────────────────────
    public GameObject healthPotionPrefab;
    public float      healthPotChance = 40f;

    // ── Components ────────────────────────────────────────────────────────────
    public Animator animator;

    private Transform    player;
    private Transform    mainCamera;
    private NavMeshAgent agent;
    private PlayerHealth playerHealthScript;
    private AudioSource  sfxSource;
    private AudioSource  walkSource;

    // ── State ─────────────────────────────────────────────────────────────────
    private bool  isDead        = false;
    private bool  isAttacking   = false;
    private bool  hasSeenPlayer = false;
    private float nextAttackTime = 0f;
    private float idleAudioTimer = 0f;

    // ─────────────────────────────────────────────────────────────────────────
    void Start()
    {
        maxHealth     = 250f + ((FloorTextController.floorNumber - 1) * 15f);
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
            lastPlayerPos      = player.position;
        }

        if (Camera.main != null) mainCamera = Camera.main.transform;
        if (healthBarFill != null) healthBarFill.fillAmount = 1f;

        idleAudioTimer = Random.Range(3f, 7f);
        UpdateHealthUI();
    }

    void Update()
    {
        UpdateHealthBar();
        if (isDead || player == null) return;

        // Jealous speed — tracks player movement
        float playerSpeed = Vector3.Distance(player.position, lastPlayerPos) / Mathf.Max(Time.deltaTime, 0.001f);
        lastPlayerPos  = player.position;
        playerIsMoving = playerSpeed > 0.5f;

        float targetSpeed = playerIsMoving ? walkSpeed * jealousSpeedBoost : walkSpeed;
        agent.speed = Mathf.Lerp(agent.speed, targetSpeed, Time.deltaTime * 3f);

        HandleAudio();

        if (!hasSeenPlayer && FlatDist(transform.position, player.position) <= aggroRadius)
            hasSeenPlayer = true;

        if (!hasSeenPlayer) return;

        if (!phase2Triggered && currentHealth / maxHealth <= phase2Threshold)
            TriggerPhase2();

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
                agent.SetDestination(player.position);
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
        mimicActive     = true;

        // Mirror the dominant attack style
        if (heavyHitsReceived >= quickHitsReceived)
        {
            // Player used heavy attacks more — mimic heavier, slower hits
            damageMultiplier  *= mimicDamageMultiplier * 1.2f;
            attackCooldown     = mimicAttackCooldown * 1.3f;
        }
        else
        {
            // Player used quick attacks more — mimic faster, lighter spam
            damageMultiplier  *= mimicDamageMultiplier;
            attackCooldown     = mimicAttackCooldown;
        }

        StartCoroutine(RoarRoutine());
    }

    private IEnumerator RoarRoutine()
    {
        isAttacking     = true;
        agent.isStopped = true;
        animator?.SetTrigger("roar");
        if (roarSound != null) sfxSource.PlayOneShot(roarSound);
        yield return new WaitForSeconds(1.8f);
        isAttacking = false;
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
    //  attackType 1 = quick slash, 2 = heavy slash (mirrors FirstPersonMovement)
    // ─────────────────────────────────────────────────────────────────────────

    public void TakeDamage(float amount, int attackType = 1)
    {
        if (isDead) return;

        // Record what type of attack the player used — for mimic tracking
        if (attackType == 1) quickHitsReceived++;
        else                 heavyHitsReceived++;

        currentHealth -= amount;
        currentHealth  = Mathf.Max(0f, currentHealth);
        UpdateHealthUI();

        if (currentHealth <= 0f) Die();
        else if (!phase2Triggered && currentHealth / maxHealth <= phase2Threshold)
            TriggerPhase2();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Death
    // ─────────────────────────────────────────────────────────────────────────

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        StopAllCoroutines();
        animator?.SetTrigger("die");
        if (agent.enabled) agent.enabled = false;
        GetComponent<Collider>().enabled  = false;

        if (healthText != null) healthText.text = "";
        walkSource?.Stop();
        sfxSource?.Stop();

        if (healthPotionPrefab != null && Random.Range(0f, 100f) < healthPotChance)
            Instantiate(healthPotionPrefab, transform.position + Vector3.up * 0.2f, Quaternion.identity);

        StartCoroutine(HideUIAfterDeath());
    }

    private IEnumerator HideUIAfterDeath()
    {
        yield return new WaitForSeconds(deathAnimationDuration);
        if (uiCanvasObject != null) uiCanvasObject.SetActive(false);
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
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 8f);
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
        if (healthBarFill == null) return;
        healthBarFill.fillAmount = Mathf.Lerp(
            healthBarFill.fillAmount, currentHealth / maxHealth, Time.deltaTime * healthDrainSpeed);
    }

    private void UpdateHealthUI()
    {
        if (healthText != null) healthText.text = (int)currentHealth + "/" + (int)maxHealth;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aggroRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadius);
    }
}
