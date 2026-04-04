using UnityEngine;
using UnityEngine.AI;
using TMPro;
using UnityEngine.UI;
using System.Collections;

// ─────────────────────────────────────────────────────────────────────────────
// GreedAI — Greed Boss (Sin #4)
//
// Sin mechanic: Greed "collects" power over time — becomes stronger as the
//               fight drags on.
//   Passive:    Gains an armor stack every 8 seconds (reduces incoming damage).
//               Max 8 stacks.
//   Phase 1:    Standard melee. Launches gold coin projectiles in a spread.
//   Phase 2 (below 50% HP): Becomes obsessed — charges the player in a
//                            straight line dealing high damage.
//
// AI: FSM — Idle → Chase → Attack → CoinSpread → Charge (phase 2)
// ─────────────────────────────────────────────────────────────────────────────

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(AudioSource))]
public class GreedAI : MonoBehaviour
{
    // ── Health & Phase ────────────────────────────────────────────────────────
    public float maxHealth          = 350f;
    private float currentHealth;
    private int   currentPhase      = 1;
    private bool  phase2Triggered   = false;
    public float  phase2Threshold   = 0.50f;

    // ── Greed — Armor Stacking ────────────────────────────────────────────────
    public float armorGainPerStack  = 10f;  // flat damage reduction per stack
    public float armorGainInterval  = 8f;
    public int   maxArmorStacks     = 8;
    private int   armorStacks       = 0;
    private float armorTimer        = 0f;

    // ── Greed — Coin Spread ───────────────────────────────────────────────────
    public GameObject coinProjectilePrefab;
    public int   coinSpreadCount    = 5;
    public float coinSpreadAngle    = 40f;
    public float coinProjectileSpeed = 12f;
    public float coinCooldown       = 4f;
    private float coinTimer         = 0f;

    // ── Greed — Phase 2 Charge ────────────────────────────────────────────────
    public float chargeSpeed        = 14f;
    public float chargeCooldown     = 5f;
    public float chargeDamage       = 35f;
    public float chargeDuration     = 0.6f;
    private float chargeTimer       = 0f;

    // ── Damage ────────────────────────────────────────────────────────────────
    public float  damageToPlayer    = 20f;
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
    public float attackCooldown = 2.0f;
    public float attackDmgDelay = 0.45f;

    // ── Audio ─────────────────────────────────────────────────────────────────
    public AudioClip hitSound;
    public AudioClip missSound;
    public AudioClip idleSound;
    public AudioClip walkSound;
    public AudioClip roarSound;
    public AudioClip chargeSound;

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
    private bool  isDead         = false;
    private bool  isAttacking    = false;
    private bool  hasSeenPlayer  = false;
    private float nextAttackTime = 0f;
    private float idleAudioTimer = 0f;

    // ─────────────────────────────────────────────────────────────────────────
    void Start()
    {
        maxHealth     = 350f + ((FloorTextController.floorNumber - 1) * 18f);
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

        float dt = Time.deltaTime;

        // Armor stacking — always ticking
        armorTimer += dt;
        if (armorTimer >= armorGainInterval && armorStacks < maxArmorStacks)
        {
            armorStacks++;
            armorTimer = 0f;
        }

        if (!hasSeenPlayer && FlatDist(transform.position, player.position) <= aggroRadius)
            hasSeenPlayer = true;

        if (!hasSeenPlayer) return;

        if (!phase2Triggered && currentHealth / maxHealth <= phase2Threshold)
            TriggerPhase2();

        // Coin spread — fires regardless of melee attack state
        coinTimer += dt;
        if (coinTimer >= coinCooldown && !isAttacking)
        {
            coinTimer = 0f;
            FireCoinSpread();
        }

        // Phase 2 charge
        if (currentPhase >= 2)
        {
            chargeTimer += dt;
            if (chargeTimer >= chargeCooldown && !isAttacking)
            {
                chargeTimer = 0f;
                StartCoroutine(ChargeAttack());
            }
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
    //  Phase 2
    // ─────────────────────────────────────────────────────────────────────────

    private void TriggerPhase2()
    {
        phase2Triggered = true;
        currentPhase    = 2;
        chargeTimer     = 1f;   // first charge comes quickly
        damageMultiplier *= 1.25f;
        animator?.SetTrigger("roar");
        if (roarSound != null) sfxSource.PlayOneShot(roarSound);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Coin spread projectiles
    // ─────────────────────────────────────────────────────────────────────────

    private void FireCoinSpread()
    {
        if (coinProjectilePrefab == null || player == null) return;

        Vector3 baseDir  = (player.position - transform.position).normalized;
        float   halfAngle = coinSpreadAngle / 2f;
        float   step      = coinSpreadCount > 1 ? coinSpreadAngle / (coinSpreadCount - 1) : 0f;

        for (int i = 0; i < coinSpreadCount; i++)
        {
            float   angle = -halfAngle + step * i;
            Vector3 dir   = Quaternion.Euler(0f, angle, 0f) * baseDir;

            GameObject coin = Instantiate(
                coinProjectilePrefab,
                transform.position + Vector3.up * 1f,
                Quaternion.LookRotation(dir));

            Rigidbody rb = coin.GetComponent<Rigidbody>();
            if (rb == null) rb = coin.AddComponent<Rigidbody>();
            rb.useGravity    = false;
            rb.linearVelocity = dir * coinProjectileSpeed;

            GreedCoinProjectile cp = coin.AddComponent<GreedCoinProjectile>();
            cp.damage = 15f;

            Destroy(coin, 4f);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Charge attack (phase 2)
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator ChargeAttack()
    {
        if (isAttacking) yield break;
        isAttacking = true;

        if (player == null) { isAttacking = false; yield break; }

        Vector3 chargeDir = (player.position - transform.position).normalized;
        chargeDir.y = 0f;

        agent.isStopped = true;
        FacePlayer();

        if (chargeSound != null) sfxSource.PlayOneShot(chargeSound);

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity = chargeDir * chargeSpeed;

        float elapsed  = 0f;
        bool  hitPlayer = false;

        while (elapsed < chargeDuration)
        {
            if (!hitPlayer)
            {
                float dist = Vector3.Distance(transform.position, player.position);
                if (dist < 1.8f && playerHealthScript != null)
                {
                    playerHealthScript.TakeDamage(chargeDamage * damageMultiplier);
                    hitPlayer = true;
                }
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (rb != null) rb.linearVelocity = Vector3.zero;
        agent.isStopped = false;
        isAttacking     = false;
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
    //  Damage — armor stacks reduce incoming damage
    // ─────────────────────────────────────────────────────────────────────────

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        float reduction = armorStacks * armorGainPerStack;
        float reduced   = Mathf.Max(1f, amount - reduction);

        currentHealth -= reduced;
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

// ─────────────────────────────────────────────────────────────────────────────
// GreedCoinProjectile — damages player on trigger contact
// Self-contained, spawned by GreedAI.FireCoinSpread()
// ─────────────────────────────────────────────────────────────────────────────
public class GreedCoinProjectile : MonoBehaviour
{
    public float damage = 15f;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerHealth ph = other.GetComponent<PlayerHealth>();
        if (ph == null) ph = other.GetComponentInParent<PlayerHealth>();
        if (ph != null) ph.TakeDamage(damage);

        Destroy(gameObject);
    }
}
