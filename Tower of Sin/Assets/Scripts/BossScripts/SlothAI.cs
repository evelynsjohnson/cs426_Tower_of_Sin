using UnityEngine;
using UnityEngine.AI;
using TMPro;
using UnityEngine.UI;
using System.Collections;

// ─────────────────────────────────────────────────────────────────────────────
// SlothAI — Sloth Boss (Sin #3)
//
// Sin mechanic: Sloth barely moves but is almost unkillable and constantly
//               spawns waves of minions.
//   Phase 1: Slow lumbering movement. Summons zombie minions every N seconds.
//            Very high base HP (1.4x multiplier).
//   Phase 2 (below 40% HP): Torpor — briefly stops moving, becomes INVINCIBLE,
//            heals 20% max HP, then wakes up more aggressive.
//
// AI: FSM — Idle → Chase → Attack → Summon → Torpor
// ─────────────────────────────────────────────────────────────────────────────

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(AudioSource))]
public class SlothAI : MonoBehaviour
{
    // ── Health & Phase ────────────────────────────────────────────────────────
    public float maxHealth          = 560f;   // 400 * 1.4 base
    private float currentHealth;
    private int   currentPhase      = 1;
    private bool  phase2Triggered   = false;
    public float  phase2Threshold   = 0.40f;

    // ── Sloth — Minion Summon ─────────────────────────────────────────────────
    public GameObject minionPrefab;
    public int        minionSpawnCount    = 2;
    public float      minionSummonCooldown = 10f;
    public float      minionSpawnRadius   = 6f;
    private float     summonTimer         = 3f;   // first wave comes quickly

    // ── Sloth — Torpor ────────────────────────────────────────────────────────
    public float torporHealPercent = 0.20f;  // heals 20% max HP during torpor
    public float torporDuration    = 4f;
    private bool torpored          = false;
    private bool torporDone        = false;

    // ── Damage ────────────────────────────────────────────────────────────────
    public float damageToPlayer    = 20f;
    private float damageMultiplier = 1f;

    // ── UI ────────────────────────────────────────────────────────────────────
    public GameObject  uiCanvasObject;
    public TMP_Text    healthText;
    public Image       healthBarFill;
    public float       healthDrainSpeed       = 5f;
    public float       deathAnimationDuration = 2.5f;

    // ── Movement ──────────────────────────────────────────────────────────────
    public float walkSpeed      = 1.5f;   // very slow
    public float aggroRadius    = 16f;
    public float attackRadius   = 2.5f;
    public float attackCooldown = 2.5f;
    public float attackDmgDelay = 0.5f;

    // ── Audio ─────────────────────────────────────────────────────────────────
    public AudioClip hitSound;
    public AudioClip missSound;
    public AudioClip idleSound;
    public AudioClip walkSound;
    public AudioClip roarSound;
    public AudioClip summonSound;

    // ── Loot ──────────────────────────────────────────────────────────────────
    public GameObject healthPotionPrefab;
    public float      healthPotChance = 45f;

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
        // Sloth has 40% more HP than base, scaled with floor
        maxHealth     = (400f * 1.4f) + ((FloorTextController.floorNumber - 1) * 20f);
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

        HandleAudio();

        // Minion summon timer always ticks (even before aggro)
        if (!torpored)
        {
            summonTimer -= Time.deltaTime;
            if (summonTimer <= 0f) { SummonMinions(); summonTimer = minionSummonCooldown; }
        }

        if (!hasSeenPlayer && FlatDist(transform.position, player.position) <= aggroRadius)
            hasSeenPlayer = true;

        if (!hasSeenPlayer) return;

        // Check torpor trigger
        if (!phase2Triggered && !torporDone && currentHealth / maxHealth <= phase2Threshold)
        {
            phase2Triggered = true;
            currentPhase    = 2;
            StartCoroutine(TorporRoutine());
            return;
        }

        if (torpored || isAttacking) return;

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
    //  Summon minions
    // ─────────────────────────────────────────────────────────────────────────

    private void SummonMinions()
    {
        if (minionPrefab == null) return;

        animator?.SetTrigger("roar");
        if (summonSound != null) sfxSource.PlayOneShot(summonSound);
        else if (roarSound != null) sfxSource.PlayOneShot(roarSound);

        for (int i = 0; i < minionSpawnCount; i++)
        {
            Vector2 rand2D   = Random.insideUnitCircle * minionSpawnRadius;
            Vector3 spawnPos = transform.position + new Vector3(rand2D.x, 0f, rand2D.y);

            NavMeshHit hit;
            if (NavMesh.SamplePosition(spawnPos, out hit, minionSpawnRadius, NavMesh.AllAreas))
                Instantiate(minionPrefab, hit.position, Quaternion.identity);
            else
                Instantiate(minionPrefab, spawnPos, Quaternion.identity);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Torpor — invincible heal + wake-up enrage
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator TorporRoutine()
    {
        torpored    = true;
        isAttacking = true;

        if (agent != null) agent.isStopped = true;
        animator?.SetBool("isWalking", false);
        walkSource?.Pause();
        animator?.SetTrigger("roar");

        float elapsed  = 0f;
        float healTotal = maxHealth * torporHealPercent;

        while (elapsed < torporDuration)
        {
            currentHealth  = Mathf.Min(maxHealth, currentHealth + (healTotal / torporDuration) * Time.deltaTime);
            UpdateHealthUI();
            elapsed += Time.deltaTime;
            yield return null;
        }

        torporDone  = true;
        torpored    = false;
        isAttacking = false;

        if (agent != null) agent.isStopped = false;

        // Wake up angrier
        agent.speed            = walkSpeed * 1.3f;
        attackCooldown        *= 0.75f;
        damageMultiplier      *= 1.2f;
        summonTimer             = 2f;   // immediate next wave
        minionSummonCooldown   *= 0.75f;
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
    //  Damage — invincible during torpor
    // ─────────────────────────────────────────────────────────────────────────

    public void TakeDamage(float amount)
    {
        if (isDead || torpored) return;  // invincible during torpor

        currentHealth -= amount;
        currentHealth  = Mathf.Max(0f, currentHealth);
        UpdateHealthUI();

        if (currentHealth <= 0f) Die();
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
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 6f);
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
            idleAudioTimer = Random.Range(5f, 10f);
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
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, minionSpawnRadius);
    }
}
