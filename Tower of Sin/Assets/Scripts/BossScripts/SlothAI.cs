using UnityEngine;
using UnityEngine.AI;
using TMPro;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(AudioSource))]
public class SlothAI : MonoBehaviour
{
    // Health & phase
    public float maxHealth = 900f;
    private float currentHealth;
    private int currentPhase = 1;
    private bool phase2Triggered = false;
    public float phase2Threshold = 0.40f;

    // Torpor
    public float torporHealPercent = 0.20f;
    public float torporDuration = 4f;
    private bool torpored = false;
    private bool torporDone = false;

    // Damage
    public float damageToPlayer = 20f;
    private float damageMultiplier = 1f;

    // UI
    public GameObject uiCanvasObject;
    public TMP_Text healthText;
    public Image healthBarFill;
    public float healthDrainSpeed = 5f;
    public float deathAnimationDuration = 2.5f;
    
    [Header("Shared Boss UI (Optional)")]
    [SerializeField] private Image bossHealthBarFill;
    [SerializeField] private TMP_Text bossHealthText;
    [SerializeField] private GameObject bossHealthUIRoot;

    // Movement
    public float walkSpeed = 1.0f;   // very slow
    public float aggroRadius = 16f;
    public float attackRadius = 2.5f;
    public float attackCooldown = 2.5f;
    public float attackDmgDelay = 0.5f;

    // Audio
    public AudioClip hitSound;
    public AudioClip missSound;
    public AudioClip idleSound;
    public AudioClip walkSound;
    public AudioClip roarSound;

    // Loot
    public GameObject healthPotionPrefab;
    public float healthPotChance = 45f;

    // Components
    public Animator animator;

    private Transform player;
    private Transform mainCamera;
    private NavMeshAgent agent;
    private PlayerHealth playerHealthScript;
    private AudioSource sfxSource;
    private AudioSource walkSource;

    // State
    private bool isDead = false;
    private bool isAttacking = false;
    private bool hasSeenPlayer = false;
    private float nextAttackTime = 0f;
    private float idleAudioTimer = 0f;
    private int currentFloor = 5;

    public void SetFloor(int floor)
    {
        currentFloor = Mathf.Max(1, floor);
    }

    public void SetupArenaReferences(Image sharedHealthBarFill, TMP_Text sharedHealthText, GameObject sharedHealthUIRoot)
    {
        bossHealthBarFill = sharedHealthBarFill;
        bossHealthText = sharedHealthText;
        bossHealthUIRoot = sharedHealthUIRoot;

        if (bossHealthUIRoot != null)
            bossHealthUIRoot.SetActive(true);

        UpdateHealthUI();
    }

    private float GetScaledHealthForFloor(int floor)
    {
        return 900f + ((floor - 1) * 35f);
    }

    void Start()
    {
        // Massive HP, also scales by floor
        if (currentFloor <= 0)
            currentFloor = Mathf.Max(1, FloorTextController.floorNumber);

        maxHealth = GetScaledHealthForFloor(currentFloor);
        currentHealth = maxHealth;

        agent = GetComponent<NavMeshAgent>();
        agent.speed = walkSpeed;

        sfxSource = GetComponent<AudioSource>();
        sfxSource.spatialBlend = 1f;
        sfxSource.rolloffMode = AudioRolloffMode.Linear;
        sfxSource.minDistance = 2f;
        sfxSource.maxDistance = 12f;

        walkSource = gameObject.AddComponent<AudioSource>();
        walkSource.spatialBlend = 1f;
        walkSource.rolloffMode = AudioRolloffMode.Linear;
        walkSource.minDistance = 2f;
        walkSource.maxDistance = 18f;
        walkSource.clip = walkSound;
        walkSource.loop = true;

        GameObject pObj = GameObject.FindGameObjectWithTag("Player");
        if (pObj != null)
        {
            player = pObj.transform;
            playerHealthScript = pObj.GetComponent<PlayerHealth>();
        }

        if (Camera.main != null) mainCamera = Camera.main.transform;
        if (bossHealthUIRoot != null) bossHealthUIRoot.SetActive(true);
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

        // Phase 2 torpor trigger
        if (!phase2Triggered && !torporDone && currentHealth / maxHealth <= phase2Threshold)
        {
            phase2Triggered = true;
            currentPhase = 2;
            StartCoroutine(TorporRoutine());
            return;
        }

        if (torpored || isAttacking) return;

        float dist = FlatDist(transform.position, player.position);

        if (dist <= aggroRadius)
        {
            FacePlayer();

            if (dist <= attackRadius && Time.time >= nextAttackTime)
            {
                StartCoroutine(AttackRoutine());
            }
            else if (dist > attackRadius)
            {
                agent.isStopped = false;
                agent.SetDestination(player.position);
                animator?.SetBool("iswalking", true);

                if (!walkSource.isPlaying) walkSource.Play();
            }
            else
            {
                agent.isStopped = true;
                animator?.SetBool("iswalking", false);
                walkSource.Pause();
            }
        }
        else
        {
            agent.isStopped = true;
            animator?.SetBool("iswalking", false);
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

    private IEnumerator TorporRoutine()
    {
        torpored = true;
        isAttacking = true;

        if (agent != null) agent.isStopped = true;
        animator?.SetBool("iswalking", false);
        walkSource?.Pause();
        animator?.SetTrigger("roar");

        float elapsed = 0f;
        float healTotal = maxHealth * torporHealPercent;

        while (elapsed < torporDuration)
        {
            currentHealth = Mathf.Min(maxHealth, currentHealth + (healTotal / torporDuration) * Time.deltaTime);
            UpdateHealthUI();
            elapsed += Time.deltaTime;
            yield return null;
        }

        torporDone = true;
        torpored = false;
        isAttacking = false;

        if (agent != null) agent.isStopped = false;

        // wake up slightly stronger, still slow
        agent.speed = walkSpeed * 1.15f;
        attackCooldown *= 0.8f;
        damageMultiplier *= 1.2f;
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        agent.isStopped = true;
        FacePlayer();

        int attackChoice = Random.Range(0, 2);
        if (attackChoice == 0)
            animator?.SetTrigger("attack");
        else
            animator?.SetTrigger("attack2");

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
        isAttacking = false;

        if (agent != null) agent.isStopped = false;
    }

    public void TakeDamage(float amount)
    {
        if (isDead || torpored) return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(0f, currentHealth);
        UpdateHealthUI();

        if (currentHealth <= 0f) Die();
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        StopAllCoroutines();
        animator?.SetTrigger("die");

        if (agent != null && agent.enabled) agent.enabled = false;

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        TMP_Text targetText = bossHealthText != null ? bossHealthText : healthText;
        if (targetText != null) targetText.text = "";
        walkSource?.Stop();
        sfxSource?.Stop();

        if (healthPotionPrefab != null && Random.Range(0f, 100f) < healthPotChance)
            Instantiate(healthPotionPrefab, transform.position + Vector3.up * 0.2f, Quaternion.identity);

        StartCoroutine(HideUIAfterDeath());
    }

    private IEnumerator HideUIAfterDeath()
    {
        yield return new WaitForSeconds(deathAnimationDuration);
        if (bossHealthUIRoot != null) bossHealthUIRoot.SetActive(false);
        else if (uiCanvasObject != null) uiCanvasObject.SetActive(false);
    }

    private void FacePlayer()
    {
        if (player == null) return;

        Vector3 dir = player.position - transform.position;
        dir.y = 0f;

        if (dir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(dir),
                Time.deltaTime * 6f
            );
    }

    private float FlatDist(Vector3 a, Vector3 b)
    {
        return Vector3.Distance(new Vector3(a.x, 0f, a.z), new Vector3(b.x, 0f, b.z));
    }

    private void HandleAudio()
    {
        float vertDist = Mathf.Abs(player.position.y - transform.position.y);
        bool sameFloor = vertDist < 2.5f;

        sfxSource.mute = !sameFloor;
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
}
