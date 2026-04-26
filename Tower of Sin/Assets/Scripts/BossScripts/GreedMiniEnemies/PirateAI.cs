using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using TMPro;

public class PirateAI : MonoBehaviour
{
    public float baseHP = 50f;
    public float baseATK = 10f;

    [HideInInspector] public float currentHP;
    [HideInInspector] public float currentATK;
    [HideInInspector] public float maxHP;

    [Header("Movement")]
    public float walkSpeed = 2f;
    public float runSpeed = 4.5f;
    public float attackRange = 2.5f;
    public float runDistance = 8f;
    public float rotationSpeed = 8f;
    public float stopDistanceBuffer = 0.15f;

    [Header("Combat")]
    public float attackCooldown = 1.75f;
    public bool canDamagePlayer = true;

    [Header("Spawn Intro / Crawl Up")]
    public float spawnLowerAmount = 6f;
    public float crawlUpDuration = 1.5f;
    public bool untargetableDuringSpawn = true;

    [Header("References")]
    public Transform player;
    public NavMeshAgent agent;
    public Animator animator;

    [Header("UI")]
    public GameObject hpCanvas;
    public Image healthFillImage;
    public TMP_Text healthText;

    private float lastAttackTime = -999f;
    private bool isDead = false;
    private bool isAttacking = false;
    private bool isReady = false;
    private bool canBeDamaged = false;

    private Collider[] allColliders;
    private Vector3 finalSpawnPosition;

    private static readonly int ParamAtk1 = Animator.StringToHash("atk1");
    private static readonly int ParamAtk2 = Animator.StringToHash("atk2");
    private static readonly int ParamDie = Animator.StringToHash("die");
    private static readonly int ParamIsRunning = Animator.StringToHash("isRunning");

    void Start()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        allColliders = GetComponentsInChildren<Collider>(true);

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }

        ApplyFloorScaling();
        currentHP = maxHP;
        UpdateHPUI();

        if (hpCanvas != null)
            hpCanvas.SetActive(true);

        if (agent != null)
        {
            agent.stoppingDistance = Mathf.Max(0.1f, attackRange - stopDistanceBuffer);
            agent.speed = walkSpeed;
            agent.updateRotation = false;
            agent.isStopped = true;
        }

        SetRunning(false);
        StartCoroutine(SpawnIntroRoutine());
    }

    private IEnumerator SpawnIntroRoutine()
    {
        isReady = false;
        canBeDamaged = !untargetableDuringSpawn;
        canDamagePlayer = false;
        SetRunning(false);

        finalSpawnPosition = transform.position;
        Vector3 lowerPosition = finalSpawnPosition + Vector3.down * spawnLowerAmount;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        transform.position = lowerPosition;

        if (untargetableDuringSpawn)
            SetCollidersEnabled(false);

        float timer = 0f;

        while (timer < crawlUpDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / crawlUpDuration);
            transform.position = Vector3.Lerp(lowerPosition, finalSpawnPosition, t);
            yield return null;
        }

        Vector3 navmeshFinalPosition = finalSpawnPosition;

        if (NavMesh.SamplePosition(finalSpawnPosition, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            navmeshFinalPosition = hit.position;

        transform.position = navmeshFinalPosition;

        if (untargetableDuringSpawn)
            SetCollidersEnabled(true);

        if (agent != null)
        {
            agent.enabled = true;
            agent.Warp(navmeshFinalPosition);
            agent.isStopped = false;
            agent.stoppingDistance = Mathf.Max(0.1f, attackRange - stopDistanceBuffer);
            agent.speed = walkSpeed;
        }

        canBeDamaged = true;
        canDamagePlayer = true;
        isReady = true;
        SetRunning(false);
    }

    private void SetCollidersEnabled(bool enabled)
    {
        if (allColliders == null) return;

        foreach (Collider col in allColliders)
        {
            if (col != null)
                col.enabled = enabled;
        }
    }

    void Update()
    {
        if (!isReady || isDead || player == null || agent == null || !agent.enabled)
            return;

        float distance = Vector3.Distance(transform.position, player.position);

        FacePlayer();

        if (isAttacking)
        {
            agent.isStopped = true;
            SetRunning(false);
            return;
        }

        if (distance <= attackRange)
        {
            agent.isStopped = true;
            agent.ResetPath();
            SetRunning(false);

            if (Time.time >= lastAttackTime + attackCooldown)
                StartAttack();
        }
        else
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);

            bool shouldRun = distance > runDistance;
            agent.speed = shouldRun ? runSpeed : walkSpeed;

            SetRunning(true);
        }
    }

    void ApplyFloorScaling()
    {
        int floor = FloorTextController.floorNumber;
        int bonusSteps = Mathf.Max(0, (floor / 5) - 1);

        float multiplier = 1f + (0.10f * bonusSteps);

        maxHP = baseHP * multiplier;
        currentATK = baseATK * multiplier;
    }

    void FacePlayer()
    {
        if (player == null) return;

        Vector3 lookPos = player.position - transform.position;
        lookPos.y = 0f;

        if (lookPos.sqrMagnitude < 0.001f) return;

        Quaternion targetRot = Quaternion.LookRotation(lookPos);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
    }

    void StartAttack()
    {
        if (animator == null) return;

        isAttacking = true;
        lastAttackTime = Time.time;

        SetRunning(false);

        animator.ResetTrigger(ParamAtk1);
        animator.ResetTrigger(ParamAtk2);

        if (Random.value < 0.5f)
            animator.SetTrigger(ParamAtk1);
        else
            animator.SetTrigger(ParamAtk2);

        StartCoroutine(AttackDamageWindow());
    }

    private IEnumerator AttackDamageWindow()
    {
        yield return new WaitForSeconds(0.75f);

        float timer = 0f;
        bool alreadyHit = false;

        while (timer < 1f)
        {
            if (!alreadyHit)
            {
                float distance = Vector3.Distance(transform.position, player.position);

                if (distance <= attackRange + 0.75f)
                {
                    DealDamageToPlayer();
                    alreadyHit = true;
                }
            }

            timer += Time.deltaTime;
            yield return null;
        }

        isAttacking = false;
        SetRunning(false);
    }

    public void EndAttack()
    {
        if (isDead) return;

        isAttacking = false;
        SetRunning(false);
    }

    public void DealDamageToPlayer()
    {
        if (isDead || !canDamagePlayer || player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance > attackRange + 0.75f) return;

        PlayerHealth ph = player.GetComponent<PlayerHealth>();

        if (ph != null)
            ph.TakeDamage(Mathf.RoundToInt(currentATK));
    }

    public void TakeDamage(float damage)
    {
        TakeDamage(damage, 0);
    }

    public void TakeDamage(float damage, int slashChoice)
    {
        if (isDead || !canBeDamaged) return;

        currentHP = Mathf.Max(currentHP - damage, 0f);
        UpdateHPUI();

        if (currentHP <= 0f)
            Die();
    }

    void UpdateHPUI()
    {
        if (healthFillImage != null)
            healthFillImage.fillAmount = maxHP <= 0f ? 0f : currentHP / maxHP;

        if (healthText != null)
            healthText.text = $"{Mathf.CeilToInt(currentHP)}/{Mathf.CeilToInt(maxHP)}";
    }

    void Die()
    {
        if (isDead) return;

        isDead = true;
        currentATK = 0f;
        canDamagePlayer = false;
        isAttacking = false;
        canBeDamaged = false;

        StopAllCoroutines();

        if (agent != null)
        {
            if (agent.enabled)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }

            agent.enabled = false;
        }

        SetCollidersEnabled(false);

        if (hpCanvas != null)
            hpCanvas.SetActive(false);

        if (animator != null)
        {
            SetRunning(false);
            animator.ResetTrigger(ParamAtk1);
            animator.ResetTrigger(ParamAtk2);
            animator.SetTrigger(ParamDie);
        }

        Destroy(gameObject, 10f);
    }

    private void SetRunning(bool value)
    {
        if (animator != null)
            animator.SetBool(ParamIsRunning, value);
    }
}