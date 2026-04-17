using System.Collections;
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

    [SerializeField] private AudioClip circleSpawnClip;
    [SerializeField][Range(0f, 1f)] private float circleSpawnVolume = 1f;

    [SerializeField] private Color aliveLightColor = Color.green;
    [SerializeField] private Color deadLightColor = Color.white;

    [SerializeField] private AudioClip bossMusicClip;
    [SerializeField][Range(0f, 1f)] private float bossMusicVolume = 1f;
    [SerializeField] private float bossMusicFadeOutDuration = 1.5f;

    [SerializeField] private float baseMaxHealth = 650f;
    [SerializeField] private float currentHealth;
    [SerializeField] private float maxHealth;
    [SerializeField] private BossPhase currentPhase = BossPhase.Phase1;
    [SerializeField] private int currentFloor = 5;

    [SerializeField] private Animator animator;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private GameObject envycirclePrefab;
    [SerializeField] private Transform player;
    [SerializeField] private string playerTag = "Player";

    [SerializeField] private float teleportSearchRadius = 25f;
    [SerializeField] private float navMeshSampleDistance = 8f;
    [SerializeField] private int teleportAttempts = 20;

    [SerializeField] private float delayAfterTeleport = 3f;
    [SerializeField] private string attackTriggerName = "attack1";
    [SerializeField] private Vector3 circleSpawnOffset = Vector3.zero;

    [SerializeField] private float engageRange = 15f;
    [SerializeField] private float attackCooldown = 15f;
    [SerializeField] private float phase1DelayAfterEachCircleEnds = 2f;
    [SerializeField] private float phase2DelayBetweenSpawns = 1f;
    [SerializeField] private float phase2DelayAfterLastCircleEnds = 2f;

    [SerializeField] private float circleDelayBeforeHit = 1.5f;
    [SerializeField] private float circleDamageRadius = 3f;
    [SerializeField] private float circleDamage = 25f;
    [SerializeField] private float circleLifetime = 2.5f;

    [SerializeField] private bool drawDebugRange = true;

    private Light[] controlledLights = new Light[0];
    private Transform basementDoorLeft;
    private Transform basementDoorRight;
    private AudioSource gateAudioSource;
    private AudioClip largeGateClip;
    private AudioSource backgroundMusicSource;
    private GameObject bossChestPrefab;
    private Transform bossChestSpawnPoint;

    private Image bossHealthBarFill;
    private TMP_Text bossHealthText;
    private GameObject bossHealthUIRoot;

    private float doorMoveDistanceZ = 1f;
    private float doorMoveDuration = 1f;

    private float nextAttackTime = 0f;
    private bool isBusy = false;
    private bool phase2Started = false;
    private bool isDead = false;
    private bool deathHandled = false;
    private Coroutine aiLoopRoutine;
    private Coroutine musicFadeRoutine;

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

        FindPlayerIfNeeded();
    }

    private void Start()
    {
        SetupHealthForFloor(currentFloor);
        UpdateBossUI();
        ApplyAliveLightColor();

        if (bossHealthUIRoot != null)
            bossHealthUIRoot.SetActive(true);

        if (aiLoopRoutine != null)
            StopCoroutine(aiLoopRoutine);

        aiLoopRoutine = StartCoroutine(BossLoop());
    }

    public void SetupArenaReferences(
        Light[] arenaLights,
        Transform leftDoor,
        Transform rightDoor,
        AudioSource gateSource,
        AudioClip gateClip,
        AudioSource musicSource,
        GameObject chestPrefab,
        Transform chestSpawnPoint,
        Image healthBarFill,
        TMP_Text healthText,
        GameObject healthUIRoot,
        float doorDistanceZ,
        float doorOpenDuration)
    {
        controlledLights = arenaLights ?? new Light[0];
        basementDoorLeft = leftDoor;
        basementDoorRight = rightDoor;
        gateAudioSource = gateSource;
        largeGateClip = gateClip;
        backgroundMusicSource = musicSource;
        bossChestPrefab = chestPrefab;
        bossChestSpawnPoint = chestSpawnPoint;

        bossHealthBarFill = healthBarFill;
        bossHealthText = healthText;
        bossHealthUIRoot = healthUIRoot;

        doorMoveDistanceZ = doorDistanceZ;
        doorMoveDuration = doorOpenDuration;

        if (bossHealthUIRoot != null)
            bossHealthUIRoot.SetActive(true);

        UpdateBossUI();
        ApplyAliveLightColor();
        StartBossMusic();
    }

    private void StartBossMusic()
    {
        if (backgroundMusicSource == null || bossMusicClip == null)
            return;

        if (musicFadeRoutine != null)
            StopCoroutine(musicFadeRoutine);

        backgroundMusicSource.clip = bossMusicClip;
        backgroundMusicSource.volume = bossMusicVolume;
        backgroundMusicSource.loop = true;
        backgroundMusicSource.Play();
    }

    private void FadeOutBossMusic()
    {
        if (backgroundMusicSource == null)
            return;

        if (musicFadeRoutine != null)
            StopCoroutine(musicFadeRoutine);

        musicFadeRoutine = StartCoroutine(FadeOutMusicRoutine());
    }

    private IEnumerator FadeOutMusicRoutine()
    {
        float startVolume = backgroundMusicSource.volume;
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, bossMusicFadeOutDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            backgroundMusicSource.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }

        backgroundMusicSource.volume = 0f;
        backgroundMusicSource.Stop();
        backgroundMusicSource.clip = null;
        backgroundMusicSource.loop = false;
        musicFadeRoutine = null;
    }

    private void ApplyAliveLightColor()
    {
        if (controlledLights == null) return;

        for (int i = 0; i < controlledLights.Length; i++)
        {
            if (controlledLights[i] != null)
                controlledLights[i].color = aliveLightColor;
        }
    }

    private void ApplyDeadLightColor()
    {
        if (controlledLights == null) return;

        for (int i = 0; i < controlledLights.Length; i++)
        {
            if (controlledLights[i] != null)
                controlledLights[i].color = deadLightColor;
        }
    }

    private IEnumerator BossLoop()
    {
        while (!isDead)
        {
            FindPlayerIfNeeded();

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

    private void FindPlayerIfNeeded()
    {
        if (player != null) return;

        GameObject p = GameObject.FindGameObjectWithTag(playerTag);
        if (p != null)
            player = p.transform;
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
        deathHandled = false;
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
        if (isDead || damage <= 0f)
            return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        UpdateBossUI();

        if (!phase2Started && currentHealth <= maxHealth * 0.5f)
            EnterPhase2();

        if (currentHealth <= 0f)
            Die();
    }

    private void EnterPhase2()
    {
        phase2Started = true;
        currentPhase = BossPhase.Phase2;
        Debug.Log($"{name}: Entered Phase 2");
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
        ApplyDeadLightColor();
        FadeOutBossMusic();

        if (!deathHandled)
            StartCoroutine(HandleDeathSequence());
    }

    private IEnumerator HandleDeathSequence()
    {
        deathHandled = true;

        if (gateAudioSource != null)
        {
            if (largeGateClip != null)
                gateAudioSource.PlayOneShot(largeGateClip);
            else
                gateAudioSource.Play();
        }

        Vector3 leftStart = basementDoorLeft != null ? basementDoorLeft.position : Vector3.zero;
        Vector3 rightStart = basementDoorRight != null ? basementDoorRight.position : Vector3.zero;

        Vector3 leftEnd = leftStart + new Vector3(0f, 0f, doorMoveDistanceZ);
        Vector3 rightEnd = rightStart + new Vector3(0f, 0f, -doorMoveDistanceZ);

        float elapsed = 0f;

        while (elapsed < doorMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, doorMoveDuration));
            float eased = Mathf.SmoothStep(0f, 1f, t);

            if (basementDoorLeft != null)
                basementDoorLeft.position = Vector3.Lerp(leftStart, leftEnd, eased);

            if (basementDoorRight != null)
                basementDoorRight.position = Vector3.Lerp(rightStart, rightEnd, eased);

            yield return null;
        }

        if (basementDoorLeft != null)
            basementDoorLeft.position = leftEnd;

        if (basementDoorRight != null)
            basementDoorRight.position = rightEnd;

        SpawnBossChest();
    }

    private void SpawnBossChest()
    {
        if (bossChestPrefab == null)
            return;

        Vector3 spawnPos = bossChestSpawnPoint != null ? bossChestSpawnPoint.position : transform.position;
        Quaternion spawnRot = bossChestSpawnPoint != null ? bossChestSpawnPoint.rotation : Quaternion.identity;

        Instantiate(bossChestPrefab, spawnPos, spawnRot);
    }

    private void UpdateBossUI()
    {
        if (bossHealthBarFill != null)
            bossHealthBarFill.fillAmount = (maxHealth > 0f) ? currentHealth / maxHealth : 0f;

        if (bossHealthText != null)
            bossHealthText.text = $"{Mathf.CeilToInt(currentHealth)}/{Mathf.CeilToInt(maxHealth)}";
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

        FindPlayerIfNeeded();

        if (player == null)
        {
            Debug.LogWarning($"{name}: Player not found.");
            isBusy = false;
            yield break;
        }

        FaceTarget(player.position);

        if (animator != null && !string.IsNullOrEmpty(attackTriggerName))
            animator.SetTrigger(attackTriggerName);

        if (envycirclePrefab == null)
        {
            Debug.LogError($"{name}: envycirclePrefab is not assigned.");
            isBusy = false;
            yield break;
        }

        if (currentPhase == BossPhase.Phase1)
            yield return StartCoroutine(DoPhase1CircleAttack());
        else if (currentPhase == BossPhase.Phase2)
            yield return StartCoroutine(DoPhase2CircleAttack());

        isBusy = false;
    }

    private IEnumerator DoPhase1CircleAttack()
    {
        const int circleCount = 3;

        for (int i = 0; i < circleCount; i++)
        {
            GameObject circle = SpawnCircleAtCurrentPlayerPosition();

            if (circle != null)
                yield return StartCoroutine(HandleCircleDamage(circle));

            if (i < circleCount - 1)
                yield return new WaitForSeconds(phase1DelayAfterEachCircleEnds);
        }
    }

    private IEnumerator DoPhase2CircleAttack()
    {
        const int circleCount = 3;
        float totalCircleDuration = Mathf.Max(circleLifetime, circleDelayBeforeHit);

        for (int i = 0; i < circleCount; i++)
        {
            GameObject circle = SpawnCircleAtCurrentPlayerPosition();

            if (circle != null)
                StartCoroutine(HandleCircleDamage(circle));

            if (i < circleCount - 1)
                yield return new WaitForSeconds(phase2DelayBetweenSpawns);
        }

        yield return new WaitForSeconds(totalCircleDuration + phase2DelayAfterLastCircleEnds);
    }

    private GameObject SpawnCircleAtCurrentPlayerPosition()
    {
        FindPlayerIfNeeded();

        if (player == null || envycirclePrefab == null)
            return null;

        Vector3 spawnPos = player.position + circleSpawnOffset;
        GameObject circle = Instantiate(envycirclePrefab, spawnPos, Quaternion.identity);

        if (circleSpawnClip != null)
            AudioSource.PlayClipAtPoint(circleSpawnClip, spawnPos, circleSpawnVolume);

        return circle;
    }

    private IEnumerator HandleCircleDamage(GameObject circle)
    {
        if (circle == null)
            yield break;

        Vector3 circleCenter = circle.transform.position;

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
                PlayerHealth ph = player.GetComponent<PlayerHealth>();
                if (ph == null) ph = player.GetComponentInChildren<PlayerHealth>();
                if (ph == null) ph = player.GetComponentInParent<PlayerHealth>();

                if (ph != null)
                    ph.TakeDamage(circleDamage);
                else
                    Debug.LogWarning($"{name}: Could not find PlayerHealth on player.");
            }
        }

        float remainingLifetime = Mathf.Max(0f, circleLifetime - circleDelayBeforeHit);
        yield return new WaitForSeconds(remainingLifetime);

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

    private void FaceTarget(Vector3 targetPosition)
    {
        Vector3 flatDir = targetPosition - transform.position;
        flatDir.y = 0f;

        if (flatDir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(flatDir.normalized);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebugRange) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, engageRange);
    }
}