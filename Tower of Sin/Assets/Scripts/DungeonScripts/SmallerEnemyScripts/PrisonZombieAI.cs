using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(Rigidbody))]
public class PrisonZombieAI : MonoBehaviour
{
    public enum ZombieState { Idle, Returning, Pursuing, Fleeing, Attacking, Blocking, Enraged, Dead }
    public ZombieState currentState = ZombieState.Idle;

    public float maxHealth = 100f;
    private float currentHealth;
    public float damageToPlayer = 10f;

    public GameObject uiCanvasObject;
    public TMP_Text healthText;
    public Image healthBarFill;
    public float healthDrainSpeed = 5f;
    public float deathAnimationDuration = 2f;

    public float aggroRadius = 10f;
    public float attackRadius = 2f;
    public float maxLeashDistance = 20f;
    public float walkSpeed = 3.5f;
    public float fleeSafeDistance = 5f;
    public float attackCooldown = 2f;
    public float attackDamageDelay = 0.5f;
    public float blockDuration = 1f;
    public float healDelay = 5f;

    public float viewAngle = 90f; // FOV
    public float viewDistance = 12f;
    public LayerMask visionObstructionMask; // walls, etc.

    public AudioClip hitSound;
    public AudioClip missSound;
    public AudioClip idleSound;
    public AudioClip walkSound;
    public AudioClip roarSound;

    public Animator animator;
    public GameObject healthPotionPrefab;
    public float healthPotChance = 50f; //50% chance

    private Transform player;
    private Transform mainCamera;
    private PlayerHealth playerHealthScript;
    private AudioSource sfxAudioSource;
    private AudioSource walkAudioSource;
    private Rigidbody rb;
    private Vector3 initialSpawnPosition;

    private float nextAttackTime = 0f;
    private float idleAudioTimer = 0f;
    private float lastDamageTime = 0f;
    private bool hasSeenPlayer = false;

    private BayesianBrain bayesianBrain;
    private Vector3 currentMoveTarget;
    void Start()
    {
        // scaling health
        maxHealth = 100f + ((FloorTextController.floorNumber - 1) * 5f);
        currentHealth = maxHealth;
        initialSpawnPosition = transform.position;

        // rigidbody
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
        rb.isKinematic = false;

        // audio
        sfxAudioSource = GetComponent<AudioSource>();
        sfxAudioSource.spatialBlend = 1f;
        sfxAudioSource.rolloffMode = AudioRolloffMode.Linear;
        sfxAudioSource.minDistance = 2f;
        sfxAudioSource.maxDistance = 8f;

        // separate audio source for footsteps
        walkAudioSource = gameObject.AddComponent<AudioSource>();
        walkAudioSource.spatialBlend = 1f;
        walkAudioSource.rolloffMode = AudioRolloffMode.Linear;
        walkAudioSource.minDistance = 2f;
        walkAudioSource.maxDistance = 15f;
        walkAudioSource.clip = walkSound;
        walkAudioSource.loop = true;

        // UI
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerHealthScript = playerObj.GetComponent<PlayerHealth>();
        }

        // camera ref
        if (Camera.main != null) mainCamera = Camera.main.transform;
        if (healthBarFill != null) healthBarFill.fillAmount = 1f;

        idleAudioTimer = Random.Range(2f, 5f);
        UpdateHealthUI();

        bayesianBrain = new BayesianBrain();
    }

    // main movement logic + state handling
    void FixedUpdate()
    {
        if (currentState == ZombieState.Dead || player == null) return;

        switch (currentState)
        {
            case ZombieState.Fleeing:
                HandleFleeing();
                break;
            case ZombieState.Returning:
                HandleReturning();
                break;
            case ZombieState.Pursuing:
                MoveTowardsTarget(player.position);
                break;
            case ZombieState.Idle:
            case ZombieState.Attacking:
            case ZombieState.Blocking:
            case ZombieState.Enraged:
                rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
                break;
        }
    }

    // UI updates + health regen + audio + state transitions
    void Update()
    {
        UpdateUIElements();

        if (currentState == ZombieState.Dead || player == null) return;

        HandleHealthRegen();
        HandleAudioAndAggro();

        if (currentState == ZombieState.Pursuing || currentState == ZombieState.Idle)
        {
            HandlePursuitAndIdleLogic();
        }

        UpdateAnimations();
    }

    // billboard
    void LateUpdate()
    {
        if (uiCanvasObject != null && mainCamera != null)
        {
            uiCanvasObject.transform.LookAt(uiCanvasObject.transform.position + mainCamera.forward);
        }
    }

    // Mvement to target pos
    // both for chasing player + running away from them
    private void MoveTowardsTarget(Vector3 targetPos)
    {
        Vector3 direction = (targetPos - transform.position);
        direction.y = 0;

        if (direction.magnitude > 0.1f)
        {
            Vector3 moveDirection = direction.normalized;

            rb.MovePosition(rb.position + moveDirection * walkSpeed * Time.fixedDeltaTime);

            if (moveDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDirection), Time.fixedDeltaTime * 5f);
            }
        }
    }

    // when fleeing the zombie will try to move away from the player then it will switch back to idle
    private void HandleFleeing()
    {
        Vector3 flatPlayerPos = new Vector3(player.position.x, transform.position.y, player.position.z);
        if (Vector3.Distance(transform.position, flatPlayerPos) < fleeSafeDistance)
        {
            Vector3 dirAwayFromPlayer = (transform.position - flatPlayerPos).normalized;
            currentMoveTarget = transform.position + (dirAwayFromPlayer * 5f);
            MoveTowardsTarget(currentMoveTarget);
        }
        else
        {
            currentState = ZombieState.Idle;
        }
    }

    // if the zombie has chased the player too far from its spawn point, it will return to it, then switch back to idle
    // leashing mechanic - doesn't apply very often though (?) so might remove, might not matter
    private void HandleReturning()
    {
        MoveTowardsTarget(initialSpawnPosition);
        Vector3 flatSpawnPos = new Vector3(initialSpawnPosition.x, transform.position.y, initialSpawnPosition.z);

        if (Vector3.Distance(transform.position, flatSpawnPos) <= 0.5f)
        {
            currentState = ZombieState.Idle;
            hasSeenPlayer = false;
        }
    }

    // checks if the player is within the zombie's FOV / in "sight"
    private bool CanSeePlayer()
    {
        if (player == null) return false;

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer > viewDistance) return false;

        float angle = Vector3.Angle(transform.forward, directionToPlayer);
        if (angle > viewAngle / 2f) return false;

        // Line of sight raycast 
        Ray ray = new Ray(transform.position + Vector3.up * 1.5f, directionToPlayer);
        if (Physics.Raycast(ray, out RaycastHit hit, viewDistance, ~0))
        {
            if (hit.transform == player)
            {
                return true;
            }
        }

        return false;
    }

    // idle/chasing
    private void HandlePursuitAndIdleLogic()
    {
        Vector3 flatPlayerPos = new Vector3(player.position.x, transform.position.y, player.position.z);
        float distanceToPlayer = Vector3.Distance(transform.position, flatPlayerPos);
        float distanceToSpawn = Vector3.Distance(transform.position, new Vector3(initialSpawnPosition.x, transform.position.y, initialSpawnPosition.z));

        if (distanceToSpawn > maxLeashDistance)
        {
            currentState = ZombieState.Returning;
            return;
        }

        if (!hasSeenPlayer && CanSeePlayer())
        {
            hasSeenPlayer = true;
        }

        if (hasSeenPlayer && distanceToPlayer <= aggroRadius)
        {
            if (distanceToPlayer <= attackRadius && Time.time >= nextAttackTime)
            {
                StartCoroutine(AttackRoutine());
            }
            else if (distanceToPlayer > attackRadius)
            {
                currentState = ZombieState.Pursuing;
            }
            else
            {
                currentState = ZombieState.Idle;
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
            currentState = ZombieState.Idle;
        }
    }

    // attackType 0 = normal attack, 1 = heavy attack (which can be blocked)
    // reasoning is player's 0 attack comes from the top right (animation), and the zombie only has one arm (left) that can block only the top left (theoretically)
    // so the zombie wouldn't be able to block the player's heavy attack coming from the bottom left (the zombie's bottom right) because the zombie doesn't have that arm
    // it's a bit random but idk I didn't think it was very logical lol
    public void TakeDamage(float amount, int attackType = 0)
    {
        if (currentState == ZombieState.Dead) return;

        lastDamageTime = Time.time;
        hasSeenPlayer = true;

        if (currentState == ZombieState.Blocking && attackType == 1) return;

        currentHealth -= amount;
        UpdateHealthUI();

        if (currentHealth <= 0)
        {
            Die();
            return;
        }
        else
        {
            EvaluateBayesianState();
        }
    }

    // deciding if zombie will block, enrage, flee, or do nothing different
    private void EvaluateBayesianState()
    {
        if (currentState == ZombieState.Attacking || currentState == ZombieState.Blocking || currentState == ZombieState.Enraged) return;

        float healthPercentage = currentHealth / maxHealth;
        ZombieState nextState = bayesianBrain.DecideNextState(healthPercentage);

        if (nextState == ZombieState.Blocking) StartCoroutine(BlockRoutine());
        else if (nextState == ZombieState.Enraged) StartCoroutine(EnrageRoutine());
        else if (nextState == ZombieState.Fleeing) currentState = ZombieState.Fleeing;
    }

    // attack + doing dmg
    private IEnumerator AttackRoutine()
    {
        nextAttackTime = Time.time + attackCooldown;

        currentState = ZombieState.Attacking;
        animator.SetTrigger("attack");

        yield return new WaitForSeconds(attackDamageDelay);

        if (currentState == ZombieState.Dead) yield break;

        if (player != null)
        {
            Vector3 flatPlayerPos = new Vector3(player.position.x, transform.position.y, player.position.z);

            if (currentState == ZombieState.Dead) yield break;

            if (Vector3.Distance(transform.position, flatPlayerPos) <= attackRadius + 0.5f)
            {
                if (playerHealthScript != null)
                    playerHealthScript.TakeDamage(damageToPlayer);

                if (hitSound != null)
                    sfxAudioSource.PlayOneShot(hitSound);
            }
        }

        yield return new WaitForSeconds(1.0f);

        if (currentState != ZombieState.Dead) currentState = ZombieState.Pursuing;
    }

    // blocking
    private IEnumerator BlockRoutine()
    {
        currentState = ZombieState.Blocking;
        animator.SetTrigger("block");

        yield return new WaitForSeconds(blockDuration);

        if (currentState != ZombieState.Dead)
        {
            currentState = ZombieState.Pursuing;
            EvaluateBayesianState();
        }
    }

    // RAWRRRR ENRAGEEEE
    private IEnumerator EnrageRoutine()
    {
        currentState = ZombieState.Enraged;
        animator.SetTrigger("roar");

        if (roarSound != null) sfxAudioSource.PlayOneShot(roarSound);

        yield return new WaitForSeconds(1.5f);

        damageToPlayer *= 1.5f;
        attackCooldown /= 1.5f;
        animator.speed = 1.5f;

        if (currentState != ZombieState.Dead) currentState = ZombieState.Pursuing;
    }

    // death
    // Some issue with the zombie's death animation + uneven flooring makes it so like, the zombie hands are going through the floor and the zombie legs are floating, but it's fijne
    void Die()
    {
        if (currentState == ZombieState.Dead) return;

        currentState = ZombieState.Dead;

        damageToPlayer = 0f;
        attackRadius = 0f;

        animator.SetTrigger("die");

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        GetComponent<Collider>().enabled = false;

        if (healthText != null) healthText.text = "";
        if (walkAudioSource != null) walkAudioSource.Stop();
        if (sfxAudioSource != null) sfxAudioSource.Stop();

        if (healthPotionPrefab != null && Random.Range(0f, 100f) < healthPotChance)
        {
            Instantiate(healthPotionPrefab, transform.position + Vector3.up * .2f, Quaternion.identity);
        }

        StopAllCoroutines();
        StartCoroutine(HideUIAfterDeath());
    }

    // regens if not hit for a while
    private void HandleHealthRegen()
    {
        if (Time.time - lastDamageTime >= healDelay && currentHealth < maxHealth)
        {
            currentHealth += (maxHealth / 5f) * Time.deltaTime;
            if (currentHealth > maxHealth) currentHealth = maxHealth;
            UpdateHealthUI();
        }
    }

    // idle / growing audio + mutes if on a different' floor
    // was annoying when you could hear all spawned zombies regardless of if they were on a different floor
    private void HandleAudioAndAggro()
    {
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
    }

    private void UpdateAnimations()
    {
        bool isWalking = (currentState == ZombieState.Pursuing || currentState == ZombieState.Fleeing || currentState == ZombieState.Returning);
        animator.SetBool("isWalking", isWalking);

        if (isWalking && !walkAudioSource.isPlaying) walkAudioSource.Play();
        else if (!isWalking && walkAudioSource.isPlaying) walkAudioSource.Pause();
    }

    private void UpdateHealthUI()
    {
        if (healthText != null) healthText.text = (int)currentHealth + "/" + (int)maxHealth;
    }

    private void UpdateUIElements()
    {
        if (healthBarFill != null)
        {
            float targetFill = currentHealth / maxHealth;
            healthBarFill.fillAmount = Mathf.Lerp(healthBarFill.fillAmount, targetFill, Time.deltaTime * healthDrainSpeed);
        }
    }

    private IEnumerator HideUIAfterDeath()
    {
        yield return new WaitForSeconds(deathAnimationDuration);
        if (uiCanvasObject != null) uiCanvasObject.SetActive(false);
    }
}

// Bayesian deciding whether the zombie block, enrage, flee, or do nothing different when taking damage
public class BayesianBrain
{
    // probabilities are based on the zombie's current health percentage, the lower the health, the more likely it is to flee or enrage the less likely it is to block
    public PrisonZombieAI.ZombieState DecideNextState(float healthPercentage)
    {
        float blockProb = CalculateBlockProbability(healthPercentage);
        float enrageProb = CalculateEnrageProbability(healthPercentage);
        float fleeProb = CalculateFleeProbability(healthPercentage);
        float pursueProb = 100f - (blockProb + enrageProb + fleeProb);

        float roll = Random.Range(0f, 100f);

        // checked in order of block, then enrage, then flee, then pursue (aka doing nothing diff)
        if (roll <= blockProb) return PrisonZombieAI.ZombieState.Blocking;
        roll -= blockProb;

        if (roll <= enrageProb) return PrisonZombieAI.ZombieState.Enraged;
        roll -= enrageProb;

        if (roll <= fleeProb) return PrisonZombieAI.ZombieState.Fleeing;

        return PrisonZombieAI.ZombieState.Pursuing;
    }

    private float CalculateBlockProbability(float hpPercentage)
    {
        if (hpPercentage > 0.5f) return Mathf.Lerp(10f, 15f, 1f - hpPercentage);
        return Mathf.Lerp(15f, 5f, hpPercentage * 2f);
    }

    private float CalculateEnrageProbability(float hpPercentage)
    {
        if (hpPercentage > 0.5f) return 10f;
        return Mathf.Lerp(60f, 20f, hpPercentage * 2f);
    }

    private float CalculateFleeProbability(float hpPercentage)
    {
        if (hpPercentage > 0.5f) return 0f;
        if (hpPercentage > 0.25f) return Mathf.Lerp(15f, 0f, (hpPercentage - 0.25f) * 4f);
        return Mathf.Lerp(10f, 15f, hpPercentage * 4f);
    }
}