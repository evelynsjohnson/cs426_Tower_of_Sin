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

    [Header("Boss Stats")]
    [SerializeField] private float baseMaxHealth = 1000f;
    [SerializeField] private float currentHealth;
    [SerializeField] private float maxHealth;
    [SerializeField] private BossPhase currentPhase = BossPhase.Phase1;

    [SerializeField] private int currentFloor = 5;

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private GameObject envycirclePrefab;
    [SerializeField] private Transform player;

    [Header("UI")]
    [SerializeField] private Image bossHealthBarFill;
    [SerializeField] private TMP_Text bossHealthText;
    [SerializeField] private GameObject bossHealthUIRoot;

    [Header("Player Search")]
    [SerializeField] private string playerTag = "Player";

    [Header("Teleport")]
    [SerializeField] private float phase1TeleportInterval = 15f;
    [SerializeField] private float teleportSearchRadius = 25f;
    [SerializeField] private float navMeshSampleDistance = 8f;
    [SerializeField] private int teleportAttempts = 20;

    [Header("Attack Timing")]
    [SerializeField] private float delayAfterTeleport = 3f;
    [SerializeField] private string attackTriggerName = "attack1";
    [SerializeField] private float bossAttackAnimationDuration = 3f;

    [Header("Circle Spawn")]
    [SerializeField] private Vector3 circleSpawnOffset = Vector3.zero;
    [SerializeField] private float circleSpawnRadius = 4f;
    [SerializeField] private int phase1CircleCount = 1;
    [SerializeField] private int phase2CircleCount = 3;
    [SerializeField] private int circleSpawnAttempts = 12;

    [Header("Attack Circle Animation")]
    [SerializeField] private float visualAnimDuration = 2.25f;
    [SerializeField] private float handStartScale = 1f;
    [SerializeField] private float handEndScale = 2f;

    [SerializeField] private float canvasStartScale = 0f;
    [SerializeField] private float canvasEndScale = 0.3333333f;
    [SerializeField] private Vector3 canvasRotationPerSecond = new Vector3(0f, 90f, 0f);

    [Header("Spotlight Pulse")]
    [SerializeField] private float spotlightMoveAmount = 0.4f;
    [SerializeField] private float spotlightPulseDuration = 1f;

    [Header("Hand Rotation")]
    [SerializeField] private float handRotateSpeed = 100f;
    [SerializeField] private float handWobbleAngle = 30f;
    [SerializeField] private float handWobbleSpeed = 2f;

    [Header("Sink / Despawn")]
    [SerializeField] private float sinkDuration = 2.5f;
    [SerializeField] private float minSinkSpeed = 0.35f;
    [SerializeField] private float maxSinkSpeed = 1.2f;

    [Header("Prefab Child Names")]
    [SerializeField] private string canvasChildName = "Canvas";
    [SerializeField] private string handsParentName = "Hands";
    [SerializeField] private bool autoCollectHandsIfNoParentFound = true;

    private bool isBusy = false;
    private bool phase2Started = false;
    private bool isDead = false;
    private Coroutine aiLoopRoutine;

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
            if (p != null) player = p.transform;
        }
    }

    private void Start()
    {
        SetupHealthForFloor(currentFloor);
        UpdateBossUI();

        if (bossHealthUIRoot != null)
            bossHealthUIRoot.SetActive(true);

        aiLoopRoutine = StartCoroutine(BossLoop());
    }

    private IEnumerator BossLoop()
    {
        while (!isDead)
        {
            if (currentPhase == BossPhase.Phase1)
            {
                yield return new WaitForSeconds(phase1TeleportInterval);

                if (!isBusy && !isDead)
                    yield return StartCoroutine(TeleportAndAttack());
            }
            else if (currentPhase == BossPhase.Phase2)
            {
                // Keep behavior similar unless you want phase 2 timing changed later.
                yield return new WaitForSeconds(phase1TeleportInterval);

                if (!isBusy && !isDead)
                    yield return StartCoroutine(TeleportAndAttack());
            }
            else
            {
                yield break;
            }
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
        // Floor 5 gets no bonus.
        // Floor 10 = +100
        // Floor 15 = +200
        // Floor 20 = +300
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

        currentHealth -= damage;
        currentHealth = Mathf.Max(0f, currentHealth);

        UpdateBossUI();

        if (!phase2Started && currentHealth <= 500f)
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

        // Optional animator trigger if you have one:
        // animator?.SetTrigger("phase2");
        Debug.Log("AngelBossAI: Entered Phase 2");
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;
        currentPhase = BossPhase.Dead;
        StopAllCoroutines();

        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        if (animator != null)
        {
            // Optional if you have a death trigger:
            // animator.SetTrigger("die");
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
            if (p != null) player = p.transform;
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

        int circleCount = (currentPhase == BossPhase.Phase2) ? phase2CircleCount : phase1CircleCount;

        List<GameObject> spawnedCircles = new List<GameObject>();

        for (int i = 0; i < circleCount; i++)
        {
            Vector3 spawnPos = GetNearbyNavMeshPointNearPlayer(player, circleSpawnRadius, navMeshSampleDistance, circleSpawnAttempts);

            GameObject circle = Instantiate(envycirclePrefab, spawnPos + circleSpawnOffset, Quaternion.identity);
            spawnedCircles.Add(circle);
        }

        List<Coroutine> anims = new List<Coroutine>();
        foreach (GameObject circle in spawnedCircles)
        {
            if (circle != null)
                anims.Add(StartCoroutine(AnimateAttackCircle(circle)));
        }

        yield return new WaitForSeconds(Mathf.Max(0f, bossAttackAnimationDuration));

        List<Coroutine> sinks = new List<Coroutine>();
        foreach (GameObject circle in spawnedCircles)
        {
            if (circle != null)
                sinks.Add(StartCoroutine(SinkAndDespawnAttack(circle)));
        }

        yield return new WaitForSeconds(sinkDuration);

        isBusy = false;
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

        if (!found)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit fallbackHit, navMeshSampleDistance, NavMesh.AllAreas))
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
        {
            return fallback.position;
        }

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
        Transform handsParent = FindDeepChild(attackRoot.transform, handsParentName);
        Light spotLight = attackRoot.GetComponentInChildren<Light>();

        List<Transform> hands = new List<Transform>();

        if (handsParent != null)
        {
            foreach (Transform child in handsParent)
                hands.Add(child);
        }
        else if (autoCollectHandsIfNoParentFound)
        {
            Transform[] allChildren = attackRoot.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in allChildren)
            {
                if (t != attackRoot.transform && t != canvasTransform)
                    hands.Add(t);
            }
        }

        Dictionary<Transform, Vector3> handBaseScales = new Dictionary<Transform, Vector3>();

        foreach (Transform hand in hands)
        {
            if (hand == null) continue;
            handBaseScales[hand] = hand.localScale;
        }

        if (canvasTransform != null)
        {
            canvasTransform.localScale = Vector3.one * canvasStartScale;
        }

        Vector3 lightStartLocalPos = Vector3.zero;
        if (spotLight != null)
            lightStartLocalPos = spotLight.transform.localPosition;

        float elapsed = 0f;

        while (elapsed < visualAnimDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / visualAnimDuration);
            float eased = Mathf.SmoothStep(0f, 1f, t);

            foreach (Transform hand in hands)
            {
                if (hand == null) continue;

                Vector3 baseScale = handBaseScales[hand];
                float scaleFactor = Mathf.Lerp(handStartScale, handEndScale, eased);
                hand.localScale = baseScale * scaleFactor;

                Vector3 toCenter = (attackRoot.transform.position - hand.position).normalized;
                if (toCenter.sqrMagnitude > 0.001f)
                {
                    Quaternion towardCenter = Quaternion.LookRotation(toCenter);
                    Quaternion awayFromCenter = Quaternion.LookRotation(-toCenter);

                    float wobble = Mathf.Sin(Time.time * handWobbleSpeed + hand.GetSiblingIndex()) * 0.5f + 0.5f;
                    Quaternion targetRotation = Quaternion.Slerp(towardCenter, awayFromCenter, wobble);
                    hand.rotation = Quaternion.RotateTowards(hand.rotation, targetRotation, handRotateSpeed * Time.deltaTime);
                }
            }

            if (canvasTransform != null)
            {
                float canvasScale = Mathf.Lerp(canvasStartScale, canvasEndScale, eased);
                canvasTransform.localScale = Vector3.one * canvasScale;
                canvasTransform.Rotate(canvasRotationPerSecond * Time.deltaTime, Space.Self);
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