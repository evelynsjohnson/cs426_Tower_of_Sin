using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Audio;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(Rigidbody))]
public class PrisonZombieAI : MonoBehaviour
{
    public enum ZombieState { Idle, Returning, Pursuing, Fleeing, Attacking, Blocking, Enraged, Dead }
    public ZombieState currentState = ZombieState.Idle;

    [Header("Stats")]
    public float maxHealth = 100f;
    private float currentHealth;
    public float damageToPlayer = 10f;

    [Header("UI")]
    public GameObject uiCanvasObject;
    public TMP_Text healthText;
    public Image healthBarFill;
    public float healthDrainSpeed = 5f;
    public float deathAnimationDuration = 2f;

    [Header("Combat / Movement")]
    public float aggroRadius = 10f;
    public float attackRadius = 2f;
    public float maxLeashDistance = 20f;
    public float walkSpeed = 3.5f;
    public float fleeSafeDistance = 5f;
    public float attackCooldown = 2f;
    public float attackDamageDelay = 0.5f;
    public float attackRecoveryTime = 1.0f;
    public float blockDuration = 1f;
    public float healDelay = 5f;

    [Header("Vision")]
    public float viewAngle = 90f;
    public float viewDistance = 12f;
    public LayerMask visionObstructionMask;

    [Header("A* Pathfinding")]
    public LayerMask pathObstacleMask;
    public float nodeRadius = 0.4f;
    public float pathNodeHeightOffset = 0.5f;
    public float pathPadding = 2f;
    public float pathRecalculationInterval = 0.35f;
    public int maxGridSizePerAxis = 40;
    public float waypointReachDistance = 0.5f;

    [Header("Audio")]
    public AudioClip hitSound;
    public AudioClip missSound;
    public AudioClip idleSound;
    public AudioClip walkSound;
    public AudioClip roarSound;
    public AudioMixerGroup sfxMixerGroup;

    [Header("References")]
    public Animator animator;
    public GameObject healthPotionPrefab;
    public float healthPotChance = 50f;

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

    private List<Vector3> currentPath = new List<Vector3>();
    private int currentPathIndex = 0;
    private float nextPathRecalcTime = 0f;

    // Action state safety
    private Coroutine attackRoutine;
    private Coroutine blockRoutine;
    private Coroutine enrageRoutine;
    private bool isDying = false;

    // Base values so enrage does not permanently stack weirdly
    private float baseDamageToPlayer;
    private float baseAttackCooldown;

    void Start()
    {
        maxHealth = 100f + ((FloorTextController.floorNumber - 1) * 5f);
        currentHealth = maxHealth;
        initialSpawnPosition = transform.position;

        baseDamageToPlayer = damageToPlayer;
        baseAttackCooldown = attackCooldown;


        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
        rb.isKinematic = false;

        sfxAudioSource = GetComponent<AudioSource>();
        sfxAudioSource.outputAudioMixerGroup = sfxMixerGroup;
        sfxAudioSource.spatialBlend = 1f;
        sfxAudioSource.rolloffMode = AudioRolloffMode.Linear;
        sfxAudioSource.minDistance = 2f;
        sfxAudioSource.maxDistance = 8f;

        walkAudioSource = gameObject.AddComponent<AudioSource>();
        walkAudioSource.outputAudioMixerGroup = sfxMixerGroup;
        walkAudioSource.spatialBlend = 1f;
        walkAudioSource.rolloffMode = AudioRolloffMode.Linear;
        walkAudioSource.minDistance = 2f;
        walkAudioSource.maxDistance = 15f;
        walkAudioSource.clip = walkSound;
        walkAudioSource.loop = true;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerHealthScript = playerObj.GetComponent<PlayerHealth>();
        }

        if (Camera.main != null)
            mainCamera = Camera.main.transform;

        if (healthBarFill != null)
            healthBarFill.fillAmount = 1f;

        idleAudioTimer = Random.Range(2f, 5f);
        UpdateHealthUI();

        bayesianBrain = new BayesianBrain();
    }

    void FixedUpdate()
    {
        if (currentState == ZombieState.Dead || isDying || player == null)
            return;

        switch (currentState)
        {
            case ZombieState.Fleeing:
                HandleFleeing();
                break;

            case ZombieState.Returning:
                HandleReturning();
                break;

            case ZombieState.Pursuing:
                FollowPathTo(player.position);
                break;

            case ZombieState.Idle:
            case ZombieState.Attacking:
            case ZombieState.Blocking:
            case ZombieState.Enraged:
                rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
                break;
        }
    }

    void Update()
    {
        UpdateUIElements();

        if (currentState == ZombieState.Dead || isDying || player == null)
            return;

        HandleHealthRegen();
        HandleAudioAndAggro();

        if (currentState == ZombieState.Pursuing || currentState == ZombieState.Idle)
        {
            HandlePursuitAndIdleLogic();
        }

        UpdateAnimations();
    }

    void LateUpdate()
    {
        if (uiCanvasObject != null && mainCamera != null)
        {
            uiCanvasObject.transform.LookAt(uiCanvasObject.transform.position + mainCamera.forward);
        }
    }

    private void FollowPathTo(Vector3 targetPos)
    {
        targetPos.y = transform.position.y;

        if (Time.time >= nextPathRecalcTime)
        {
            RequestPath(targetPos);
            nextPathRecalcTime = Time.time + pathRecalculationInterval;
        }

        MoveAlongCurrentPath();
    }

    private void RequestPath(Vector3 targetPos)
    {
        List<Vector3> newPath = FindPath(transform.position, targetPos);

        if (newPath != null && newPath.Count > 0)
        {
            if (currentPath == null || currentPath.Count == 0 || currentPathIndex >= currentPath.Count)
            {
                currentPath = newPath;
                currentPathIndex = 0;
            }
            else
            {
                float oldEndDist = Vector3.Distance(currentPath[currentPath.Count - 1], targetPos);
                float newEndDist = Vector3.Distance(newPath[newPath.Count - 1], targetPos);

                if (newEndDist <= oldEndDist + 0.5f)
                {
                    currentPath = newPath;
                    currentPathIndex = 0;
                }
            }
        }
    }

    private bool HasClearPath(Vector3 from, Vector3 to)
    {
        Vector3 origin = from + Vector3.up * pathNodeHeightOffset;
        Vector3 target = to + Vector3.up * pathNodeHeightOffset;
        Vector3 dir = target - origin;
        float dist = dir.magnitude;

        if (dist <= 0.01f) return true;

        return !Physics.SphereCast(origin, nodeRadius * 0.8f, dir.normalized, out _, dist, pathObstacleMask);
    }

    private void MoveAlongCurrentPath()
    {
        if (currentPath == null || currentPath.Count == 0)
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            return;
        }

        for (int i = currentPath.Count - 1; i > currentPathIndex; i--)
        {
            Vector3 testPoint = currentPath[i];
            testPoint.y = transform.position.y;

            if (HasClearPath(transform.position, testPoint))
            {
                currentPathIndex = i;
                break;
            }
        }

        while (currentPathIndex < currentPath.Count)
        {
            Vector3 waypoint = currentPath[currentPathIndex];
            waypoint.y = transform.position.y;

            float dist = Vector3.Distance(
                new Vector3(transform.position.x, 0f, transform.position.z),
                new Vector3(waypoint.x, 0f, waypoint.z)
            );

            if (dist <= waypointReachDistance)
            {
                currentPathIndex++;
            }
            else
            {
                MoveTowardsTarget(waypoint);
                return;
            }
        }

        rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
    }

    private void MoveTowardsTarget(Vector3 targetPos)
    {
        Vector3 direction = targetPos - transform.position;
        direction.y = 0f;

        if (direction.magnitude > 0.05f)
        {
            Vector3 moveDirection = direction.normalized;
            rb.MovePosition(rb.position + moveDirection * walkSpeed * Time.fixedDeltaTime);

            if (moveDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(moveDirection),
                    Time.fixedDeltaTime * 5f
                );
            }
        }
    }

    private void HandleFleeing()
    {
        Vector3 flatPlayerPos = new Vector3(player.position.x, transform.position.y, player.position.z);

        if (Vector3.Distance(transform.position, flatPlayerPos) < fleeSafeDistance)
        {
            Vector3 dirAwayFromPlayer = (transform.position - flatPlayerPos).normalized;
            currentMoveTarget = transform.position + (dirAwayFromPlayer * 5f);
            currentMoveTarget.y = transform.position.y;

            FollowPathTo(currentMoveTarget);
        }
        else
        {
            ClearPath();
            currentState = ZombieState.Idle;
        }
    }

    private void HandleReturning()
    {
        FollowPathTo(initialSpawnPosition);

        Vector3 flatSpawnPos = new Vector3(initialSpawnPosition.x, transform.position.y, initialSpawnPosition.z);
        if (Vector3.Distance(transform.position, flatSpawnPos) <= 0.5f)
        {
            ClearPath();
            currentState = ZombieState.Idle;
            hasSeenPlayer = false;
        }
    }

    private bool CanSeePlayer()
    {
        if (player == null) return false;

        Vector3 eyePos = transform.position + Vector3.up * 1.5f;
        Vector3 playerCenter = player.position + Vector3.up * 1f;
        Vector3 directionToPlayer = (playerCenter - eyePos).normalized;
        float distanceToPlayer = Vector3.Distance(eyePos, playerCenter);

        if (distanceToPlayer > viewDistance) return false;

        float angle = Vector3.Angle(transform.forward, directionToPlayer);
        if (angle > viewAngle / 2f) return false;

        if (Physics.Raycast(eyePos, directionToPlayer, out RaycastHit hit, viewDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform == player || hit.transform.IsChildOf(player))
                return true;
        }

        return false;
    }

    private void HandlePursuitAndIdleLogic()
    {
        Vector3 flatPlayerPos = new Vector3(player.position.x, transform.position.y, player.position.z);
        float distanceToPlayer = Vector3.Distance(transform.position, flatPlayerPos);
        float distanceToSpawn = Vector3.Distance(
            transform.position,
            new Vector3(initialSpawnPosition.x, transform.position.y, initialSpawnPosition.z)
        );

        if (distanceToSpawn > maxLeashDistance)
        {
            CancelCurrentAction();
            ClearPath();
            currentState = ZombieState.Returning;
            return;
        }

        if (!hasSeenPlayer && CanSeePlayer())
        {
            hasSeenPlayer = true;
        }

        if (hasSeenPlayer && distanceToPlayer <= aggroRadius)
        {
            if (distanceToPlayer <= attackRadius &&
                Time.time >= nextAttackTime &&
                currentState != ZombieState.Attacking &&
                attackRoutine == null)
            {
                ClearPath();
                CancelCurrentAction();
                attackRoutine = StartCoroutine(AttackRoutine());
            }
            else if (distanceToPlayer > attackRadius)
            {
                if (!IsBusyState())
                    currentState = ZombieState.Pursuing;
            }
            else
            {
                ClearPath();

                if (!IsBusyState())
                    currentState = ZombieState.Idle;

                Vector3 lookPos = player.position - transform.position;
                lookPos.y = 0f;
                if (lookPos != Vector3.zero)
                {
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        Quaternion.LookRotation(lookPos),
                        Time.deltaTime * 5f
                    );
                }
            }
        }
        else
        {
            if (!IsBusyState())
            {
                ClearPath();
                currentState = ZombieState.Idle;
            }
        }
    }

    public void TakeDamage(float amount, int attackType = 0)
    {
        if (currentState == ZombieState.Dead || isDying)
            return;

        lastDamageTime = Time.time;
        hasSeenPlayer = true;

        // Light attack can be blocked
        if (currentState == ZombieState.Blocking && attackType == 1)
            return;

        currentHealth -= amount;
        if (currentHealth < 0f) currentHealth = 0f;
        UpdateHealthUI();

        if (currentHealth <= 0f)
        {
            damageToPlayer = 0f;
            Die();
            return;
        }

        EvaluateBayesianState();
    }

    private void EvaluateBayesianState()
    {
        if (currentState == ZombieState.Dead || isDying)
            return;

        if (currentState == ZombieState.Attacking ||
            currentState == ZombieState.Blocking ||
            currentState == ZombieState.Enraged)
            return;

        float healthPercentage = currentHealth / maxHealth;
        ZombieState nextState = bayesianBrain.DecideNextState(healthPercentage);

        if (nextState == ZombieState.Blocking)
        {
            CancelCurrentAction();
            ClearPath();
            blockRoutine = StartCoroutine(BlockRoutine());
        }
        else if (nextState == ZombieState.Enraged)
        {
            CancelCurrentAction();
            ClearPath();
            enrageRoutine = StartCoroutine(EnrageRoutine());
        }
        else if (nextState == ZombieState.Fleeing)
        {
            CancelCurrentAction();
            ClearPath();
            currentState = ZombieState.Fleeing;
        }
        else
        {
            if (!IsBusyState())
                currentState = ZombieState.Pursuing;
        }
    }

    private IEnumerator AttackRoutine()
    {
        if (currentState == ZombieState.Dead || isDying)
        {
            attackRoutine = null;
            yield break;
        }

        nextAttackTime = Time.time + attackCooldown;
        currentState = ZombieState.Attacking;

        ResetCombatTriggers();
        animator.SetBool("isWalking", false);
        animator.SetTrigger("attack");

        yield return new WaitForSeconds(attackDamageDelay);

        if (currentState != ZombieState.Attacking || isDying || currentState == ZombieState.Dead)
        {
            attackRoutine = null;
            yield break;
        }

        if (player != null)
        {
            Vector3 flatPlayerPos = new Vector3(player.position.x, transform.position.y, player.position.z);

            if (Vector3.Distance(transform.position, flatPlayerPos) <= attackRadius + 0.5f)
            {
                if (playerHealthScript != null)
                    playerHealthScript.TakeDamage(damageToPlayer);

                if (hitSound != null)
                    sfxAudioSource.PlayOneShot(hitSound);
            }
            else
            {
                if (missSound != null)
                    sfxAudioSource.PlayOneShot(missSound);
            }
        }

        yield return new WaitForSeconds(attackRecoveryTime);

        if (!isDying && currentState == ZombieState.Attacking)
            currentState = ZombieState.Pursuing;

        attackRoutine = null;
    }

    private IEnumerator BlockRoutine()
    {
        if (currentState == ZombieState.Dead || isDying)
        {
            blockRoutine = null;
            yield break;
        }

        currentState = ZombieState.Blocking;

        ResetCombatTriggers();
        animator.SetBool("isWalking", false);
        animator.SetTrigger("block");

        yield return new WaitForSeconds(blockDuration);

        if (!isDying && currentState == ZombieState.Blocking)
        {
            currentState = ZombieState.Pursuing;
        }

        blockRoutine = null;
    }

    private IEnumerator EnrageRoutine()
    {
        if (currentState == ZombieState.Dead || isDying)
        {
            enrageRoutine = null;
            yield break;
        }

        currentState = ZombieState.Enraged;

        ResetCombatTriggers();
        animator.SetBool("isWalking", false);
        animator.SetTrigger("roar");

        if (roarSound != null)
            sfxAudioSource.PlayOneShot(roarSound);

        yield return new WaitForSeconds(1.5f);

        if (isDying || currentState != ZombieState.Enraged || currentState == ZombieState.Dead)
        {
            enrageRoutine = null;
            yield break;
        }

        damageToPlayer = baseDamageToPlayer * 1.5f;
        attackCooldown = baseAttackCooldown / 1.5f;
        animator.speed = 1.5f;

        currentState = ZombieState.Pursuing;
        enrageRoutine = null;
    }

    private void Die()
    {
        if (currentState == ZombieState.Dead || isDying)
            return;

        isDying = true;
        currentState = ZombieState.Dead;

        CancelCurrentAction();
        ClearPath();

        damageToPlayer = 0f;
        attackRadius = 0f;
        animator.speed = 1f;

        ResetCombatTriggers();
        animator.SetBool("isWalking", false);
        animator.SetTrigger("die");

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        if (healthText != null)
            healthText.text = "";

        if (walkAudioSource != null)
            walkAudioSource.Stop();

        if (sfxAudioSource != null)
            sfxAudioSource.Stop();

        if (healthPotionPrefab != null && Random.Range(0f, 100f) < healthPotChance)
        {
            Instantiate(healthPotionPrefab, transform.position + Vector3.up * 0.2f, Quaternion.identity);
        }

        StartCoroutine(HideUIAfterDeath());
    }

    private void HandleHealthRegen()
    {
        if (Time.time - lastDamageTime >= healDelay && currentHealth < maxHealth)
        {
            currentHealth += (maxHealth / 5f) * Time.deltaTime;
            if (currentHealth > maxHealth) currentHealth = maxHealth;
            UpdateHealthUI();
        }
    }

    private void HandleAudioAndAggro()
    {
        float verticalDistance = Mathf.Abs(player.position.y - transform.position.y);
        bool onSameFloor = verticalDistance < 2.5f;

        sfxAudioSource.mute = !onSameFloor;
        walkAudioSource.mute = !onSameFloor;

        idleAudioTimer -= Time.deltaTime;
        if (idleAudioTimer <= 0f)
        {
            if (idleSound != null && onSameFloor && currentState != ZombieState.Dead && !isDying)
                sfxAudioSource.PlayOneShot(idleSound);

            idleAudioTimer = Random.Range(2f, 5f);
        }
    }

    private void UpdateAnimations()
    {
        bool isWalking = (currentState == ZombieState.Pursuing ||
                          currentState == ZombieState.Fleeing ||
                          currentState == ZombieState.Returning);

        animator.SetBool("isWalking", isWalking);

        if (isWalking && !walkAudioSource.isPlaying)
            walkAudioSource.Play();
        else if (!isWalking && walkAudioSource.isPlaying)
            walkAudioSource.Pause();
    }

    private void UpdateHealthUI()
    {
        if (healthText != null)
            healthText.text = (int)currentHealth + "/" + (int)maxHealth;
    }

    private void UpdateUIElements()
    {
        if (healthBarFill != null)
        {
            float targetFill = currentHealth / maxHealth;
            healthBarFill.fillAmount = Mathf.Lerp(
                healthBarFill.fillAmount,
                targetFill,
                Time.deltaTime * healthDrainSpeed
            );
        }
    }

    private IEnumerator HideUIAfterDeath()
    {
        yield return new WaitForSeconds(deathAnimationDuration);

        if (uiCanvasObject != null)
            uiCanvasObject.SetActive(false);
    }

    private void ClearPath()
    {
        currentPath.Clear();
        currentPathIndex = 0;
    }

    private void ResetCombatTriggers()
    {
        animator.ResetTrigger("attack");
        animator.ResetTrigger("block");
        animator.ResetTrigger("roar");
        animator.ResetTrigger("die");
    }

    private void CancelCurrentAction()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        if (blockRoutine != null)
        {
            StopCoroutine(blockRoutine);
            blockRoutine = null;
        }

        if (enrageRoutine != null)
        {
            StopCoroutine(enrageRoutine);
            enrageRoutine = null;
        }
    }

    private bool IsBusyState()
    {
        return currentState == ZombieState.Attacking ||
               currentState == ZombieState.Blocking ||
               currentState == ZombieState.Enraged ||
               currentState == ZombieState.Dead ||
               isDying;
    }

    private List<Vector3> FindPath(Vector3 startWorld, Vector3 targetWorld)
    {
        startWorld.y = transform.position.y;
        targetWorld.y = transform.position.y;

        float nodeDiameter = nodeRadius * 2f;

        float dx = Mathf.Abs(targetWorld.x - startWorld.x);
        float dz = Mathf.Abs(targetWorld.z - startWorld.z);

        float worldSizeX = Mathf.Max(dx + pathPadding * 2f, nodeDiameter * 6f);
        float worldSizeZ = Mathf.Max(dz + pathPadding * 2f, nodeDiameter * 6f);

        int gridSizeX = Mathf.Clamp(Mathf.RoundToInt(worldSizeX / nodeDiameter), 6, maxGridSizePerAxis);
        int gridSizeY = Mathf.Clamp(Mathf.RoundToInt(worldSizeZ / nodeDiameter), 6, maxGridSizePerAxis);

        Vector3 gridCenter = new Vector3(
            (startWorld.x + targetWorld.x) * 0.5f,
            transform.position.y,
            (startWorld.z + targetWorld.z) * 0.5f
        );

        PathNode[,] grid = BuildGrid(gridCenter, gridSizeX, gridSizeY, nodeDiameter);

        PathNode startNode = NodeFromWorldPoint(startWorld, grid, gridCenter, gridSizeX, gridSizeY, nodeDiameter);
        PathNode targetNode = NodeFromWorldPoint(targetWorld, grid, gridCenter, gridSizeX, gridSizeY, nodeDiameter);

        if (startNode == null || targetNode == null)
            return null;

        startNode.walkable = true;
        targetNode.walkable = true;
        startNode.gCost = 0;
        startNode.hCost = GetDistance(startNode, targetNode);
        startNode.parent = null;

        List<PathNode> openSet = new List<PathNode>();
        HashSet<PathNode> closedSet = new HashSet<PathNode>();
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            PathNode currentNode = openSet[0];

            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].FCost < currentNode.FCost ||
                    (openSet[i].FCost == currentNode.FCost && openSet[i].hCost < currentNode.hCost))
                {
                    currentNode = openSet[i];
                }
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            if (currentNode == targetNode)
            {
                return RetracePath(startNode, targetNode);
            }

            foreach (PathNode neighbour in GetNeighbours(currentNode, grid, gridSizeX, gridSizeY))
            {
                if (!neighbour.walkable || closedSet.Contains(neighbour))
                    continue;

                int newMovementCostToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbour);
                if (newMovementCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour))
                {
                    neighbour.gCost = newMovementCostToNeighbour;
                    neighbour.hCost = GetDistance(neighbour, targetNode);
                    neighbour.parent = currentNode;

                    if (!openSet.Contains(neighbour))
                        openSet.Add(neighbour);
                }
            }
        }

        return null;
    }

    private PathNode[,] BuildGrid(Vector3 gridCenter, int gridSizeX, int gridSizeY, float nodeDiameter)
    {
        PathNode[,] grid = new PathNode[gridSizeX, gridSizeY];

        float worldSizeX = gridSizeX * nodeDiameter;
        float worldSizeY = gridSizeY * nodeDiameter;

        Vector3 worldBottomLeft = gridCenter
            - Vector3.right * (worldSizeX / 2f)
            - Vector3.forward * (worldSizeY / 2f);

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                Vector3 worldPoint = worldBottomLeft
                    + Vector3.right * (x * nodeDiameter + nodeRadius)
                    + Vector3.forward * (y * nodeDiameter + nodeRadius);

                worldPoint.y = transform.position.y + pathNodeHeightOffset;

                bool walkable = !Physics.CheckSphere(worldPoint, nodeRadius, pathObstacleMask, QueryTriggerInteraction.Ignore);
                grid[x, y] = new PathNode(walkable, worldPoint, x, y);
            }
        }

        return grid;
    }

    private PathNode NodeFromWorldPoint(
        Vector3 worldPosition,
        PathNode[,] grid,
        Vector3 gridCenter,
        int gridSizeX,
        int gridSizeY,
        float nodeDiameter)
    {
        float worldSizeX = gridSizeX * nodeDiameter;
        float worldSizeY = gridSizeY * nodeDiameter;

        Vector3 worldBottomLeft = gridCenter
            - Vector3.right * (worldSizeX / 2f)
            - Vector3.forward * (worldSizeY / 2f);

        float percentX = Mathf.Clamp01((worldPosition.x - worldBottomLeft.x) / worldSizeX);
        float percentY = Mathf.Clamp01((worldPosition.z - worldBottomLeft.z) / worldSizeY);

        int x = Mathf.Clamp(Mathf.RoundToInt((gridSizeX - 1) * percentX), 0, gridSizeX - 1);
        int y = Mathf.Clamp(Mathf.RoundToInt((gridSizeY - 1) * percentY), 0, gridSizeY - 1);

        return grid[x, y];
    }

    private List<PathNode> GetNeighbours(PathNode node, PathNode[,] grid, int gridSizeX, int gridSizeY)
    {
        List<PathNode> neighbours = new List<PathNode>();

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue;

                int checkX = node.gridX + x;
                int checkY = node.gridY + y;

                if (checkX < 0 || checkX >= gridSizeX || checkY < 0 || checkY >= gridSizeY)
                    continue;

                if (x != 0 && y != 0)
                {
                    int sideX = node.gridX + x;
                    int sideY = node.gridY;
                    int otherX = node.gridX;
                    int otherY = node.gridY + y;

                    if (!grid[sideX, sideY].walkable || !grid[otherX, otherY].walkable)
                        continue;
                }

                neighbours.Add(grid[checkX, checkY]);
            }
        }

        return neighbours;
    }

    private List<Vector3> RetracePath(PathNode startNode, PathNode endNode)
    {
        List<PathNode> path = new List<PathNode>();
        PathNode currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;

            if (currentNode == null)
                return null;
        }

        path.Reverse();

        List<Vector3> waypoints = new List<Vector3>();
        for (int i = 0; i < path.Count; i++)
        {
            Vector3 point = path[i].worldPosition;
            point.y = transform.position.y;
            waypoints.Add(point);
        }

        return SimplifyPath(waypoints);
    }

    private List<Vector3> SimplifyPath(List<Vector3> path)
    {
        if (path == null || path.Count <= 2) return path;

        List<Vector3> simplified = new List<Vector3>();
        Vector2 directionOld = Vector2.zero;

        for (int i = 1; i < path.Count; i++)
        {
            Vector2 directionNew = new Vector2(
                path[i - 1].x - path[i].x,
                path[i - 1].z - path[i].z
            ).normalized;

            if (directionNew != directionOld)
            {
                simplified.Add(path[i - 1]);
            }

            directionOld = directionNew;
        }

        simplified.Add(path[path.Count - 1]);
        return simplified;
    }

    private int GetDistance(PathNode a, PathNode b)
    {
        int dstX = Mathf.Abs(a.gridX - b.gridX);
        int dstY = Mathf.Abs(a.gridY - b.gridY);

        if (dstX > dstY)
            return 14 * dstY + 10 * (dstX - dstY);

        return 14 * dstX + 10 * (dstY - dstX);
    }

    private void OnDrawGizmosSelected()
    {
        if (currentPath == null) return;

        Gizmos.color = Color.green;
        for (int i = 0; i < currentPath.Count; i++)
        {
            Gizmos.DrawSphere(currentPath[i] + Vector3.up * 0.2f, 0.12f);

            if (i < currentPath.Count - 1)
            {
                Gizmos.DrawLine(currentPath[i] + Vector3.up * 0.2f, currentPath[i + 1] + Vector3.up * 0.2f);
            }
        }
    }
}

public class BayesianBrain
{
    public PrisonZombieAI.ZombieState DecideNextState(float healthPercentage)
    {
        float blockProb = CalculateBlockProbability(healthPercentage);
        float enrageProb = CalculateEnrageProbability(healthPercentage);
        float fleeProb = CalculateFleeProbability(healthPercentage);
        float pursueProb = Mathf.Max(0f, 100f - (blockProb + enrageProb + fleeProb));

        float roll = Random.Range(0f, 100f);

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

public class PathNode
{
    public bool walkable;
    public Vector3 worldPosition;
    public int gridX;
    public int gridY;

    public int gCost;
    public int hCost;
    public PathNode parent;

    public int FCost
    {
        get { return gCost + hCost; }
    }

    public PathNode(bool _walkable, Vector3 _worldPosition, int _gridX, int _gridY)
    {
        walkable = _walkable;
        worldPosition = _worldPosition;
        gridX = _gridX;
        gridY = _gridY;
        gCost = int.MaxValue;
        hCost = 0;
        parent = null;
    }
}