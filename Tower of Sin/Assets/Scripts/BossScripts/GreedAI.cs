using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Audio;

public class GreedAI : MonoBehaviour
{
    public enum BossPhase
    {
        Dormant,
        Phase1,
        Phase2,
        Phase4,
        Dead
    }

    private BossArenaController arenaController;
    [SerializeField] private AudioMixerGroup narrationMixer;
    [SerializeField] private AudioMixerGroup sfxMixer;

    [Header("Core References")]
    [SerializeField] private Animator animator;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform player;
    [SerializeField] private Transform spawnPointOverride;
    [SerializeField] private Transform ledgePointOverride;

    [Header("Boss References")]
    [SerializeField] private Transform roomCenter;
    [SerializeField] private Transform bossSpawnPoint;
    [SerializeField] private Transform bossSpawnPointLedge;

    [Header("Detection / Movement")]
    [SerializeField] private float wakeRange = 15f;
    [SerializeField] private float attackRange = 7f;
    [SerializeField] private float phase4MinDistance = 7f;
    [SerializeField] private float faceSpeed = 10f;

    [Header("Health / Damage")]
    [SerializeField] private float baseMaxHP = 500f;
    [SerializeField] private float baseAttackDamage = 25f;

    [Header("Attack 1 - Sweep")]
    [SerializeField] private int attack1SweepSteps = 12;
    [SerializeField] private float attack1SweepDamageWindow = 0.35f;
    [SerializeField] private float attack1TelegraphTime = 1.25f;
    [SerializeField] private float attack1ActiveTime = 0.4f;
    [SerializeField] private float attack1Cooldown = 1f;
    [SerializeField] private float attack1ConeAngle = 75f;
    [SerializeField] private float attack1ConeLength = 8f;
    [SerializeField] private float attack1DamageStartDelay = 0.25f;

    [Header("Phase 2")]
    [SerializeField] private float roomWidth = 30f;
    [SerializeField] private float roomLength = 40f;
    [SerializeField] private float telegraphHeight = 0.05f;
    [SerializeField] private float version1TelegraphDuration = 1.3f;
    [SerializeField] private float version1BurstGap = 0.15f;
    [SerializeField] private float version2LightGap = 0.35f;
    [SerializeField] private float version2DetonationGap = 0.35f;
    [SerializeField] private float version2PauseBeforeReverse = 0f;
    [SerializeField] private float phase2LoopPause = 1f;
    [SerializeField] private GameObject explosionPrefab;

    [SerializeField] private float phase4LoopPause = 7f;

    [Header("Phase 4")]
    [SerializeField] private GameObject skeletonPrefab;
    [SerializeField] private int phase4MinSoldiersPerSpawn = 2;
    [SerializeField] private int phase4MaxSoldiersPerSpawn = 6;
    [SerializeField] private float phase4MinSoldierSpawnInterval = 8f;
    [SerializeField] private float phase4MaxSoldierSpawnInterval = 10f;
    [SerializeField] private float navmeshSpawnRadius = 18f;
    [SerializeField] private float navmeshSampleRadius = 8f;
    private Coroutine phase4SoldierSpawnRoutine;

    [SerializeField] private Image bossHealthBarFill;
    [SerializeField] private TMP_Text bossHealthText;
    [SerializeField] private GameObject bossHealthUIRoot;

    [SerializeField] private float telegraphLineWidth = 0.15f;
    [SerializeField] private float telegraphYOffset = 0.05f;
    [SerializeField] private int coneArcSegments = 20;
    [SerializeField] private Color telegraphFillColor = new Color(1f, 0.2f, 0.05f, 0.22f);
    [SerializeField] private Color telegraphOutlineColor = new Color(0.45f, 0.05f, 0.02f, 0.95f);

    [SerializeField] private Color bossLightColor = new Color(1f, 0.55f, 0.12f);
    [SerializeField] private float lightIntensityMultiplier = 1.25f;

    [Header("Phase 4")]
    [SerializeField] private float phase4MinPhase1Interval = 4f;
    [SerializeField] private float phase4MaxPhase1Interval = 9f;

    [Header("Audio")]
    [SerializeField] private AudioClip cannonFireClip;
    [SerializeField] private AudioClip bossLoopClip;
    [SerializeField] private AudioClip walkingClip;
    [SerializeField] private AudioClip bossMusicClip;
    [SerializeField] private AudioClip[] randomSfxClips;
    [SerializeField] private Vector2 randomSfxIntervalRange = new Vector2(3f, 8f);
    [SerializeField] private float oneShotSpatialBlend = 1f;
    [SerializeField] private float oneShotMinDistance = 2f;
    [SerializeField] private float oneShotMaxDistance = 25f;

    [SerializeField] private AudioClip introDialogueClip;
    [SerializeField] private AudioClip phase2DialogueClip;
    [SerializeField] private AudioClip phase4DialogueClip;
    [SerializeField] private AudioClip deathDialogueClip;

    [SerializeField] private AudioClip[] attackVoiceClips; // 5 clips
    [SerializeField] private Vector2 attackVoiceIntervalRange = new Vector2(5f, 10f);
    [SerializeField] private float deathFadeOutDuration = 0.25f;

    [SerializeField] private AudioClip attack1TriggerClip;

    private AudioSource dialogueAudioSource;
    private Coroutine attackVoiceRoutine;
    private bool introDialoguePlayed = false;
    private bool phase2DialoguePlayed = false;
    private bool phase4DialoguePlayed = false;
    private int attackVoiceIndex = 0;

    [SerializeField] private bool drawGizmos = true;

    private static readonly int AnimAttack1 = Animator.StringToHash("attack1Sweep");
    private static readonly int AnimAttack2 = Animator.StringToHash("attack2Slam");
    private static readonly int AnimSpawned = Animator.StringToHash("hasSpawned");
    private static readonly int AnimRunning = Animator.StringToHash("isRunning");
    private static readonly int AnimDeath = Animator.StringToHash("death");

    private BossPhase currentPhase = BossPhase.Dormant;
    private BossPhase requestedPhase = BossPhase.Dormant;

    private Vector3 originalSpawnPoint;
    private Quaternion originalSpawnRotation;

    private float maxHP;
    private float currentHP;
    private float scaledAttackDamage;

    private int currentFloor = 5;
    private bool hasSpawned = false;
    private bool isInvulnerable = false;
    private bool isDead = false;
    private bool isBusy = false;
    private bool isTransitioning = false;
    private bool phase2PatternToggle = false;
    private bool didEnterPhase2 = false;
    private bool didEnterPhase4 = false;

    private AudioSource loopAudioSource;
    private AudioSource walkAudioSource;
    private AudioSource musicAudioSource;
    private AudioClip previousBackgroundClip;
    private Coroutine randomVoiceRoutine;

    private readonly List<GameObject> spawnedTelegraphs = new List<GameObject>();
    private readonly List<GameObject> spawnedExplosions = new List<GameObject>();
    private readonly List<GameObject> spawnedSkeletons = new List<GameObject>();

    public void SetArenaController(BossArenaController controller)
    {
        arenaController = controller;

        if (arenaController != null)
        {
            bossHealthBarFill = arenaController.GetBossHealthBarFill();
            bossHealthText = arenaController.GetBossHealthText();
            bossHealthUIRoot = arenaController.GetBossHealthUIRoot();

            arenaController.OnBossSpawned(
                bossLightColor,
                lightIntensityMultiplier,
                bossMusicClip,
                1f
            );
        }

        UpdateBossUI();
    }

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (agent == null) agent = GetComponent<NavMeshAgent>();

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }

        originalSpawnPoint = transform.position;
        originalSpawnRotation = transform.rotation;

        if (bossSpawnPoint != null)
            originalSpawnPoint = bossSpawnPoint.position;
    }

    private void Start()
    {
        RecalculateScaledStats();
        currentHP = maxHP;
        UpdateBossUI();

        SetupAudioSources();

        if (bossHealthUIRoot != null)
            bossHealthUIRoot.SetActive(true);

        if (randomVoiceRoutine != null)
            StopCoroutine(randomVoiceRoutine);
        randomVoiceRoutine = StartCoroutine(RandomVoiceLoop());

        StartCoroutine(BossBrain());
    }

    private void Update()
    {
        if (isDead) return;

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
            return;
        }

        HandleContinuousFacing();

        UpdateWalkAudio();

        bool running = HasValidNavMeshAgent() && agent.velocity.magnitude > 0.1f;
        animator.SetBool(AnimRunning, running);

        HandlePhaseRequestsByHealth();
    }

    private void HandleContinuousFacing()
    {
        if (player == null || isDead) return;

        // Don't override the initial Phase 2
        bool walkingBackToSpawnInPhase2 =
            currentPhase == BossPhase.Phase2 &&
            isTransitioning &&
            isBusy &&
            HasValidNavMeshAgent();

        if (walkingBackToSpawnInPhase2)
            return;

        // If not actively moving somewhere else, keep facing player
        if (!HasValidNavMeshAgent() || agent.velocity.magnitude < 0.05f || isBusy)
        {
            Vector3 dir = player.position - transform.position;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRot,
                    faceSpeed * Time.deltaTime
                );
            }
        }
    }

    #region Public API

    public void SetFloor(int floor)
    {
        currentFloor = Mathf.Max(5, floor);
        RecalculateScaledStats();
        currentHP = Mathf.Min(currentHP <= 0 ? maxHP : currentHP, maxHP);
        UpdateBossUI();
    }

    public void SetupArenaReferences(
        Light[] arenaLights,
        Transform basementDoorLeft,
        Transform basementDoorRight,
        AudioSource gateAudioSource,
        AudioClip largeGateClip,
        AudioSource backgroundMusicSource,
        GameObject bossChestPrefab,
        Transform bossChestSpawnPoint,
        Image bossHealthBarFill,
        TMP_Text bossHealthText,
        GameObject bossHealthUIRoot,
        float doorMoveDistanceZ,
        float doorMoveDuration
    )
    {

        this.bossHealthBarFill = bossHealthBarFill;
        this.bossHealthText = bossHealthText;
        this.bossHealthUIRoot = bossHealthUIRoot;

        musicAudioSource = backgroundMusicSource;

        FindBackgroundMusicSourceIfNeeded();

        UpdateBossUI();
    }

    public void TakeDamage(float amount)
    {
        if (isDead || isInvulnerable) return;

        currentHP -= amount;
        currentHP = Mathf.Max(0f, currentHP);
        UpdateBossUI();

        if (currentHP <= 0f)
            Die();
    }

    public void TakeDamage(int amount)
    {
        TakeDamage((float)amount);
    }

    #endregion

    #region Boss Brain

    private IEnumerator BossBrain()
    {
        while (!isDead)
        {
            if (player == null)
            {
                yield return null;
                continue;
            }

            switch (currentPhase)
            {
                case BossPhase.Dormant:
                    yield return HandleDormant();
                    break;
                case BossPhase.Phase1:
                    yield return HandlePhase1();
                    break;
                case BossPhase.Phase2:
                    yield return HandlePhase2();
                    break;
                case BossPhase.Phase4:
                    yield return HandlePhase4();
                    break;
            }

            yield return null;
        }
    }

    private IEnumerator HandleDormant()
    {
        while (!hasSpawned && !isDead)
        {
            float dist = Vector3.Distance(transform.position, player.position);
            if (dist <= wakeRange)
            {
                isInvulnerable = true;
                isBusy = true;

                yield return StartCoroutine(PlaySpawnSequence());

                if (!introDialoguePlayed)
                {
                    introDialoguePlayed = true;
                    yield return StartCoroutine(PlayPhaseDialogue(introDialogueClip));
                    StartAttackVoiceCycle();
                }

                isInvulnerable = false;
                isBusy = false;

                currentPhase = BossPhase.Phase1;
                requestedPhase = BossPhase.Phase1;
                yield break;
            }

            yield return null;
        }
    }

    private IEnumerator HandlePhase1()
    {
        while (currentPhase == BossPhase.Phase1 && !isDead)
        {
            if (TryProcessRequestedPhase())
                yield break;

            float dist = Vector3.Distance(transform.position, player.position);

            if (!isBusy)
            {
                if (dist > attackRange)
                {
                    MoveTowardsPlayer(0f);
                }
                else
                {
                    StopMoving();
                    yield return StartCoroutine(Attack1ConeSweep());
                }
            }

            yield return null;
        }
    }
    private IEnumerator HandlePhase2()
    {
        if (!isTransitioning)
            yield return StartCoroutine(TransitionToPhase2());

        while (currentPhase == BossPhase.Phase2 && !isDead)
        {
            if (requestedPhase == BossPhase.Phase4 && !isBusy)
            {
                currentPhase = BossPhase.Phase4;
                yield break;
            }

            if (!isBusy)
            {
                if (!phase2PatternToggle)
                    yield return StartCoroutine(Phase2Version1_Columns());
                else
                    yield return StartCoroutine(Phase2Version2_Columns());

                phase2PatternToggle = !phase2PatternToggle;
                yield return new WaitForSeconds(phase2LoopPause);
            }

            yield return null;
        }
    }

    private IEnumerator HandlePhase4()
    {
        if (!isTransitioning)
            yield return StartCoroutine(TransitionToPhase4());

        Coroutine extraPhase1Routine = StartCoroutine(Phase4RandomConeRoutine());

        while (currentPhase == BossPhase.Phase4 && !isDead)
        {
            if (!isBusy)
            {
                if (!phase2PatternToggle)
                    yield return StartCoroutine(Phase2Version1_Columns());
                else
                    yield return StartCoroutine(Phase2Version2_Columns());

                phase2PatternToggle = !phase2PatternToggle;
                yield return StartCoroutine(Phase4MoveDuringPause(phase4LoopPause));
            }
            else
            {
                MaintainPhase4Distance();
                yield return null;
            }
        }

        if (extraPhase1Routine != null)
            StopCoroutine(extraPhase1Routine);
    }
    private IEnumerator Phase4MoveDuringPause(float duration)
    {
        float timer = 0f;

        while (timer < duration && currentPhase == BossPhase.Phase4 && !isDead)
        {
            MaintainPhase4Distance();
            timer += Time.deltaTime;
            yield return null;
        }

        StopMoving();
    }
    private IEnumerator Phase4RandomConeRoutine()
    {
        while (currentPhase == BossPhase.Phase4 && !isDead)
        {
            float wait = Random.Range(phase4MinPhase1Interval, phase4MaxPhase1Interval);
            yield return new WaitForSeconds(wait);

            if (currentPhase != BossPhase.Phase4 || isDead)
                yield break;

            StartCoroutine(Phase4ConcurrentConeAttack());
        }
    }

    private IEnumerator Phase4ConcurrentConeAttack()
    {
        if (isDead || player == null)
            yield break;

        Vector3 attackForward = GetFlatDirectionToPlayer();
        if (attackForward.sqrMagnitude < 0.001f)
            attackForward = transform.forward;

        GameObject cone = SpawnConeTelegraph(attackForward);
        yield return new WaitForSeconds(attack1TelegraphTime);

        animator.ResetTrigger(AnimAttack1);
        animator.SetTrigger(AnimAttack1);
        PlayAttack1TriggerSound();

        yield return new WaitForSeconds(attack1DamageStartDelay);
        yield return StartCoroutine(DealSweepingConeDamage(attackForward));

        yield return new WaitForSeconds(attack1ActiveTime);

        if (cone != null)
            Destroy(cone);
    }

    #endregion

    #region Spawn / Transitions

    private IEnumerator PlaySpawnSequence()
    {
        hasSpawned = true;
        isBusy = true;

        StopMoving();
        FacePlayerImmediate();

        animator.SetBool(AnimSpawned, true);

        yield return new WaitForSeconds(2f);

        isBusy = false;
    }

    private void HandlePhaseRequestsByHealth()
    {
        if (isDead) return;

        float hpPercent = currentHP / maxHP;

        if (!didEnterPhase2 && hpPercent <= 0.75f)
        {
            didEnterPhase2 = true;
            requestedPhase = BossPhase.Phase2;
        }

        if (!didEnterPhase4 && hpPercent <= 0.5f)
        {
            didEnterPhase4 = true;
            requestedPhase = BossPhase.Phase4;
        }
    }

    private bool TryProcessRequestedPhase()
    {
        if (requestedPhase == currentPhase) return false;
        if (requestedPhase == BossPhase.Dormant || requestedPhase == BossPhase.Dead) return false;
        if (isBusy) return false;

        currentPhase = requestedPhase;
        return true;
    }
    private IEnumerator TransitionToPhase2()
    {
        isTransitioning = true;
        isBusy = true;
        isInvulnerable = true;

        StopAttackVoiceCycle();
        StopMoving();

        Vector3 target = bossSpawnPoint != null ? bossSpawnPoint.position : originalSpawnPoint;
        yield return StartCoroutine(MoveToPoint(target, 0.5f));

        StopMoving();
        yield return StartCoroutine(FacePlayerOverTime(0.35f));

        if (!phase2DialoguePlayed)
        {
            phase2DialoguePlayed = true;
            yield return StartCoroutine(PlayPhaseDialogue(phase2DialogueClip));
        }

        StartAttackVoiceCycle();

        isInvulnerable = false;
        isBusy = false;
        isTransitioning = false;
    }

    private IEnumerator TransitionToPhase4()
    {
        isTransitioning = true;
        isBusy = true;
        isInvulnerable = true;

        StopAttackVoiceCycle();
        StartPhase4SoldierSpawning();

        yield return StartCoroutine(FacePlayerOverTime(0.25f));

        if (!phase4DialoguePlayed)
        {
            phase4DialoguePlayed = true;
            yield return StartCoroutine(PlayPhaseDialogue(phase4DialogueClip));
        }

        StartAttackVoiceCycle();

        isInvulnerable = false;
        isBusy = false;
        isTransitioning = false;
    }


    private IEnumerator PlayPhaseDialogue(AudioClip clip)
    {
        if (clip == null)
            yield break;

        if (dialogueAudioSource == null)
            yield break;

        dialogueAudioSource.Stop();
        dialogueAudioSource.clip = clip;
        dialogueAudioSource.volume = 1f;
        dialogueAudioSource.Play();

        while (dialogueAudioSource.isPlaying)
            yield return null;
    }

    private void StartAttackVoiceCycle()
    {
        StopAttackVoiceCycle();
        attackVoiceRoutine = StartCoroutine(AttackVoiceLoop());
    }

    private void StopAttackVoiceCycle()
    {
        if (attackVoiceRoutine != null)
        {
            StopCoroutine(attackVoiceRoutine);
            attackVoiceRoutine = null;
        }
    }

    private IEnumerator AttackVoiceLoop()
    {
        float initialDelay = Random.Range(attackVoiceIntervalRange.x, attackVoiceIntervalRange.y);
        yield return new WaitForSeconds(initialDelay);

        while (!isDead)
        {
            if (attackVoiceClips != null && attackVoiceClips.Length > 0)
            {
                AudioClip clip = attackVoiceClips[attackVoiceIndex % attackVoiceClips.Length];
                attackVoiceIndex++;

                yield return StartCoroutine(PlayQueuedDialogueClip(clip));
            }

            float wait = Random.Range(attackVoiceIntervalRange.x, attackVoiceIntervalRange.y);
            yield return new WaitForSeconds(wait);
        }
    }

    private IEnumerator PlayQueuedDialogueClip(AudioClip clip)
    {
        if (clip == null || dialogueAudioSource == null)
            yield break;

        while (dialogueAudioSource.isPlaying)
            yield return null;

        dialogueAudioSource.clip = clip;
        dialogueAudioSource.volume = 1f;
        dialogueAudioSource.Play();

        while (dialogueAudioSource.isPlaying)
            yield return null;
    }
    #endregion

    #region Attacks

    private IEnumerator Attack1ConeSweep()
    {
        isBusy = true;
        StopMoving();
        yield return StartCoroutine(FacePlayerOverTime(0.2f));

        Vector3 attackForward = GetFlatDirectionToPlayer();
        if (attackForward.sqrMagnitude < 0.001f)
            attackForward = transform.forward;

        GameObject cone = SpawnConeTelegraph(attackForward);
        yield return new WaitForSeconds(attack1TelegraphTime);

        animator.ResetTrigger(AnimAttack1);
        animator.SetTrigger(AnimAttack1);
        PlayAttack1TriggerSound();

        yield return new WaitForSeconds(attack1DamageStartDelay);
        yield return StartCoroutine(DealSweepingConeDamage(attackForward));

        yield return new WaitForSeconds(attack1ActiveTime);

        if (cone != null) Destroy(cone);

        yield return new WaitForSeconds(attack1Cooldown);

        isBusy = false;
    }
    private void PlayAttack1TriggerSound()
    {
        PlayOneShotAtPosition(attack1TriggerClip, transform.position);
    }

    private IEnumerator DealSweepingConeDamage(Vector3 attackForward)
    {
        if (player == null) yield break;

        bool alreadyHit = false;
        float halfAngle = attack1ConeAngle * 0.5f;

        for (int step = 0; step < attack1SweepSteps; step++)
        {
            float t = attack1SweepSteps <= 1 ? 1f : step / (float)(attack1SweepSteps - 1);
            float centerAngle = Mathf.Lerp(halfAngle, -halfAngle, t);
            float sectorWidth = Mathf.Max(attack1ConeAngle / 3f, 18f);

            if (!alreadyHit && IsPlayerInsideConeSector(attackForward, centerAngle, sectorWidth, attack1ConeLength))
            {
                alreadyHit = true;
                TryDamagePlayer(player.gameObject, scaledAttackDamage);
            }

            yield return new WaitForSeconds(attack1SweepDamageWindow / Mathf.Max(1, attack1SweepSteps));
        }
    }

    private bool IsPlayerInsideConeSector(Vector3 attackForward, float sectorCenterAngle, float sectorWidth, float radius)
    {
        if (player == null) return false;

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;

        float dist = toPlayer.magnitude;
        if (dist > radius || dist < 0.01f)
            return false;

        float playerAngle = Vector3.SignedAngle(attackForward, toPlayer.normalized, Vector3.up);
        return Mathf.Abs(Mathf.DeltaAngle(playerAngle, sectorCenterAngle)) <= sectorWidth * 0.5f;
    }

    // VERSION 1:
    // Telegraph columns 0,2,4 -> attack anim -> 3 detonations in those same columns
    // Then columns 1,3 -> attack anim -> 3 detonations in those same columns
    private IEnumerator Phase2Version1_Columns()
    {
        isBusy = true;
        try
        {
            StopMoving();
        }
        catch { isBusy = false; yield break; }

        yield return StartCoroutine(FacePlayerOverTime(0.2f));

        int[] firstSet = { 0, 2, 4 }; // far left, middle, far right
        int[] secondSet = { 1, 3 };   // middle left, middle right

        List<GameObject> telegraphsA = SpawnColumnTelegraphs(firstSet);
        yield return new WaitForSeconds(version1TelegraphDuration);

        animator.ResetTrigger(AnimAttack2);
        animator.SetTrigger(AnimAttack2);
        yield return StartCoroutine(DetonateColumnSet(firstSet, 3, version1BurstGap));

        DestroyTelegraphs(telegraphsA);

        yield return new WaitForSeconds(0.15f);

        List<GameObject> telegraphsB = SpawnColumnTelegraphs(secondSet);
        yield return new WaitForSeconds(version1TelegraphDuration);

        animator.ResetTrigger(AnimAttack2);
        animator.SetTrigger(AnimAttack2);
        yield return StartCoroutine(DetonateColumnSet(secondSet, 3, version1BurstGap));

        DestroyTelegraphs(telegraphsB);

        isBusy = false;
    }

    // VERSION 2:
    // Light columns sequentially L->R with 1 second gap
    // Then detonate sequentially L->R with 1 second gap
    // Wait 5s
    // Repeat R->L
    private IEnumerator Phase2Version2_Columns()
    {
        isBusy = true;
        StopMoving();
        yield return StartCoroutine(FacePlayerOverTime(0.2f));

        int[] leftToRight = { 0, 1, 2, 3, 4 };
        int[] rightToLeft = { 4, 3, 2, 1, 0 };

        yield return StartCoroutine(TelegraphColumnsSequential(leftToRight, version2LightGap));
        animator.ResetTrigger(AnimAttack2);
        animator.SetTrigger(AnimAttack2);
        yield return StartCoroutine(DetonateColumnsSequential(leftToRight, version2DetonationGap));

        yield return new WaitForSeconds(version2PauseBeforeReverse);

        yield return StartCoroutine(TelegraphColumnsSequential(rightToLeft, version2LightGap));
        animator.ResetTrigger(AnimAttack2);
        animator.SetTrigger(AnimAttack2);
        yield return StartCoroutine(DetonateColumnsSequential(rightToLeft, version2DetonationGap));

        isBusy = false;
    }

    #endregion

    #region Phase 2 Helpers

    private IEnumerator TelegraphColumnsSequential(int[] columnIndices, float gap)
    {
        List<GameObject> activeTelegraphs = new List<GameObject>();

        for (int i = 0; i < columnIndices.Length; i++)
        {
            GameObject tele = SpawnSingleColumnTelegraph(columnIndices[i]);
            activeTelegraphs.Add(tele);

            if (i < columnIndices.Length - 1)
                yield return new WaitForSeconds(gap);
        }

        // keep them alive until detonation starts
        for (int i = 0; i < activeTelegraphs.Count; i++)
        {
            if (activeTelegraphs[i] != null)
                spawnedTelegraphs.Add(activeTelegraphs[i]);
        }
    }

    private IEnumerator DetonateColumnsSequential(int[] columnIndices, float gap)
    {
        float columnWidth = roomWidth / 5f;

        foreach (int columnIndex in columnIndices)
        {
            Vector3 center = GetColumnCenter(columnIndex);

            PlayCannonShot(center);
            SpawnExplosionForColumn(columnIndex);

            DamageIfPlayerInsideRectangle(center, columnWidth, roomLength);

            DestroyColumnTelegraphByName("ColumnTelegraph_" + columnIndex);

            yield return new WaitForSeconds(gap);
        }
    }

    private IEnumerator DetonateColumnSet(int[] columnIndices, int detonationCount, float burstGap)
    {
        float columnWidth = roomWidth / 5f;

        for (int burst = 0; burst < detonationCount; burst++)
        {
            foreach (int columnIndex in columnIndices)
            {
                Vector3 center = GetColumnCenter(columnIndex);

                PlayCannonShot(center);
                SpawnExplosionForColumn(columnIndex);
                DamageIfPlayerInsideRectangle(center, columnWidth, roomLength);
            }

            yield return new WaitForSeconds(burstGap);
        }
    }

    private void SpawnExplosionForColumn(int columnIndex)
    {
        if (explosionPrefab == null || roomCenter == null) return;

        float columnWidth = roomWidth / 5f;
        float xMin = roomCenter.position.x - roomWidth * 0.5f;
        float x = xMin + (columnWidth * columnIndex) + columnWidth * 0.5f;

        float zMin = roomCenter.position.z - roomLength * 0.5f;
        float sectionLength = roomLength / 3f;

        for (int i = 0; i < 3; i++)
        {
            float z = zMin + sectionLength * i + sectionLength * 0.5f;
            Vector3 spawnPos = new Vector3(x, roomCenter.position.y, z);

            GameObject exp = Instantiate(explosionPrefab, spawnPos, Quaternion.identity);
            spawnedExplosions.Add(exp);
            Destroy(exp, 1.5f);
        }
    }

    private List<GameObject> SpawnColumnTelegraphs(int[] columnIndices)
    {
        List<GameObject> list = new List<GameObject>();

        foreach (int index in columnIndices)
            list.Add(SpawnSingleColumnTelegraph(index));

        return list;
    }

    private GameObject SpawnSingleColumnTelegraph(int columnIndex)
    {
        Vector3 center = GetColumnCenter(columnIndex);

        return SpawnRectangleTelegraph(
            center,
            roomWidth / 5f,
            roomLength,
            "ColumnTelegraph_" + columnIndex
        );
    }

    private void DestroyColumnTelegraphByName(string telegraphName)
    {
        for (int i = spawnedTelegraphs.Count - 1; i >= 0; i--)
        {
            if (spawnedTelegraphs[i] == null) continue;

            if (spawnedTelegraphs[i].name == telegraphName)
            {
                Destroy(spawnedTelegraphs[i]);
                spawnedTelegraphs.RemoveAt(i);
            }
        }
    }

    #endregion

    #region Telegraph Helpers

    private GameObject SpawnConeTelegraph(Vector3 forward)
    {
        Vector3 origin = transform.position + Vector3.up * telegraphYOffset;
        if (forward.sqrMagnitude < 0.001f)
            forward = transform.forward;

        float halfAngle = attack1ConeAngle * 0.5f;

        Vector3[] points = new Vector3[coneArcSegments + 2];
        points[0] = origin;

        for (int i = 0; i <= coneArcSegments; i++)
        {
            float t = i / (float)coneArcSegments;
            float angle = Mathf.Lerp(-halfAngle, halfAngle, t);
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * forward;
            points[i + 1] = origin + dir.normalized * attack1ConeLength;
        }

        return CreateFilledPolygonTelegraph("ConeTelegraph", points);
    }

    private GameObject SpawnRectangleTelegraph(Vector3 center, float width, float length, string name)
    {
        Vector3 baseCenter = center + Vector3.up * telegraphYOffset;

        float halfW = width * 0.5f;
        float halfL = length * 0.5f;

        Vector3[] rect =
        {
            baseCenter + new Vector3(-halfW, 0f, -halfL),
            baseCenter + new Vector3(-halfW, 0f,  halfL),
            baseCenter + new Vector3( halfW, 0f,  halfL),
            baseCenter + new Vector3( halfW, 0f, -halfL)
        };

        return CreateFilledPolygonTelegraph(name, rect);
    }

    private bool IsPlayerInsideRectangle(Vector3 center, float width, float length)
    {
        if (player == null) return false;

        Vector3 local = player.position - center;
        return Mathf.Abs(local.x) <= width * 0.5f && Mathf.Abs(local.z) <= length * 0.5f;
    }

    private void DamageIfPlayerInsideRectangle(Vector3 center, float width, float length)
    {
        if (player == null) return;

        if (IsPlayerInsideRectangle(center, width, length))
            TryDamagePlayer(player.gameObject, scaledAttackDamage);
    }

    private GameObject CreateFilledPolygonTelegraph(string name, Vector3[] worldPoints)
    {
        GameObject root = new GameObject(name);
        root.transform.position = Vector3.zero;

        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(root.transform, false);

        MeshFilter mf = fillObj.AddComponent<MeshFilter>();
        MeshRenderer mr = fillObj.AddComponent<MeshRenderer>();

        Mesh mesh = BuildFlatPolygonMesh(worldPoints);
        mf.mesh = mesh;
        mr.material = CreateRuntimeColorMaterial(telegraphFillColor);

        CreateLineRenderer(root.transform, "Outline", CloseLoop(worldPoints), telegraphOutlineColor);

        spawnedTelegraphs.Add(root);
        return root;
    }

    private Mesh BuildFlatPolygonMesh(Vector3[] worldPoints)
    {
        Mesh mesh = new Mesh();

        Vector3[] verts = new Vector3[worldPoints.Length];
        for (int i = 0; i < worldPoints.Length; i++)
            verts[i] = worldPoints[i];

        List<int> tris = new List<int>();
        for (int i = 1; i < worldPoints.Length - 1; i++)
        {
            tris.Add(0);
            tris.Add(i);
            tris.Add(i + 1);
        }

        Vector2[] uv = new Vector2[verts.Length];
        for (int i = 0; i < uv.Length; i++)
            uv[i] = new Vector2(verts[i].x, verts[i].z);

        mesh.vertices = verts;
        mesh.triangles = tris.ToArray();
        mesh.uv = uv;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    private LineRenderer CreateLineRenderer(Transform parent, string objName, Vector3[] points, Color color)
    {
        GameObject lineObj = new GameObject(objName);
        lineObj.transform.SetParent(parent);

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop = false;
        lr.positionCount = points.Length;
        lr.SetPositions(points);

        lr.startWidth = telegraphLineWidth;
        lr.endWidth = telegraphLineWidth;

        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = color;
        lr.material = mat;

        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.alignment = LineAlignment.View;
        lr.textureMode = LineTextureMode.Stretch;
        lr.numCornerVertices = 4;
        lr.numCapVertices = 4;

        return lr;
    }

    private Material CreateRuntimeColorMaterial(Color color)
    {
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = color;
        return mat;
    }

    private Vector3[] CloseLoop(Vector3[] points)
    {
        Vector3[] closed = new Vector3[points.Length + 1];
        for (int i = 0; i < points.Length; i++)
            closed[i] = points[i];
        closed[points.Length] = points[0];
        return closed;
    }

    #endregion

    #region Phase 3 / 4 Spawning

    private void StartPhase4SoldierSpawning()
    {
        if (phase4SoldierSpawnRoutine != null)
            StopCoroutine(phase4SoldierSpawnRoutine);

        phase4SoldierSpawnRoutine = StartCoroutine(Phase4SoldierSpawnLoop());
    }

    private IEnumerator Phase4SoldierSpawnLoop()
    {
        // Spawn one group immediately when Phase 4 starts
        SpawnPhase4Soldiers();

        while (currentPhase == BossPhase.Phase4 && !isDead)
        {
            float wait = Random.Range(phase4MinSoldierSpawnInterval, phase4MaxSoldierSpawnInterval);
            yield return new WaitForSeconds(wait);

            if (currentPhase != BossPhase.Phase4 || isDead)
                yield break;

            SpawnPhase4Soldiers();
        }
    }

    private void SpawnPhase4Soldiers()
    {
        if (skeletonPrefab == null)
        {
            Debug.LogWarning("[GreedAI] skeletonPrefab is not assigned. Cannot spawn Phase 4 soldiers.");
            return;
        }

        int count = Random.Range(phase4MinSoldiersPerSpawn, phase4MaxSoldiersPerSpawn + 1);
        Vector3 basePos = roomCenter != null ? roomCenter.position : transform.position;

        for (int i = 0; i < count; i++)
        {
            if (TryGetRandomNavMeshSpawnPoint(basePos, out Vector3 spawnPos))
            {
                Quaternion spawnRot = Quaternion.identity;

                if (player != null)
                {
                    Vector3 dir = player.position - spawnPos;
                    dir.y = 0f;

                    if (dir.sqrMagnitude > 0.001f)
                        spawnRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
                }

                GameObject soldier = Instantiate(skeletonPrefab, spawnPos, spawnRot);
                spawnedSkeletons.Add(soldier);
            }
            else
            {
                Debug.LogWarning("[GreedAI] Could not find a valid NavMesh point for soldier spawn.");
            }
        }
    }

    private bool TryGetRandomNavMeshSpawnPoint(Vector3 center, out Vector3 result)
    {
        for (int attempt = 0; attempt < 20; attempt++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * navmeshSpawnRadius;
            Vector3 randomPoint = center + new Vector3(randomCircle.x, 0f, randomCircle.y);

            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, navmeshSampleRadius, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
        }

        if (NavMesh.SamplePosition(center, out NavMeshHit fallbackHit, navmeshSampleRadius, NavMesh.AllAreas))
        {
            result = fallbackHit.position;
            return true;
        }

        result = center;
        return false;
    }
    #endregion

    #region Movement / Teleport

    private bool HasValidNavMeshAgent()
    {
        return agent != null && agent.enabled && agent.isOnNavMesh;
    }

    private void MoveTowardsPlayer(float minDistance)
    {
        if (player == null || !HasValidNavMeshAgent()) return;

        Vector3 toBoss = transform.position - player.position;
        toBoss.y = 0f;
        float dist = toBoss.magnitude;

        if (dist <= Mathf.Max(minDistance, 0.1f))
        {
            StopMoving();
            return;
        }

        Vector3 desired = player.position;
        if (minDistance > 0f)
        {
            Vector3 dir = toBoss.normalized;
            desired = player.position + dir * minDistance;
        }

        agent.isStopped = false;
        agent.SetDestination(desired);
    }

    private void MaintainPhase4Distance()
    {
        if (currentPhase != BossPhase.Phase4 || player == null || isBusy) return;

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist > attackRange)
        {
            MoveTowardsPlayer(phase4MinDistance);
        }
        else if (dist < phase4MinDistance)
        {
            Vector3 away = (transform.position - player.position).normalized;
            Vector3 target = player.position + away * phase4MinDistance;

            if (HasValidNavMeshAgent())
            {
                agent.isStopped = false;
                agent.SetDestination(target);
            }
        }
        else
        {
            StopMoving();
            FacePlayerImmediate();
        }
    }

    private void StopMoving()
    {
        if (!HasValidNavMeshAgent()) return;

        agent.isStopped = true;
        agent.ResetPath();
    }

    private IEnumerator MoveToPoint(Vector3 point, float stoppingDistance)
    {
        if (!HasValidNavMeshAgent())
        {
            transform.position = point;
            yield break;
        }

        NavMeshHit hit;
        Vector3 target = point;

        if (NavMesh.SamplePosition(point, out hit, 5f, NavMesh.AllAreas))
            target = hit.position;

        agent.isStopped = false;
        agent.stoppingDistance = stoppingDistance;
        agent.SetDestination(target);

        float timer = 0f;
        float maxMoveTime = 4f;

        while (!isDead && timer < maxMoveTime)
        {
            float dist = Vector3.Distance(transform.position, target);

            if (dist <= stoppingDistance + 0.5f)
                break;

            if (!agent.pathPending && agent.pathStatus == NavMeshPathStatus.PathInvalid)
                break;

            timer += Time.deltaTime;
            yield return null;
        }

        StopMoving();
    }

    private IEnumerator FacePlayerOverTime(float duration)
    {
        if (player == null)
            yield break;

        float t = 0f;
        Quaternion start = transform.rotation;
        Quaternion end = Quaternion.LookRotation(GetFlatDirectionToPlayer(), Vector3.up);

        while (t < duration)
        {
            t += Time.deltaTime;
            transform.rotation = Quaternion.Slerp(start, end, t / duration);
            yield return null;
        }

        transform.rotation = end;
    }

    private void FacePlayerImmediate()
    {
        if (player == null) return;

        Vector3 dir = GetFlatDirectionToPlayer();
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
    }

    private Vector3 GetFlatDirectionToPlayer()
    {
        if (player == null) return transform.forward;

        Vector3 dir = player.position - transform.position;
        dir.y = 0f;
        return dir.sqrMagnitude < 0.001f ? transform.forward : dir.normalized;
    }

    #endregion

    #region Audio / Lights / UI


    private void UpdateWalkAudio()
    {
        if (walkAudioSource == null || walkingClip == null) return;

        bool shouldWalkPlay = !isDead && !isBusy && HasValidNavMeshAgent() && agent.velocity.magnitude > 0.1f;

        if (shouldWalkPlay)
        {
            if (!walkAudioSource.isPlaying)
                walkAudioSource.Play();
        }
        else
        {
            if (walkAudioSource.isPlaying)
                walkAudioSource.Stop();
        }
    }

    private IEnumerator RandomVoiceLoop()
    {
        while (!isDead)
        {
            float wait = Random.Range(randomSfxIntervalRange.x, randomSfxIntervalRange.y);
            yield return new WaitForSeconds(wait);

            if (randomSfxClips != null && randomSfxClips.Length > 0)
            {
                AudioClip clip = randomSfxClips[Random.Range(0, randomSfxClips.Length)];
                PlayOneShotAtPosition(clip, transform.position);
            }
        }
    }
    private void PlayOneShotAtPosition(AudioClip clip, Vector3 pos)
    {
        if (clip == null) return;

        GameObject temp = new GameObject("OneShot_BossSFX");
        temp.transform.position = pos;

        AudioSource src = temp.AddComponent<AudioSource>();
        src.clip = clip;
        src.outputAudioMixerGroup = sfxMixer;

        src.playOnAwake = false;
        src.loop = false;
        src.spatialBlend = 1f;
        src.rolloffMode = AudioRolloffMode.Logarithmic;
        src.minDistance = oneShotMinDistance;
        src.maxDistance = oneShotMaxDistance;
        src.dopplerLevel = 0f;
        src.volume = 1f;

        src.Play();
        Destroy(temp, clip.length + 0.25f);
    }

    private void PlayCannonShot(Vector3 pos)
    {
        PlayOneShotAtPosition(cannonFireClip, pos);
    }

    private void TryDamagePlayer(GameObject playerObj, float damage)
    {
        if (playerObj == null || damage <= 0f) return;
        playerObj.SendMessage("TakeDamage", Mathf.RoundToInt(damage), SendMessageOptions.DontRequireReceiver);
    }

    private void UpdateBossUI()
    {
        if (bossHealthBarFill != null)
            bossHealthBarFill.fillAmount = maxHP <= 0f ? 0f : currentHP / maxHP;

        if (bossHealthText != null)
            bossHealthText.text = $"{Mathf.CeilToInt(currentHP)} / {Mathf.CeilToInt(maxHP)}";
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;
        currentPhase = BossPhase.Dead;
        requestedPhase = BossPhase.Dead;
        scaledAttackDamage = 0f;

        if (phase4SoldierSpawnRoutine != null)
        {
            StopCoroutine(phase4SoldierSpawnRoutine);
            phase4SoldierSpawnRoutine = null;
        }

        StopAllCoroutines();
        StopMoving();
        StopAttackVoiceCycle();
        ClearAllSpawnedObjects();

        if (loopAudioSource != null) loopAudioSource.Stop();
        if (walkAudioSource != null) walkAudioSource.Stop();
        if (randomVoiceRoutine != null) StopCoroutine(randomVoiceRoutine);

        StartCoroutine(HandleDeathSequence());


        Collider[] cols = GetComponentsInChildren<Collider>();
        foreach (Collider col in cols)
        {
            col.enabled = false;
        }

    }

    private IEnumerator HandleDeathSequence()
    {
        if (dialogueAudioSource != null && dialogueAudioSource.isPlaying)
            yield return StartCoroutine(FadeOutAudio(dialogueAudioSource, deathFadeOutDuration));

        animator.SetBool(AnimDeath, true);
        animator.SetTrigger(AnimDeath);
        animator.SetBool(AnimRunning, false);

        if (deathDialogueClip != null && dialogueAudioSource != null)
        {
            dialogueAudioSource.clip = deathDialogueClip;
            dialogueAudioSource.volume = 1f;
            dialogueAudioSource.Play();
        }

        if (arenaController != null)
        {
            arenaController.OnBossDied();
        }
    }

    private IEnumerator FadeOutAudio(AudioSource source, float duration)
    {
        if (source == null) yield break;

        float startVolume = source.volume;
        float t = 0f;

        while (t < duration && source != null)
        {
            t += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, t / duration);
            yield return null;
        }

        if (source != null)
        {
            source.Stop();
            source.volume = startVolume;
        }
    }


    private void RecalculateScaledStats()
    {
        int steps = GetScalingSteps();
        maxHP = baseMaxHP * (1f + 0.05f * steps);
        scaledAttackDamage = baseAttackDamage * (1f + 0.10f * steps);
    }

    private int GetScalingSteps()
    {
        return Mathf.Max(0, (currentFloor / 5) - 1);
    }

    #endregion

    #region Utility

    private Vector3 GetColumnCenter(int columnIndex)
    {
        FindRoomCenterIfNeeded();

        if (roomCenter == null)
        {
            Debug.LogError("[GreedAI] roomCenter is not assigned! Phase 2 columns cannot work.");
            return transform.position;
        }
        float columnWidth = roomWidth / 5f;
        float xMin = roomCenter.position.x - roomWidth * 0.5f;
        float x = xMin + (columnWidth * columnIndex) + columnWidth * 0.5f;
        return new Vector3(x, roomCenter.position.y, roomCenter.position.z);
    }
    private void FindRoomCenterIfNeeded()
    {
        if (roomCenter != null) return;

        GameObject found = GameObject.Find("RoomCenter");

        if (found != null)
        {
            roomCenter = found.transform;
        }
        else
        {
            Debug.LogError("[GreedAI] Could not find RoomCenter in scene.");
        }
    }

    private Vector3 SampleNavmeshPoint(Vector3 desired, float radius, Vector3 fallback)
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(desired, out hit, radius, NavMesh.AllAreas))
            return hit.position;

        if (NavMesh.SamplePosition(fallback, out hit, radius, NavMesh.AllAreas))
            return hit.position;

        return desired;
    }

    private void DestroyTelegraphs(List<GameObject> telegraphs)
    {
        foreach (GameObject g in telegraphs)
        {
            if (g != null) Destroy(g);
        }
    }

    private void ClearAllSpawnedObjects()
    {
        DestroyAllInList(spawnedTelegraphs);
        DestroyAllInList(spawnedExplosions);
        DestroyAllInList(spawnedSkeletons);
    }

    private void DestroyAllInList(List<GameObject> list)
    {
        foreach (GameObject g in list)
        {
            if (g != null) Destroy(g);
        }
        list.Clear();
    }

    private void FindBackgroundMusicSourceIfNeeded()
    {
        if (musicAudioSource != null)
        {
            return;
        }

        GameObject bg = GameObject.Find("BackgroundAudio");

        if (musicAudioSource == null)
        {
            GameObject tagged = GameObject.FindGameObjectWithTag("Music");
        }

    }
    private void SetupAudioSources()
    {
        loopAudioSource = gameObject.AddComponent<AudioSource>();
        loopAudioSource.clip = bossLoopClip;
        loopAudioSource.loop = true;
        loopAudioSource.playOnAwake = false;
        loopAudioSource.spatialBlend = 1f;
        loopAudioSource.minDistance = 5f;
        loopAudioSource.maxDistance = 30f;
        loopAudioSource.volume = 1f;
        loopAudioSource.outputAudioMixerGroup = sfxMixer;

        if (bossLoopClip != null)
            loopAudioSource.Play();

        walkAudioSource = gameObject.AddComponent<AudioSource>();
        walkAudioSource.clip = walkingClip;
        walkAudioSource.loop = true;
        walkAudioSource.playOnAwake = false;
        walkAudioSource.spatialBlend = 1f;
        walkAudioSource.minDistance = 4f;
        walkAudioSource.maxDistance = 20f;
        walkAudioSource.volume = 1f;
        walkAudioSource.outputAudioMixerGroup = sfxMixer;

        if (narrationMixer == null)
            Debug.LogError("[GreedAI] narrationMixer is not assigned in the Inspector! Dialogue will bypass the mixer and the volume slider will have no effect.");

        dialogueAudioSource = gameObject.AddComponent<AudioSource>();
        dialogueAudioSource.playOnAwake = false;
        dialogueAudioSource.loop = false;
        dialogueAudioSource.spatialBlend = 0.2f;
        dialogueAudioSource.minDistance = 8f;
        dialogueAudioSource.maxDistance = 60f;
        dialogueAudioSource.volume = 1f;
        dialogueAudioSource.outputAudioMixerGroup = narrationMixer;
    }

    #endregion

    #region Gizmos

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, wakeRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (roomCenter != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(roomCenter.position, new Vector3(roomWidth, 0.1f, roomLength));
        }
    }

    #endregion
}