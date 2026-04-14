using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using TMPro;

public class AngelBossAI : MonoBehaviour
{
    public enum BossPhase
    {
        Phase1,
        Phase2,
        Dead
    }

    [SerializeField] private float baseMaxHealth = 650f;
    [SerializeField] private float currentHealth;
    [SerializeField] private float maxHealth;
    [SerializeField] private BossPhase currentPhase = BossPhase.Phase1;
    [SerializeField] private int currentFloor = 5;

    [SerializeField] private float circleDelayBeforeHit = 1.5f;
    [SerializeField] private float circleDamageRadius = 3f;
    [SerializeField] private float circleDamage = 25f;
    [SerializeField] private float circleLifetime = 2.5f;

    [SerializeField] private Animator animator;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private GameObject envycirclePrefab;
    [SerializeField] private Transform player;

    [SerializeField] private Image bossHealthBarFill;
    [SerializeField] private TMP_Text bossHealthText;
    [SerializeField] private GameObject bossHealthUIRoot;

    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float phase1TeleportInterval = 15f;
    [SerializeField] private float teleportSearchRadius = 25f;
    [SerializeField] private float navMeshSampleDistance = 8f;
    [SerializeField] private int teleportAttempts = 20;

    [SerializeField] private float delayAfterTeleport = 3f;
    [SerializeField] private string attackTriggerName = "attack1";
    [SerializeField] private float bossAttackAnimationDuration = 3f;

    [SerializeField] private Vector3 circleSpawnOffset = new Vector3(0f, -4f, 0f);
    [SerializeField] private float circleSpawnRadius = 4f;
    [SerializeField] private int phase1CircleCount = 1;
    [SerializeField] private int phase2CircleCount = 3;
    [SerializeField] private int circleSpawnAttempts = 12;

    [SerializeField] private float visualAnimDuration = 2.25f;
    [SerializeField] private float handsStartLocalZ = -5f;
    [SerializeField] private float handsEndLocalZ = 0f;
    [SerializeField] private float handsRiseDuration = 2.25f;
    [SerializeField] private float canvasZRotateSpeed = 45f;
    [SerializeField] private Vector3 canvasRotationPerSecond = new Vector3(0f, 90f, 0f);

    [SerializeField] private float spotlightMoveAmount = 0.4f;
    [SerializeField] private float spotlightPulseDuration = 1f;

    [SerializeField] private float sinkDuration = 2.5f;
    [SerializeField] private float minSinkSpeed = 0.35f;
    [SerializeField] private float maxSinkSpeed = 1.2f;

    [SerializeField] private string canvasChildName = "Canvas";
    [SerializeField] private string handsParentName = "Hands";
    [SerializeField] private bool autoCollectHandsIfNoParentFound = true;

    [SerializeField] private float engageRange = 15f;
    [SerializeField] private float attackCooldown = 15f;
    [SerializeField] private bool drawDebugRange = true;
    [SerializeField] private Vector3 circleRotationEuler = new Vector3(0f, 0f, 0f);
    private float nextAttackTime = 0f;

    private bool isBusy = false;
    private bool phase2Started = false;
    private bool isDead = false;
    private Coroutine aiLoopRoutine;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => isDead;

    private void Reset()
    {
        animator = GetComponentInChildren<Animator>();
        agent = GetComponent<NavMeshAgent>();
    }

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag(playerTag);
            if (p != null)
                player = p.transform;
        }
    }

    private void Start()
    {
        SetupHealthForFloor(currentFloor);
        UpdateBossUI();

        if (bossHealthUIRoot != null)
            bossHealthUIRoot.SetActive(true);

        if (aiLoopRoutine != null)
            StopCoroutine(aiLoopRoutine);

        aiLoopRoutine = StartCoroutine(BossLoop());
    }

    private IEnumerator BossLoop()
    {
        while (!isDead)
        {
            if (player == null)
            {
                GameObject p = GameObject.FindGameObjectWithTag(playerTag);
                if (p != null)
                    player = p.transform;
            }

            if (player != null && !isBusy)
            {
                float dist = Vector3.Distance(transform.position, player.position);

                if (dist <= engageRange && Time.time >= nextAttackTime)
                {
                    nextAttackTime = Time.time + attackCooldown;
                    yield return StartCoroutine(TeleportAndAttack());
                }
            }

            yield return null;
        }
    }

    public void SetFloor(int floor)
    {
        currentFloor = Mathf.Max(1, floor);
        SetupHealthForFloor(currentFloor);
        UpdateBossUI();
    }

    private void SetupHealthForFloor(int floor)
    {
        maxHealth = GetScaledHealthForFloor(floor);
        currentHealth = maxHealth;
        currentPhase = BossPhase.Phase1;
        phase2Started = false;
        isDead = false;
    }

    private float GetScaledHealthForFloor(int floor)
    {
        int bonusSteps = Mathf.Max(0, floor / 5 - 1);
        return baseMaxHealth + (bonusSteps * 100f);
    }

    public void TakeDamage(float damage)
    {
        TakeDamage(damage, 0);
    }

    public void TakeDamage(float damage, int slashChoice)
    {
        if (isDead) return;
        if (damage <= 0f) return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        Debug.Log($"AngelBossAI took {damage} damage. HP: {currentHealth}/{maxHealth}");

        UpdateBossUI();

        if (!phase2Started && currentHealth <= maxHealth * 0.5f)
        {
            EnterPhase2();
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void EnterPhase2()
    {
        phase2Started = true;
        currentPhase = BossPhase.Phase2;

        Debug.Log("AngelBossAI: Entered Phase 2");

        if (!isBusy && !isDead)
            StartCoroutine(SpawnPhase2CirclesNow());
    }
    private IEnumerator SpawnPhase2CirclesNow()
    {
        yield return null;

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag(playerTag);
            if (p != null)
                player = p.transform;
        }

        if (player == null)
        {
            Debug.LogWarning("AngelBossAI: Player not found for phase 2 circles.");
            yield break;
        }

        if (envycirclePrefab == null)
        {
            Debug.LogError("AngelBossAI: envycirclePrefab is not assigned.");
            yield break;
        }

        for (int i = 0; i < phase2CircleCount; i++)
        {
            Vector3 spawnPos = player.position + circleSpawnOffset;

            GameObject circle = Instantiate(envycirclePrefab, spawnPos, Quaternion.identity);
            StartCoroutine(HandleCircleDamage(circle, spawnPos));
        }
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;
        currentPhase = BossPhase.Dead;
        StopAllCoroutines();

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        UpdateBossUI();
        Debug.Log("AngelBossAI: Boss defeated.");
    }

    private void UpdateBossUI()
    {
        if (bossHealthBarFill != null)
        {
            bossHealthBarFill.fillAmount = (maxHealth > 0f) ? currentHealth / maxHealth : 0f;
        }

        if (bossHealthText != null)
        {
            bossHealthText.text = $"{Mathf.CeilToInt(currentHealth)}/{Mathf.CeilToInt(maxHealth)}";
        }
    }

    private IEnumerator TeleportAndAttack()
    {
        isBusy = true;

        Debug.Log("AngelBossAI: TeleportAndAttack started");

        TeleportToRandomNavMeshLocation();

        yield return new WaitForSeconds(delayAfterTeleport);

        if (isDead)
        {
            isBusy = false;
            yield break;
        }

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag(playerTag);
            if (p != null)
                player = p.transform;
        }

        if (player == null)
        {
            Debug.LogWarning("AngelBossAI: Player not found.");
            isBusy = false;
            yield break;
        }

        FaceTarget(player.position);

        if (animator != null && !string.IsNullOrEmpty(attackTriggerName))
            animator.SetTrigger(attackTriggerName);

        if (envycirclePrefab == null)
        {
            Debug.LogError("AngelBossAI: envycirclePrefab is not assigned.");
            isBusy = false;
            yield break;
        }

        int circleCount = (currentPhase == BossPhase.Phase2) ? phase2CircleCount : phase1CircleCount;

        for (int i = 0; i < circleCount; i++)
        {
            Vector3 spawnPos = player.position + circleSpawnOffset;
            spawnPos.y = player.position.y + circleSpawnOffset.y;

            GameObject circle = Instantiate(envycirclePrefab, spawnPos, Quaternion.identity);
            StartCoroutine(HandleCircleDamage(circle, spawnPos));
        }

        yield return new WaitForSeconds(attackCooldown);

        isBusy = false;
    }

    private IEnumerator HandleCircleDamage(GameObject circle, Vector3 circleCenter)
    {
        yield return new WaitForSeconds(circleDelayBeforeHit);

        if (player != null)
        {
            Vector3 playerFlat = player.position;
            Vector3 circleFlat = circleCenter;

            playerFlat.y = 0f;
            circleFlat.y = 0f;

            float dist = Vector3.Distance(playerFlat, circleFlat);

            if (dist <= circleDamageRadius)
            {
                Debug.Log("AngelBossAI: Player was inside circle and took damage.");

                PlayerHealth ph = player.GetComponent<PlayerHealth>();
                if (ph == null) ph = player.GetComponentInChildren<PlayerHealth>();
                if (ph == null) ph = player.GetComponentInParent<PlayerHealth>();

                if (ph != null)
                {
                    ph.TakeDamage(circleDamage);
                }
                else
                {
                    Debug.LogWarning("AngelBossAI: Could not find PlayerHealth on player.");
                }
            }
            else
            {
                Debug.Log("AngelBossAI: Player escaped the circle.");
            }
        }

        yield return new WaitForSeconds(Mathf.Max(0f, circleLifetime - circleDelayBeforeHit));

        if (circle != null)
            Destroy(circle);
    }
    private void TeleportToRandomNavMeshLocation()
    {
        Vector3 chosenPosition = transform.position;
        bool found = false;

        for (int i = 0; i < teleportAttempts; i++)
        {
            Vector2 random2D = Random.insideUnitCircle * teleportSearchRadius;
            Vector3 randomPoint = transform.position + new Vector3(random2D.x, 0f, random2D.y);

            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, navMeshSampleDistance, NavMesh.AllAreas))
            {
                chosenPosition = hit.position;
                found = true;
                break;
            }
        }

        if (!found && NavMesh.SamplePosition(transform.position, out NavMeshHit fallbackHit, navMeshSampleDistance, NavMesh.AllAreas))
        {
            chosenPosition = fallbackHit.position;
        }

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.Warp(chosenPosition);
            agent.ResetPath();
        }
        else
        {
            transform.position = chosenPosition;
        }
    }

    private Vector3 GetNearbyNavMeshPointNearPlayer(Transform playerTransform, float radius, float maxSampleDistance, int attempts)
    {
        if (playerTransform == null)
            return transform.position;

        for (int i = 0; i < attempts; i++)
        {
            Vector2 random2D = Random.insideUnitCircle * radius;
            Vector3 candidate = playerTransform.position + new Vector3(random2D.x, 0f, random2D.y);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, maxSampleDistance, NavMesh.AllAreas))
            {
                return hit.position;
            }
        }

        if (NavMesh.SamplePosition(playerTransform.position, out NavMeshHit fallback, maxSampleDistance, NavMesh.AllAreas))
            return fallback.position;

        return playerTransform.position;
    }

    private void FaceTarget(Vector3 targetPosition)
    {
        Vector3 flatDir = targetPosition - transform.position;
        flatDir.y = 0f;

        if (flatDir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(flatDir.normalized);
    }

    private IEnumerator AnimateAttackCircle(GameObject attackRoot)
    {
        if (attackRoot == null)
            yield break;

        Transform canvasTransform = FindDeepChild(attackRoot.transform, canvasChildName);
        Transform handsTransform = FindDeepChild(attackRoot.transform, handsParentName);
        Light spotLight = attackRoot.GetComponentInChildren<Light>();

        Vector3 handsStartLocalPos = Vector3.zero;
        Vector3 handsEndLocalPos = Vector3.zero;

        if (handsTransform != null)
        {
            handsEndLocalPos = handsTransform.localPosition;
            handsStartLocalPos = handsEndLocalPos;
            handsStartLocalPos.z = handsStartLocalZ;
            handsEndLocalPos.z = handsEndLocalZ;

            handsTransform.localPosition = handsStartLocalPos;
        }

        Quaternion canvasStartRot = Quaternion.identity;
        if (canvasTransform != null)
        {
            canvasStartRot = canvasTransform.localRotation;
        }

        Vector3 lightStartLocalPos = Vector3.zero;
        if (spotLight != null)
        {
            lightStartLocalPos = spotLight.transform.localPosition;
        }

        float elapsed = 0f;

        while (elapsed < handsRiseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, handsRiseDuration));
            float easedT = Mathf.SmoothStep(0f, 1f, t);

            if (handsTransform != null)
            {
                handsTransform.localPosition = Vector3.Lerp(handsStartLocalPos, handsEndLocalPos, easedT);
            }

            if (canvasTransform != null)
            {
                canvasTransform.localRotation = canvasStartRot * Quaternion.Euler(0f, 0f, canvasZRotateSpeed * elapsed);
            }

            if (spotLight != null)
            {
                float pulseT = Mathf.PingPong(elapsed / Mathf.Max(0.0001f, spotlightPulseDuration), 1f);
                float yOffset = Mathf.Lerp(-spotlightMoveAmount, spotlightMoveAmount, pulseT);

                Vector3 p = lightStartLocalPos;
                p.y += yOffset;
                spotLight.transform.localPosition = p;
            }

            yield return null;
        }

        if (handsTransform != null)
        {
            handsTransform.localPosition = handsEndLocalPos;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebugRange) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, engageRange);
    }

    private IEnumerator SinkAndDespawnAttack(GameObject attackRoot)
    {
        if (attackRoot == null)
            yield break;

        Transform[] allParts = attackRoot.GetComponentsInChildren<Transform>(true);
        Dictionary<Transform, Vector3> startPositions = new Dictionary<Transform, Vector3>();
        Dictionary<Transform, float> sinkSpeeds = new Dictionary<Transform, float>();

        foreach (Transform part in allParts)
        {
            if (part == attackRoot.transform) continue;

            startPositions[part] = part.position;
            sinkSpeeds[part] = Random.Range(minSinkSpeed, maxSinkSpeed);
        }

        float elapsed = 0f;

        while (elapsed < sinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / sinkDuration);

            foreach (Transform part in allParts)
            {
                if (part == null || part == attackRoot.transform) continue;

                Vector3 startPos = startPositions[part];
                float sinkAmount = sinkSpeeds[part] * t;
                part.position = startPos + Vector3.down * sinkAmount;
            }

            yield return null;
        }

        Destroy(attackRoot);
    }

    private Transform FindDeepChild(Transform parent, string childName)
    {
        if (parent == null || string.IsNullOrEmpty(childName))
            return null;

        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == childName)
                return child;
        }

        return null;
    }
}