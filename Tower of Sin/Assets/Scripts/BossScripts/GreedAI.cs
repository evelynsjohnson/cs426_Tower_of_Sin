using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using TMPro;

public class GreedAI : MonoBehaviour
{
    public enum BossPhase
    {
        Dormant,
        Phase1,
        Phase2,
        Phase3,
        Phase4,
        Dead
    }

    [SerializeField] private Animator animator;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform player;
    [SerializeField] private Transform spawnPointOverride;
    [SerializeField] private Transform ledgePointOverride;

    [SerializeField] private float wakeRange = 20f;
    [SerializeField] private float attackRange = 7f;
    [SerializeField] private float phase4MinDistance = 7f;
    [SerializeField] private float faceSpeed = 10f;

    [SerializeField] private float baseMaxHP = 500f;
    [SerializeField] private float baseAttackDamage = 25f;

    [Header("Attack 1")]
    [SerializeField] private float attack1TelegraphTime = 1.25f;
    [SerializeField] private float attack1ActiveTime = 0.4f;
    [SerializeField] private float attack1Cooldown = 1.4f;
    [SerializeField] private float attack1ConeAngle = 75f;
    [SerializeField] private float attack1ConeLength = 8f;
    [SerializeField] private LayerMask playerLayer;

    [Header("Phase 2")]
    [SerializeField] private Transform roomCenter;
    [SerializeField] private float roomWidth = 40f;
    [SerializeField] private float roomLength = 40f;
    [SerializeField] private float telegraphHeight = 0.05f;

    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private float rowTelegraphDuration = 1.25f;
    [SerializeField] private float columnTelegraphDuration = 1f;
    [SerializeField] private float detonationRadius = 2.25f;
    [SerializeField] private float detonationDelayBetweenBursts = 0.2f;
    [SerializeField] private float detonationDelayBetweenColumns = 1f;
    [SerializeField] private float phase2LoopPause = 1.25f;

    [Header("Phase 3")]
    [SerializeField] private GameObject tentaclePrefab;
    [SerializeField] private int phase3TentacleCount = 5;
    [SerializeField] private float tentacleSpawnRadiusNearPlayer = 4f;
    [SerializeField] private float tentacleCornerInset = 3f;

    [Header("Phase 4")]
    [SerializeField] private GameObject skeletonPrefab;
    [SerializeField] private int skeletonSpawnCount = 6;
    [SerializeField] private float navmeshSpawnRadius = 18f;

    [SerializeField] private Transform bossSpawnPoint;
    [SerializeField] private Transform bossSpawnPointLedge;

    [SerializeField] private Image bossHealthBarFill;
    [SerializeField] private TMP_Text bossHealthText;
    [SerializeField] private GameObject bossHealthUIRoot;

    [Header("Runtime Telegraph Drawing")]
    [SerializeField] private Material telegraphMaterial;
    [SerializeField] private float telegraphLineWidth = 0.15f;
    [SerializeField] private float telegraphYOffset = 0.05f;
    [SerializeField] private int coneArcSegments = 20;

    [SerializeField] private bool drawGizmos = true;

    private static readonly int AnimAttack1 = Animator.StringToHash("attack1Sweep");
    private static readonly int AnimAttack2 = Animator.StringToHash("attack2Slam");
    private static readonly int AnimSpawned = Animator.StringToHash("hasSpawned");
    private static readonly int AnimRunning = Animator.StringToHash("isRunning");
    private static readonly int AnimDeath = Animator.StringToHash("death");
    private static readonly int AnimHorn = Animator.StringToHash("blowHorn");

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
    private bool phase2PatternToggle = false; // false = version1, true = version2
    private bool didEnterPhase2 = false;
    private bool didEnterPhase3 = false;
    private bool didEnterPhase4 = false;

    private Coroutine brainRoutine;
    private Coroutine attackRoutine;

    private readonly List<GameObject> spawnedTelegraphs = new List<GameObject>();
    private readonly List<GameObject> spawnedExplosions = new List<GameObject>();
    private readonly List<GameObject> spawnedTentacles = new List<GameObject>();
    private readonly List<GameObject> spawnedSkeletons = new List<GameObject>();

    public void SetSceneReferences(
        Transform playerTransform,
        Transform spawnPoint,
        Transform ledgePoint,
        Transform roomCenterTransform,
        Image healthFill,
        TMPro.TMP_Text healthText,
        GameObject healthUIRootObject
    )
    {
        player = playerTransform;
        bossSpawnPoint = spawnPoint;
        bossSpawnPointLedge = ledgePoint;
        roomCenter = roomCenterTransform;
        bossHealthBarFill = healthFill;
        bossHealthText = healthText;
        bossHealthUIRoot = healthUIRootObject;
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

        if (bossSpawnPoint != null) originalSpawnPoint = bossSpawnPoint.position;
    }

    private void Start()
    {
        RecalculateScaledStats();
        currentHP = maxHP;
        UpdateBossUI();

        if (bossHealthUIRoot != null)
            bossHealthUIRoot.SetActive(true);

        if (brainRoutine != null)
            StopCoroutine(brainRoutine);

        brainRoutine = StartCoroutine(BossBrain());
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

        animator.SetBool(AnimRunning, agent != null && agent.enabled && agent.velocity.magnitude > 0.1f);

        HandlePhaseRequestsByHealth();
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
        UpdateBossUI();
    }

    public void TakeDamage(float amount)
    {
        if (isDead || isInvulnerable) return;

        currentHP -= amount;
        currentHP = Mathf.Max(0f, currentHP);
        UpdateBossUI();

        if (currentHP <= 0f)
        {
            Die();
        }
    }

    public void NotifyTentacleDied(GameObject tentacle)
    {
        if (tentacle != null)
            spawnedTentacles.Remove(tentacle);

        if (!isDead)
        {
            float hpLoss = maxHP * 0.05f;
            currentHP = Mathf.Max(0f, currentHP - hpLoss);
            UpdateBossUI();

            if (currentHP <= 0f)
            {
                Die();
                return;
            }
        }

        if (didEnterPhase3 && spawnedTentacles.Count == 0)
        {
            StartCoroutine(EndPhase3Invulnerability());
        }
    }

    #endregion

    #region Core Brain

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

                case BossPhase.Phase3:
                    yield return HandlePhase3();
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
                yield return StartCoroutine(PlaySpawnSequence());
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
            {
                yield break;
            }

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
                    attackRoutine = StartCoroutine(Attack1ConeSweep());
                    yield return attackRoutine;
                }
            }

            yield return null;
        }
    }

    private IEnumerator HandlePhase2()
    {
        if (!isTransitioning)
        {
            yield return StartCoroutine(TransitionToPhase2());
        }

        while (currentPhase == BossPhase.Phase2 && !isDead)
        {
            if (requestedPhase == BossPhase.Phase3 && !isBusy)
            {
                currentPhase = BossPhase.Phase3;
                yield break;
            }

            if (requestedPhase == BossPhase.Phase4 && !isBusy)
            {
                currentPhase = BossPhase.Phase4;
                yield break;
            }

            if (!isBusy)
            {
                if (!phase2PatternToggle)
                    attackRoutine = StartCoroutine(Phase2Version1_Rows());
                else
                    attackRoutine = StartCoroutine(Phase2Version2_Columns());

                phase2PatternToggle = !phase2PatternToggle;
                yield return attackRoutine;
                yield return new WaitForSeconds(phase2LoopPause);
            }

            yield return null;
        }
    }

    private IEnumerator HandlePhase3()
    {
        if (!isTransitioning)
        {
            yield return StartCoroutine(TransitionToPhase3());
        }

        // In phase 3, boss continues using phase 2 attack patterns while invulnerable.
        while (currentPhase == BossPhase.Phase3 && !isDead)
        {
            if (requestedPhase == BossPhase.Phase4 && !isBusy)
            {
                currentPhase = BossPhase.Phase4;
                yield break;
            }

            if (!isBusy)
            {
                if (!phase2PatternToggle)
                    attackRoutine = StartCoroutine(Phase2Version1_Rows());
                else
                    attackRoutine = StartCoroutine(Phase2Version2_Columns());

                phase2PatternToggle = !phase2PatternToggle;
                yield return attackRoutine;
                yield return new WaitForSeconds(phase2LoopPause);
            }

            yield return null;
        }
    }

    private IEnumerator HandlePhase4()
    {
        if (!isTransitioning)
        {
            yield return StartCoroutine(TransitionToPhase4());
        }

        int cycleIndex = 0;

        while (currentPhase == BossPhase.Phase4 && !isDead)
        {
            if (!isBusy)
            {
                switch (cycleIndex)
                {
                    case 0:
                        yield return StartCoroutine(Attack1ConeSweep());
                        break;
                    case 1:
                        yield return StartCoroutine(Phase2Version1_Rows());
                        break;
                    case 2:
                        yield return StartCoroutine(Phase2Version2_Columns());
                        break;
                }

                cycleIndex = (cycleIndex + 1) % 3;
            }
            else
            {
                MaintainPhase4Distance();
            }

            yield return null;
        }
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

        // Let spawn anim play.
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

        if (!didEnterPhase3 && hpPercent <= 0.50f)
        {
            didEnterPhase3 = true;
            requestedPhase = BossPhase.Phase3;
        }

        if (!didEnterPhase4 && hpPercent <= 0.25f)
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

        StopMoving();

        Vector3 target = bossSpawnPoint != null ? bossSpawnPoint.position : originalSpawnPoint;
        yield return StartCoroutine(MoveToPoint(target, 0.5f));

        StopMoving();
        yield return StartCoroutine(FacePlayerOverTime(0.35f));

        isBusy = false;
        isTransitioning = false;
    }

    private IEnumerator TransitionToPhase3()
    {
        isTransitioning = true;
        isBusy = true;
        isInvulnerable = true;

        StopMoving();

        Vector3 ledgeTarget = bossSpawnPointLedge != null
            ? bossSpawnPointLedge.position
            : (ledgePointOverride != null ? ledgePointOverride.position : transform.position);

        if (agent != null && agent.enabled)
            agent.Warp(ledgeTarget);
        else
            transform.position = ledgeTarget;

        yield return StartCoroutine(FacePlayerOverTime(0.25f));

        SpawnPhase3Tentacles();

        isBusy = false;
        isTransitioning = false;
    }

    private IEnumerator EndPhase3Invulnerability()
    {
        isBusy = true;

        Vector3 target = bossSpawnPoint != null ? bossSpawnPoint.position : originalSpawnPoint;

        if (agent != null && agent.enabled)
            agent.Warp(target);
        else
            transform.position = target;

        yield return StartCoroutine(FacePlayerOverTime(0.25f));

        isInvulnerable = false;
        isBusy = false;
    }

    private IEnumerator TransitionToPhase4()
    {
        isTransitioning = true;
        isBusy = true;

        SpawnPhase4Skeletons();

        yield return new WaitForSeconds(0.5f);

        isBusy = false;
        isTransitioning = false;
    }

    #endregion

    #region Attacks

    private IEnumerator Attack1ConeSweep()
    {
        isBusy = true;
        StopMoving();
        yield return StartCoroutine(FacePlayerOverTime(0.2f));

        GameObject cone = SpawnConeTelegraph();
        yield return new WaitForSeconds(attack1TelegraphTime);

        animator.SetTrigger(AnimAttack1);

        DealConeDamageToPlayer();

        yield return new WaitForSeconds(attack1ActiveTime);

        if (cone != null) Destroy(cone);

        yield return new WaitForSeconds(attack1Cooldown);

        isBusy = false;
    }

    private IEnumerator Phase2Version1_Rows()
    {
        isBusy = true;
        StopMoving();
        yield return StartCoroutine(FacePlayerOverTime(0.2f));

        animator.SetTrigger(AnimAttack2);

        // first set: left, middle, right = rows 0,2,4
        int[] setA = { 0, 2, 4 };
        int[] setB = { 1, 3 };

        List<GameObject> telegraphsA = SpawnRowTelegraphs(setA);
        yield return new WaitForSeconds(rowTelegraphDuration);
        yield return StartCoroutine(DetonateRows(setA));
        DestroyTelegraphs(telegraphsA);

        List<GameObject> telegraphsB = SpawnRowTelegraphs(setB);
        yield return new WaitForSeconds(rowTelegraphDuration);
        yield return StartCoroutine(DetonateRows(setB));
        DestroyTelegraphs(telegraphsB);

        isBusy = false;
    }

    private IEnumerator Phase2Version2_Columns()
    {
        isBusy = true;
        StopMoving();
        yield return StartCoroutine(FacePlayerOverTime(0.2f));

        // Left > Right
        for (int col = 0; col < 5; col++)
        {
            GameObject tele = SpawnColumnTelegraph(col);
            if (col == 0)
                animator.SetTrigger(AnimAttack2);

            yield return new WaitForSeconds(columnTelegraphDuration);
            yield return StartCoroutine(DetonateColumn(col));
            if (tele != null) Destroy(tele);
            yield return new WaitForSeconds(detonationDelayBetweenColumns);
        }

        yield return new WaitForSeconds(5f);

        // Right > Left
        for (int col = 4; col >= 0; col--)
        {
            GameObject tele = SpawnColumnTelegraph(col);
            if (col == 4)
                animator.SetTrigger(AnimAttack2);

            yield return new WaitForSeconds(columnTelegraphDuration);
            yield return StartCoroutine(DetonateColumn(col));
            if (tele != null) Destroy(tele);
            yield return new WaitForSeconds(detonationDelayBetweenColumns);
        }

        isBusy = false;
    }

    #endregion

    #region Attack Helpers

    private GameObject SpawnConeTelegraph()
    {
        GameObject telegraphRoot = new GameObject("ConeTelegraph");
        telegraphRoot.transform.position = transform.position + Vector3.up * telegraphYOffset;

        Vector3 forward = GetFlatDirectionToPlayer();
        if (forward.sqrMagnitude < 0.001f)
            forward = transform.forward;

        float halfAngle = attack1ConeAngle * 0.5f;
        Vector3 leftDir = Quaternion.Euler(0f, -halfAngle, 0f) * forward;
        Vector3 rightDir = Quaternion.Euler(0f, halfAngle, 0f) * forward;

        Vector3 origin = telegraphRoot.transform.position;
        Vector3 leftPoint = origin + leftDir.normalized * attack1ConeLength;
        Vector3 rightPoint = origin + rightDir.normalized * attack1ConeLength;

        CreateLineRenderer(
            telegraphRoot.transform,
            "ConeLeft",
            new Vector3[] { origin, leftPoint }
        );

        CreateLineRenderer(
            telegraphRoot.transform,
            "ConeRight",
            new Vector3[] { origin, rightPoint }
        );

        Vector3[] arcPoints = new Vector3[coneArcSegments + 1];
        for (int i = 0; i <= coneArcSegments; i++)
        {
            float t = i / (float)coneArcSegments;
            float angle = Mathf.Lerp(-halfAngle, halfAngle, t);
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * forward;
            arcPoints[i] = origin + dir.normalized * attack1ConeLength;
        }

        CreateLineRenderer(
            telegraphRoot.transform,
            "ConeArc",
            arcPoints
        );

        spawnedTelegraphs.Add(telegraphRoot);
        return telegraphRoot;
    }
    private GameObject SpawnRectangleTelegraph(Vector3 center, float width, float length, string name)
    {
        GameObject telegraphRoot = new GameObject(name);
        telegraphRoot.transform.position = center + Vector3.up * telegraphYOffset;

        float halfW = width * 0.5f;
        float halfL = length * 0.5f;

        Vector3 p1 = telegraphRoot.transform.position + new Vector3(-halfW, 0f, -halfL);
        Vector3 p2 = telegraphRoot.transform.position + new Vector3(-halfW, 0f, halfL);
        Vector3 p3 = telegraphRoot.transform.position + new Vector3(halfW, 0f, halfL);
        Vector3 p4 = telegraphRoot.transform.position + new Vector3(halfW, 0f, -halfL);

        CreateLineRenderer(
            telegraphRoot.transform,
            "RectangleOutline",
            new Vector3[] { p1, p2, p3, p4, p1 }
        );

        spawnedTelegraphs.Add(telegraphRoot);
        return telegraphRoot;
    }

    private LineRenderer CreateLineRenderer(Transform parent, string objName, Vector3[] points)
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

        lr.material = telegraphMaterial;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.alignment = LineAlignment.View;
        lr.textureMode = LineTextureMode.Stretch;
        lr.numCornerVertices = 4;
        lr.numCapVertices = 4;

        return lr;
    }


    private void DealConeDamageToPlayer()
    {
        if (player == null) return;

        Vector3 toPlayer = player.position - transform.position;
        Vector3 flat = new Vector3(toPlayer.x, 0f, toPlayer.z);

        if (flat.magnitude > attack1ConeLength)
            return;

        float angle = Vector3.Angle(transform.forward, flat.normalized);
        if (angle > attack1ConeAngle * 0.5f)
            return;

        TryDamagePlayer(player.gameObject, scaledAttackDamage);
    }

    private List<GameObject> SpawnRowTelegraphs(int[] rowIndices)
    {
        List<GameObject> list = new List<GameObject>();

        foreach (int row in rowIndices)
        {
            Vector3 center = GetRowCenter(row);
            GameObject tele = SpawnRectangleTelegraph(
                center,
                roomWidth,
                roomLength / 5f,
                "RowTelegraph_" + row
            );

            list.Add(tele);
        }

        return list;
    }

    private GameObject SpawnColumnTelegraph(int columnIndex)
    {
        Vector3 center = GetColumnCenter(columnIndex);

        return SpawnRectangleTelegraph(
            center,
            roomWidth / 5f,
            roomLength,
            "ColumnTelegraph_" + columnIndex
        );
    }

    private IEnumerator DetonateRows(int[] rowIndices)
    {
        foreach (int row in rowIndices)
        {
            Vector3 left = GetRowBurstPoint(row, 0);
            Vector3 mid = GetRowBurstPoint(row, 1);
            Vector3 right = GetRowBurstPoint(row, 2);

            SpawnExplosionAndDamage(left);
            SpawnExplosionAndDamage(mid);
            SpawnExplosionAndDamage(right);

            yield return new WaitForSeconds(detonationDelayBetweenBursts);

            SpawnExplosionAndDamage(left);
            SpawnExplosionAndDamage(mid);
            SpawnExplosionAndDamage(right);

            yield return new WaitForSeconds(detonationDelayBetweenBursts);

            SpawnExplosionAndDamage(left);
            SpawnExplosionAndDamage(mid);
            SpawnExplosionAndDamage(right);

            yield return new WaitForSeconds(detonationDelayBetweenBursts);
        }
    }

    private IEnumerator DetonateColumn(int columnIndex)
    {
        for (int row = 0; row < 5; row++)
        {
            Vector3 p = GetColumnCellCenter(columnIndex, row);
            SpawnExplosionAndDamage(p);
        }

        yield return null;
    }

    private void SpawnExplosionAndDamage(Vector3 position)
    {
        if (explosionPrefab != null)
        {
            GameObject exp = Instantiate(explosionPrefab, position, Quaternion.identity);
            spawnedExplosions.Add(exp);
            Destroy(exp, 4f);
        }

        if (player == null) return;

        float dist = Vector3.Distance(
            new Vector3(player.position.x, 0f, player.position.z),
            new Vector3(position.x, 0f, position.z)
        );

        if (dist <= detonationRadius)
        {
            TryDamagePlayer(player.gameObject, scaledAttackDamage);
        }
    }

    #endregion

    #region Phase 3 / Phase 4 Spawning

    private void SpawnPhase3Tentacles()
    {
        ClearDeadRefs(spawnedTentacles);

        if (tentaclePrefab == null || roomCenter == null) return;

        List<Vector3> positions = new List<Vector3>();

        float halfW = roomWidth * 0.5f - tentacleCornerInset;
        float halfL = roomLength * 0.5f - tentacleCornerInset;
        Vector3 c = roomCenter.position;

        positions.Add(new Vector3(c.x - halfW, c.y, c.z - halfL));
        positions.Add(new Vector3(c.x - halfW, c.y, c.z + halfL));
        positions.Add(new Vector3(c.x + halfW, c.y, c.z - halfL));
        positions.Add(new Vector3(c.x + halfW, c.y, c.z + halfL));

        if (player != null)
        {
            Vector3 nearPlayer = player.position + Random.insideUnitSphere * tentacleSpawnRadiusNearPlayer;
            nearPlayer.y = c.y;
            positions.Add(nearPlayer);
        }

        int count = Mathf.Min(phase3TentacleCount, positions.Count);
        for (int i = 0; i < count; i++)
        {
            Vector3 valid = SampleNavmeshPoint(positions[i], 5f, positions[i]);
            GameObject t = Instantiate(tentaclePrefab, valid, Quaternion.identity);
            spawnedTentacles.Add(t);

            TentacleBossUnit tentacleUnit = t.GetComponent<TentacleBossUnit>();
            if (tentacleUnit != null)
            {
                float tentacleHP = 100f * (1f + 0.05f * GetScalingSteps());
                tentacleUnit.Initialize(this, tentacleHP, player);
            }
        }
    }

    private void SpawnPhase4Skeletons()
    {
        if (skeletonPrefab == null) return;

        for (int i = 0; i < skeletonSpawnCount; i++)
        {
            Vector3 basePos = roomCenter != null ? roomCenter.position : transform.position;
            Vector3 random = basePos + new Vector3(
                Random.Range(-navmeshSpawnRadius, navmeshSpawnRadius),
                0f,
                Random.Range(-navmeshSpawnRadius, navmeshSpawnRadius)
            );

            Vector3 valid = SampleNavmeshPoint(random, 8f, basePos);
            GameObject skel = Instantiate(skeletonPrefab, valid, Quaternion.identity);
            spawnedSkeletons.Add(skel);
        }
    }

    #endregion

    #region Movement / Facing

    private void MoveTowardsPlayer(float minDistance)
    {
        if (player == null || agent == null || !agent.enabled) return;

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
        if (currentPhase != BossPhase.Phase4 || player == null) return;
        if (isBusy) return;

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist > attackRange)
        {
            MoveTowardsPlayer(phase4MinDistance);
        }
        else if (dist < phase4MinDistance)
        {
            Vector3 away = (transform.position - player.position).normalized;
            Vector3 target = player.position + away * phase4MinDistance;
            if (agent != null && agent.enabled)
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
        if (agent == null || !agent.enabled) return;
        agent.isStopped = true;
        agent.ResetPath();
    }

    private IEnumerator MoveToPoint(Vector3 point, float stoppingDistance)
    {
        if (agent == null || !agent.enabled)
        {
            transform.position = point;
            yield break;
        }

        agent.isStopped = false;
        agent.stoppingDistance = stoppingDistance;
        agent.SetDestination(point);

        while (!isDead && Vector3.Distance(transform.position, point) > stoppingDistance + 0.25f)
        {
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

    #region Damage / Death / UI

    private void TryDamagePlayer(GameObject playerObj, float damage)
    {
        if (playerObj == null || damage <= 0f) return;

        playerObj.SendMessage("TakeDamage", Mathf.RoundToInt(damage), SendMessageOptions.DontRequireReceiver);
        playerObj.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;
        currentPhase = BossPhase.Dead;
        requestedPhase = BossPhase.Dead;
        scaledAttackDamage = 0f;

        StopAllCoroutines();
        StopMoving();

        ClearAllSpawnedObjects();

        animator.SetBool(AnimDeath, true);
        animator.SetTrigger(AnimDeath);
        animator.SetBool(AnimRunning, false);
    }

    private void UpdateBossUI()
    {
        if (bossHealthBarFill != null)
            bossHealthBarFill.fillAmount = maxHP <= 0 ? 0f : currentHP / maxHP;

        if (bossHealthText != null)
            bossHealthText.text = $"{Mathf.CeilToInt(currentHP)} / {Mathf.CeilToInt(maxHP)}";
    }

    private void RecalculateScaledStats()
    {
        int steps = GetScalingSteps();

        maxHP = baseMaxHP * (1f + (0.05f * steps));
        scaledAttackDamage = baseAttackDamage * (1f + (0.10f * steps));
    }

    private int GetScalingSteps()
    {
        // floor 5 => 0 steps, floor 10 => 1, floor 15 => 2
        return Mathf.Max(0, (currentFloor / 5) - 1);
    }

    #endregion

    #region Utility

    private Vector3 GetRowCenter(int rowIndex)
    {
        float cell = roomLength / 5f;
        float zMin = roomCenter.position.z - roomLength * 0.5f;
        float z = zMin + (cell * rowIndex) + cell * 0.5f;
        return new Vector3(roomCenter.position.x, roomCenter.position.y, z);
    }

    private Vector3 GetColumnCenter(int columnIndex)
    {
        float cell = roomWidth / 5f;
        float xMin = roomCenter.position.x - roomWidth * 0.5f;
        float x = xMin + (cell * columnIndex) + cell * 0.5f;
        return new Vector3(x, roomCenter.position.y, roomCenter.position.z);
    }

    private Vector3 GetRowBurstPoint(int rowIndex, int burstIndex)
    {
        float section = roomWidth / 3f;
        float xMin = roomCenter.position.x - roomWidth * 0.5f;
        float x = xMin + section * burstIndex + section * 0.5f;
        Vector3 rowCenter = GetRowCenter(rowIndex);
        return new Vector3(x, rowCenter.y, rowCenter.z);
    }

    private Vector3 GetColumnCellCenter(int columnIndex, int rowIndex)
    {
        float colSize = roomWidth / 5f;
        float rowSize = roomLength / 5f;

        float xMin = roomCenter.position.x - roomWidth * 0.5f;
        float zMin = roomCenter.position.z - roomLength * 0.5f;

        float x = xMin + colSize * columnIndex + colSize * 0.5f;
        float z = zMin + rowSize * rowIndex + rowSize * 0.5f;

        return new Vector3(x, roomCenter.position.y, z);
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
        DestroyAllInList(spawnedTentacles);
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

    private void ClearDeadRefs(List<GameObject> list)
    {
        list.RemoveAll(item => item == null);
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