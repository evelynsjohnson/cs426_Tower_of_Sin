using UnityEngine;
using UnityEngine.AI;
using TMPro;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(AudioSource))]
public class PrisonZombieAI : MonoBehaviour
{

    //public float maxHealth = 100f;
    public float maxHealth = 100f + (FloorTextController.floorNumber * 5f); // +5 hp per floor
    private float currentHealth;
    public float damageToPlayer = 10f; 
    
    private bool hasMadeLowHealthDecision = false;
    public bool isBlocking = false;
    private bool isFleeing = false;
    public float blockDuration = 1f; // How long the zombie holds the block

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
    private bool isReturning = false;
    private bool isDead = false;
    private bool isAttacking = false;
    private bool hasSeenPlayer = false;

    private float nextAttackTime = 0f;
    private float idleAudioTimer = 0f;
    private float lastDamageTime = 0f;
    public float fleeSafeDistance = 5f;
    public float healDelay = 5f;
    void Start()
    {
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

        if (isDead || player == null || isAttacking) return;

        // hp regen
        if (Time.time - lastDamageTime >= healDelay && currentHealth < maxHealth)
        {
            // +1/5th of max HP per sec
            currentHealth += (maxHealth / 5f) * Time.deltaTime;
            if (currentHealth > maxHealth) currentHealth = maxHealth;
            UpdateHealthUI();
        }

        // Calculate Horizontal (XZ) distance only
        Vector3 flatPlayerPos = new Vector3(player.position.x, transform.position.y, player.position.z);
        float flatDistanceToPlayer = Vector3.Distance(transform.position, flatPlayerPos);

        // run away
        if (isFleeing)
        {
            if (flatDistanceToPlayer < fleeSafeDistance)
            {
                Vector3 dirAwayFromPlayer = (transform.position - flatPlayerPos).normalized;
                Vector3 fleePos = transform.position + (dirAwayFromPlayer * 2f);

                agent.isStopped = false;
                agent.SetDestination(fleePos);
                animator.SetBool("isWalking", true);
            }
            else
            {
                agent.isStopped = true;
                animator.SetBool("isWalking", false);
            }
            return;
        }

        Vector3 flatSpawnPos = new Vector3(initialSpawnPosition.x, transform.position.y, initialSpawnPosition.z);
        float flatDistanceToSpawn = Vector3.Distance(transform.position, flatSpawnPos);

        // Mute audio if vertical distance is > 2.5 meters (hard coded kinda)
        float verticalDistance = Mathf.Abs(player.position.y - transform.position.y);
        bool onSameFloor = verticalDistance < 2.5f;
        sfxAudioSource.mute = !onSameFloor;
        walkAudioSource.mute = !onSameFloor;

        idleAudioTimer -= Time.deltaTime;
        if (idleAudioTimer <= 0f)
        {
            if (idleSound != null && onSameFloor) sfxAudioSource.PlayOneShot(idleSound);
            idleAudioTimer = Random.Range(2f, 5f);
        }

        if (flatDistanceToSpawn > maxLeashDistance)
        {
            isReturning = true;
        }

        if (isReturning)
        {
            agent.SetDestination(initialSpawnPosition);
            animator.SetBool("isWalking", true);

            if (currentHealth < maxHealth)
            {
                currentHealth += 10f * Time.deltaTime;
                if (currentHealth > maxHealth) currentHealth = maxHealth;
                UpdateHealthUI();
            }

            if (flatDistanceToSpawn <= agent.stoppingDistance + 0.5f)
            {
                isReturning = false;
                hasSeenPlayer = false;
                animator.SetBool("isWalking", false);
            }
            return;
        }

        // Line of sight
        if (!hasSeenPlayer && flatDistanceToPlayer <= aggroRadius && onSameFloor)
        {
            Vector3 dirToPlayer = (player.position - transform.position).normalized;
            if (Physics.Raycast(transform.position + Vector3.up, dirToPlayer, out RaycastHit hit, aggroRadius))
            {
                if (hit.collider.CompareTag("Player"))
                {
                    hasSeenPlayer = true;
                }
            }
        }

        if (hasSeenPlayer && flatDistanceToPlayer <= aggroRadius)
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

        if (isFleeing)
        {
            // Calculate a point 5 units directly away from the player
            Vector3 dirAwayFromPlayer = (transform.position - player.position).normalized;
            Vector3 fleePos = transform.position + (dirAwayFromPlayer * 5f);

            agent.isStopped = false;
            agent.SetDestination(fleePos);
            animator.SetBool("isWalking", true);
            return;
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

        // Reset the heal timer and wake up the zombie
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
        // probability trigger (once per enemy when hp < 25%)
        else if (currentHealth <= maxHealth * 0.25f && !hasMadeLowHealthDecision)
        {
            hasMadeLowHealthDecision = true;
            MakeLowHealthDecision();
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
        isDead = true;
        animator.SetTrigger("die");

        agent.enabled = false;
        GetComponent<Collider>().enabled = false;

        if (healthText != null) healthText.text = "";
        if (walkAudioSource != null) walkAudioSource.Stop();
        if (sfxAudioSource != null) sfxAudioSource.Stop();      

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

    private void MakeLowHealthDecision()
    {
        float roll = Random.Range(0f, 100f);

        if (roll < 5f) // 5% chance to take a block action
        {
            StartCoroutine(BlockRoutine());
        }
        else if (roll < 85f) // 80% chance to be enraged
        {
            StartCoroutine(EnrageRoutine());
        }
        else // Remaining 15% chance to walk away
        {
            isFleeing = true;
        }
    }

    private IEnumerator BlockRoutine()
    {
        isBlocking = true;
        animator.SetTrigger("block");
        agent.isStopped = true;

        yield return new WaitForSeconds(blockDuration);

        isBlocking = false;
        agent.isStopped = false;
    }

    // "rage" chance
    private IEnumerator EnrageRoutine()
    {
        isAttacking = true;
        animator.SetTrigger("roar");

        if (roarSound != null)
        {
            sfxAudioSource.PlayOneShot(roarSound);
        }

        agent.isStopped = true;

        yield return new WaitForSeconds(1.5f);

        // Buff the zombie
        damageToPlayer *= 2f;
        attackCooldown /= 2f;
        animator.speed = 2f;

        isAttacking = false;
        agent.isStopped = false;
    }


}
