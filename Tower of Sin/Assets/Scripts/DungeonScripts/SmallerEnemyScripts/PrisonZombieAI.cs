using UnityEngine;
using UnityEngine.AI;
using TMPro;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(AudioSource))]
public class PrisonZombieAI : MonoBehaviour
{
    [Header("Stats")]
    public float maxHealth = 100f;
    private float currentHealth;
    public float damageToPlayer = 10f;

    [Header("UI & Health Bar")]
    public GameObject uiCanvasObject;
    public TMP_Text healthText;
    public Image healthBarFill;
    public float healthDrainSpeed = 5f;
    public float deathAnimationDuration = 2f;

    [Header("Ranges & AI")]
    public float aggroRadius = 10f;
    public float attackRadius = 2f;
    public float maxLeashDistance = 20f;
    public float walkSpeed = 3.5f;

    [Header("Attack Timing")]
    public float attackCooldown = 2f;
    public float attackDamageDelay = 0.5f;

    [Header("Audio")]
    public AudioClip hitSound;
    public AudioClip missSound;
    public AudioClip idleSound;
    public AudioClip walkSound;

    [Header("References")]
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

        // Calculate Horizontal (XZ) distance only
        Vector3 flatPlayerPos = new Vector3(player.position.x, transform.position.y, player.position.z);
        float flatDistanceToPlayer = Vector3.Distance(transform.position, flatPlayerPos);

        Vector3 flatSpawnPos = new Vector3(initialSpawnPosition.x, transform.position.y, initialSpawnPosition.z);
        float flatDistanceToSpawn = Vector3.Distance(transform.position, flatSpawnPos);

        // Mute audio if vertical distance is > 2.5 meters (on another floor)
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
        // Potentially remove
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

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        // Wakes up zombie if hit from out of sight
        hasSeenPlayer = true;

        currentHealth -= amount;
        UpdateHealthUI();

        if (currentHealth <= 0)
        {
            Die();
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
}