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

    // ── Movement ──────────────────────────────────────────────────────────────
    public float walkSpeed      = 3.5f;
    public float aggroRadius    = 18f;
    public float attackRadius   = 2.5f;
    public float attackCooldown = 2.2f;
    public float attackDmgDelay = 0.45f;

    // ── Audio ─────────────────────────────────────────────────────────────────
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

    // ── State ─────────────────────────────────────────────────────────────────
    private bool  isDead         = false;
    private bool  isAttacking    = false;
    private bool  hasSeenPlayer  = false;
    private float nextAttackTime = 0f;
    private float idleAudioTimer = 0f;

    // ─────────────────────────────────────────────────────────────────────────
    void Start()
    {
        maxHealth     = 400f + ((FloorTextController.floorNumber - 1) * 20f);
        currentHealth = maxHealth;

        agent       = GetComponent<NavMeshAgent>();
        agent.speed = walkSpeed;

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
        if (healthBarFill != null) healthBarFill.fillAmount = 1f;

        shieldActive   = true;
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
