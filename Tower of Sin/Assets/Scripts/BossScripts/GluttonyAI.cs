using UnityEngine;
using UnityEngine.AI;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Diagnostics;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(AudioSource))]
public class GluttonyAI : MonoBehaviour
{
    public float maxHealth = 500f;
    private float currentHealth;
    public float damageToPlayer = 10f;

    // HP Decision Trackers
    private bool rolledFull = false;
    private bool rolledHalf = false;
    private bool rolledQuarter = false;

    public bool isBlocking = false;
    // private bool isFleeing = false;
    public float blockDuration = 1f;

    public GameObject uiCanvasObject;
    public TMP_Text healthText;
    public Image healthBarFill;
    public float healthDrainSpeed = 5f;
    public float deathAnimationDuration = 2f;

    public float aggroRadius = 10f;
    public float attackRadius = 2f;
    public float maxLeashDistance = 20f;
    public float walkSpeed = 3.5f;

    public float attackCooldown = 2f;
    public float attackDamageDelay = 0.5f;

    public AudioClip hitSound;
    public AudioClip missSound;
    public AudioClip idleSound;
    public AudioClip walkSound;
    public AudioClip roarSound;

    public Animator animator;
    private Transform player;
    private Transform mainCamera;
    private NavMeshAgent agent;
    private PlayerHealth playerHealthScript;

    private AudioSource sfxAudioSource;
    private AudioSource walkAudioSource;

    private Vector3 initialSpawnPosition;
    private bool isDead = false;
    private bool isAttacking = false;
    private bool hasSeenPlayer = false;

    private float nextAttackTime = 0f;
    private float idleAudioTimer = 0f;
    private float lastDamageTime = 0f;
    public float fleeSafeDistance = 5f;
    public float healDelay = 5f;

    public float healthPotChance = 40f;
    public GameObject healthPotionPrefab;

    private bool isJumping = false;
    public float jumpHeight = 5f;
    public float jumpDuration = 1f;

    void Start()
    {
        maxHealth = 500f + ((FloorTextController.floorNumber - 5) * 5f);

        agent = GetComponent<NavMeshAgent>();

        sfxAudioSource = GetComponent<AudioSource>();
        sfxAudioSource.spatialBlend = 1f;
        sfxAudioSource.rolloffMode = AudioRolloffMode.Linear;
        sfxAudioSource.minDistance = 2f;
        sfxAudioSource.maxDistance = 8f;

        walkAudioSource = gameObject.AddComponent<AudioSource>();
        walkAudioSource.spatialBlend = 1f;
        walkAudioSource.rolloffMode = AudioRolloffMode.Linear;
        walkAudioSource.minDistance = 2f;
        walkAudioSource.maxDistance = 15f;
        walkAudioSource.clip = walkSound;
        walkAudioSource.loop = true;

        agent.speed = walkSpeed;
        currentHealth = maxHealth;
        initialSpawnPosition = transform.position;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerHealthScript = playerObj.GetComponent<PlayerHealth>();
        }

        if (Camera.main != null)
        {
            mainCamera = Camera.main.transform;
        }

        if (healthBarFill != null) healthBarFill.fillAmount = 1f;

        idleAudioTimer = Random.Range(2f, 5f);
        UpdateHealthUI();
    }

    void Update()
    {
        if (healthBarFill != null)
        {
            float targetFill = currentHealth / maxHealth;
            healthBarFill.fillAmount = Mathf.Lerp(healthBarFill.fillAmount, targetFill, Time.deltaTime * healthDrainSpeed);
        }

        if (isDead || player == null || isAttacking || isBlocking || isJumping) return;

        // hp regen
        if (Time.time - lastDamageTime >= healDelay && currentHealth < maxHealth)
        {
            currentHealth += (maxHealth / 5f) * Time.deltaTime;
            if (currentHealth > maxHealth) currentHealth = maxHealth;
            UpdateHealthUI();
        }

        Vector3 flatPlayerPos = new Vector3(player.position.x, transform.position.y, player.position.z);
        float flatDistanceToPlayer = Vector3.Distance(transform.position, flatPlayerPos);

        // run away
        // if (isFleeing)
        // {
        //     if (flatDistanceToPlayer < fleeSafeDistance)
        //     {
        //         Vector3 dirAwayFromPlayer = (transform.position - flatPlayerPos).normalized;
        //         Vector3 fleePos = transform.position + (dirAwayFromPlayer * 2f);

        //         agent.isStopped = false;
        //         agent.SetDestination(fleePos);
        //         animator.SetBool("isWalking", true);
        //     }
        //     else
        //     {
        //         agent.isStopped = true;
        //         animator.SetBool("isWalking", false);
        //     }
        //     return;
        // }



        float verticalDistance = Mathf.Abs(player.position.y - transform.position.y);
        bool onSameFloor = verticalDistance < 2.5f;
        if (!onSameFloor && !isJumping && hasSeenPlayer)
        {
            print("Jump");
            StartCoroutine(JumpDownRoutine());
            return;
        }
        // sfxAudioSource.mute = !onSameFloor;
        // walkAudioSource.mute = !onSameFloor;

        idleAudioTimer -= Time.deltaTime;
        if (idleAudioTimer <= 0f)
        {
            if (idleSound != null && onSameFloor) sfxAudioSource.PlayOneShot(idleSound);
            idleAudioTimer = Random.Range(2f, 5f);
        }

        // Vector3 flatSpawnPos = new Vector3(initialSpawnPosition.x, transform.position.y, initialSpawnPosition.z);
        // float flatDistanceToSpawn = Vector3.Distance(transform.position, flatSpawnPos);

        // Aggro trigger
        if (!hasSeenPlayer && flatDistanceToPlayer <= aggroRadius) //&& onSameFloor
        {
            hasSeenPlayer = true;
            CheckHealthThresholds();
        }

        if (hasSeenPlayer) // && flatDistanceToPlayer <= aggroRadius
        {
            if (flatDistanceToPlayer <= attackRadius && Time.time >= nextAttackTime)
            {
                agent.isStopped = true;
                animator.SetBool("isWalking", false);
                StartCoroutine(AttackRoutine());
            }
            else if (flatDistanceToPlayer > attackRadius)
            {
                agent.isStopped = false;
                agent.SetDestination(player.position);
                animator.SetBool("isWalking", true);
            }
            else
            {
                agent.isStopped = true;
                animator.SetBool("isWalking", false);

                Vector3 lookPos = player.position - transform.position;
                lookPos.y = 0;
                if (lookPos != Vector3.zero)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookPos), Time.deltaTime * 5f);
                }
            }
        }
        else
        {
            agent.isStopped = true;
            animator.SetBool("isWalking", false);
        }

        if (animator.GetBool("isWalking") && !walkAudioSource.isPlaying)
        {
            walkAudioSource.Play();
        }
        else if (!animator.GetBool("isWalking") && walkAudioSource.isPlaying)
        {
            walkAudioSource.Pause();
        }
    }

    void LateUpdate()
    {
        if (uiCanvasObject != null && mainCamera != null)
        {
            uiCanvasObject.transform.LookAt(uiCanvasObject.transform.position + mainCamera.forward);
        }
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        animator.SetTrigger("attack");

        yield return new WaitForSeconds(attackDamageDelay);

        if (!isDead && player != null)
        {
            Vector3 flatPlayerPos = new Vector3(player.position.x, transform.position.y, player.position.z);
            float flatDistance = Vector3.Distance(transform.position, flatPlayerPos);

            if (flatDistance <= attackRadius + 0.5f)
            {
                if (playerHealthScript != null) playerHealthScript.TakeDamage(damageToPlayer);
                if (hitSound != null) sfxAudioSource.PlayOneShot(hitSound);
            }
            else
            {
                if (missSound != null) sfxAudioSource.PlayOneShot(missSound);
            }
        }

        nextAttackTime = Time.time + (attackCooldown - attackDamageDelay);
        isAttacking = false;
    }

    public void TakeDamage(float amount, int attackType = 0)
    {
        if (isDead) return;

        lastDamageTime = Time.time;
        hasSeenPlayer = true;

        // blocking
        if (isBlocking && attackType == 1)
        {
            return;
        }

        currentHealth -= amount;
        UpdateHealthUI();

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            CheckHealthThresholds();
        }
    }

    private void CheckHealthThresholds()
    {
        if (currentHealth <= maxHealth * 0.25f && !rolledQuarter)
        {
            rolledQuarter = true;
            RollBehaviorTable();
        }
        else if (currentHealth <= maxHealth * 0.5f && currentHealth > maxHealth * 0.25f && !rolledHalf)
        {
            rolledHalf = true;
            RollBehaviorTable();
        }
        else if (currentHealth >= maxHealth * 0.99f && !rolledFull)
        {
            rolledFull = true;
            RollBehaviorTable();
        }
    }

    private void RollBehaviorTable()
    {
        float roll = Random.Range(0f, 100f);
        float blockChance = 0f;
        float enrageChance = 0f;

        // Assign probabilities
        if (currentHealth <= maxHealth * 0.25f)
        {
            blockChance = 5f;
            enrageChance = 60f;
        }
        else if (currentHealth <= maxHealth * 0.5f)
        {
            blockChance = 15f;
            enrageChance = 20f;
        }
        else
        {
            // Full HP
            blockChance = 10f;
            enrageChance = 10f;
        }

        if (roll < blockChance)
        {
            StartCoroutine(BlockRoutine());
        }
        else if (roll < blockChance + enrageChance)
        {
            StartCoroutine(EnrageRoutine());
        }
    }

    void UpdateHealthUI()
    {
        if (healthText != null)
        {
            healthText.text = (int)currentHealth + "/" + (int)maxHealth;
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        isAttacking = false;
        isBlocking = false;
        // isFleeing = false;

        animator.SetTrigger("die");

        agent.enabled = false;
        GetComponent<Collider>().enabled = false;

        if (healthText != null) healthText.text = "";
        if (walkAudioSource != null) walkAudioSource.Stop();
        if (sfxAudioSource != null) sfxAudioSource.Stop();

        // 20% Chance to drop health potion
        if (healthPotionPrefab != null && Random.Range(0f, 100f) < healthPotChance)
        {
            Instantiate(healthPotionPrefab, transform.position + Vector3.up * .2f, Quaternion.identity);
        }

        StopAllCoroutines();
        StartCoroutine(HideUIAfterDeath());
    }

    private IEnumerator HideUIAfterDeath()
    {
        yield return new WaitForSeconds(deathAnimationDuration);

        if (uiCanvasObject != null)
        {
            uiCanvasObject.SetActive(false);
        }
    }

    private IEnumerator BlockRoutine()
    {
        isBlocking = true;
        animator.SetTrigger("block");
        if (agent.enabled) agent.isStopped = true;

        yield return new WaitForSeconds(blockDuration);

        isBlocking = false;
        if (agent.enabled) agent.isStopped = false;

        // Reroll the table once the block is finished
        if (!isDead)
        {
            RollBehaviorTable();
        }
    }

    private IEnumerator EnrageRoutine()
    {
        isAttacking = true;
        animator.SetTrigger("roar");

        if (roarSound != null)
        {
            sfxAudioSource.PlayOneShot(roarSound);
        }

        if (agent.enabled) agent.isStopped = true;

        yield return new WaitForSeconds(1.5f);

        damageToPlayer *= 1.5f;
        attackCooldown /= 1.5f;
        animator.speed = 1.5f;

        isAttacking = false;
        if (agent.enabled) agent.isStopped = false;
    }

    private IEnumerator JumpDownRoutine()
    {
        isJumping = true;

        // Disable NavMesh so it stops overriding movement
        agent.enabled = false;

        animator.SetTrigger("jump");

        Vector3 startPos = transform.position;

        // Get player's position (flattened for consistency)
        Vector3 playerPos = player.position;

        // Optional: stop slightly short so it doesn't overlap player
        Vector3 direction = (playerPos - startPos).normalized;
        float landingOffset = 1.5f; // tweak this
        Vector3 targetPos = playerPos - direction * landingOffset;

        float time = 0f;

        while (time < jumpDuration)
        {

            float t = time / jumpDuration;

            // Smooth horizontal movement
            Vector3 horizontalPos = Vector3.Lerp(startPos, targetPos, t);

            // Parabolic vertical arc
            float height = 4f * jumpHeight * t * (1 - t);

            transform.position = horizontalPos + Vector3.up * height;

            time += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPos;

        agent.enabled = true;
        agent.Warp(transform.position);

        // Ensure valid navmesh position
        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 2f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
        }

        hasSeenPlayer = true;
        agent.isStopped = false;
        agent.SetDestination(player.position);
        animator.SetBool("isWalking", true);

        isJumping = false;
    }
}