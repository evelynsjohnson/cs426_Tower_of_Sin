using UnityEngine;
using UnityEngine.AI;
using TMPro;
using UnityEngine.UI;
using System.Collections;

// ─────────────────────────────────────────────────────────────────────────────
// WrathAI — Wrath Boss (Sin #2)
//
// Sin mechanic: The angrier Wrath gets (lower HP), the more dangerous she becomes.
//   Phase 1 (above 50% HP): Standard melee (swipe, punch). Periodically slams
//                            the ground for an AoE shockwave.
//   Phase 2 (below 50% HP): PERMANENT ENRAGE — speed x1.6, damage x1.8,
//                            attack rate x1.5, slam becomes more frequent.
//   Throughout: Brief invincibility after each hit (rewards timing over spam).
//
// AI system: Utility AI with response curves.
// Each action (Idle, Walk, Run, Swipe, Punch, Slam, Roar) is scored every frame
// from contextual inputs (distance, HP%, cooldowns, phase) each passed through
// a response curve (linear, quadratic, inverse, sigmoid, mid-peak).
// Highest score wins. Momentum penalty prevents back-to-back repetition.
// ─────────────────────────────────────────────────────────────────────────────

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(AudioSource))]
public class WrathAI : MonoBehaviour
{
    // ── Actions ───────────────────────────────────────────────────────────────
    public enum WrathAction { Idle, Walk, Run, Swipe, Punch, Slam, Roar }

    // ── Health & Phase ────────────────────────────────────────────────────────
    public float maxHealth          = 300f;
    private float currentHealth;
    private float damageMultiplier  = 1f;
    private int   currentPhase      = 1;
    private bool  phase2Triggered   = false;
    [SerializeField] private int currentFloor = 5;
    [SerializeField] private float baseHealthAtFloor5 = 360f;
    [SerializeField] private float healthPerFiveFloors = 75f;

    public float phase2Threshold    = 0.50f;  // HP% that triggers phase 2

    // ── Damage ────────────────────────────────────────────────────────────────
    public float damageSwipe        = 20f;
    public float damagePunch        = 25f;
    public float damageSlam         = 30f;    // AoE ground slam

    // ── Slam AoE ─────────────────────────────────────────────────────────────
    public float slamRadius         = 4f;
    public GameObject slamVFXPrefab;          // optional shockwave VFX

    // ── Hit Invincibility ─────────────────────────────────────────────────────
    // Brief iframes after each hit — rewards patient timing, punishes spam
    public float hitInvincibilityDuration = 0.3f;
    private bool isInvincible       = false;

    // ── UI ────────────────────────────────────────────────────────────────────
    public GameObject    uiCanvasObject;
    public TMP_Text      healthText;
    public Image         healthBarFill;
    public float         healthDrainSpeed       = 5f;
    public float         deathAnimationDuration = 2.5f;

    [Header("Shared Boss UI (Optional)")]
    [SerializeField] private Image bossHealthBarFill;
    [SerializeField] private TMP_Text bossHealthText;
    [SerializeField] private GameObject bossHealthUIRoot;

    [Header("Arena Death Effects")]
    [SerializeField] private Color bossLightColor = new Color(1f, 0.45f, 0.25f);
    [SerializeField] private float lightIntensityMultiplier = 1.2f;

    // ── Movement ──────────────────────────────────────────────────────────────
    public float walkSpeed          = 3.0f;
    public float runSpeed           = 5.5f;
    public float aggroRadius        = 18f;
    public float attackRadius       = 2.5f;

    // ── Cooldowns (seconds) ───────────────────────────────────────────────────
    public float cdSwipe            = 1.8f;
    public float cdPunch            = 2.2f;
    public float cdSlam             = 6.0f;
    public float cdRoar             = 999f;   // roar fires once at phase 2 — large cooldown prevents repeat
    public float attackDmgDelay     = 0.45f;

    private float nextSwipe         = 0f;
    private float nextPunch         = 0f;
    private float nextSlam          = 0f;
    private float nextRoar          = 0f;

    // ── Utility AI weights ────────────────────────────────────────────────────
    [Header("Utility Weights — tune in Inspector")]
    public float wIdle              = 0.10f;
    public float wWalk              = 1.00f;
    public float wRun               = 1.20f;
    public float wSwipe             = 2.00f;
    public float wPunch             = 1.80f;
    public float wSlam              = 1.60f;
    public float wRoar              = 3.00f;  // high weight — roar is a dramatic moment

    [Header("Momentum penalty (0-1): lower = stronger penalty")]
    [Range(0f, 1f)]
    public float momentumPenalty    = 0.45f;

    // ── Audio ─────────────────────────────────────────────────────────────────
    public AudioClip bossMusic;
    public AudioClip hitSound;
    public AudioClip missSound;
    public AudioClip idleSound;
    public AudioClip walkSound;
    public AudioClip roarSound;
    public AudioClip slamSound;

    // ── Loot ──────────────────────────────────────────────────────────────────
    public GameObject healthPotionPrefab;
    public float      healthPotChance    = 40f;

    // ── Component references ──────────────────────────────────────────────────
    public Animator animator;

    private Transform    player;
    private Transform    mainCamera;
    private NavMeshAgent agent;
    private PlayerHealth playerHealthScript;
    private AudioSource  sfxSource;
    private AudioSource  walkSource;
    private AudioSource  musicSource;
    private Light[] arenaLights = new Light[0];
    private Color[] originalLightColors = new Color[0];
    private float[] originalLightIntensities = new float[0];
    private Transform basementDoorLeftRef;
    private Transform basementDoorRightRef;
    private AudioSource gateAudioSourceRef;
    private AudioClip largeGateClipRef;
    private GameObject bossChestPrefabRef;
    private Transform bossChestSpawnPointRef;
    private float doorMoveDistanceZRef = 1f;
    private float doorMoveDurationRef = 3f;

    // ── Runtime state ─────────────────────────────────────────────────────────
    private bool       isDead            = false;
    private bool       isActing          = false;
    private bool       hasSeenPlayer     = false;
    private WrathAction currentAction    = WrathAction.Idle;
    private WrathAction lastAction       = WrathAction.Idle;

    private float   idleAudioTimer       = 0f;
    private float   lastDamageTime       = -999f;
    private float   healDelay            = 8f;

    public void SetFloor(int floor)
    {
        currentFloor = Mathf.Max(1, floor);
    }

    private float GetScaledHealthForFloor(int floor)
    {
        int bonusSteps = Mathf.Max(0, floor / 5 - 1);
        return baseHealthAtFloor5 + (bonusSteps * healthPerFiveFloors);
    }

    public void SetupArenaReferences(Image sharedHealthBarFill, TMP_Text sharedHealthText, GameObject sharedHealthUIRoot)
    {
        SetupArenaReferences(
            sharedHealthBarFill,
            sharedHealthText,
            sharedHealthUIRoot,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            1f,
            3f
        );
    }

    public void SetupArenaReferences(
        Image sharedHealthBarFill,
        TMP_Text sharedHealthText,
        GameObject sharedHealthUIRoot,
        Light[] lights,
        Transform basementDoorLeft,
        Transform basementDoorRight,
        AudioSource gateAudioSource,
        AudioClip largeGateClip,
        GameObject bossChestPrefab,
        Transform bossChestSpawnPoint,
        float doorMoveDistanceZ,
        float doorMoveDuration)
    {
        bossHealthBarFill = sharedHealthBarFill;
        bossHealthText = sharedHealthText;
        bossHealthUIRoot = sharedHealthUIRoot;
        basementDoorLeftRef = basementDoorLeft;
        basementDoorRightRef = basementDoorRight;
        gateAudioSourceRef = gateAudioSource;
        largeGateClipRef = largeGateClip;
        bossChestPrefabRef = bossChestPrefab;
        bossChestSpawnPointRef = bossChestSpawnPoint;
        doorMoveDistanceZRef = Mathf.Abs(doorMoveDistanceZ);
        doorMoveDurationRef = Mathf.Max(0.1f, doorMoveDuration);

        arenaLights = lights ?? new Light[0];
        originalLightColors = new Color[arenaLights.Length];
        originalLightIntensities = new float[arenaLights.Length];
        for (int i = 0; i < arenaLights.Length; i++)
        {
            if (arenaLights[i] == null) continue;
            originalLightColors[i] = arenaLights[i].color;
            originalLightIntensities[i] = arenaLights[i].intensity;
        }

        if (bossHealthUIRoot != null)
            bossHealthUIRoot.SetActive(true);

        // When shared top-of-screen boss UI is used, hide any world-space HP UI.
        if ((bossHealthBarFill != null || bossHealthText != null || bossHealthUIRoot != null) && uiCanvasObject != null)
            uiCanvasObject.SetActive(false);

        ApplyBossLightState();
        SetHealthBarFillImmediate(1f);
        UpdateHealthUI();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    void Start()
    {
        if (currentFloor <= 0)
            currentFloor = Mathf.Max(1, FloorTextController.floorNumber);

        maxHealth     = GetScaledHealthForFloor(currentFloor);
        currentHealth = maxHealth;

        agent       = GetComponent<NavMeshAgent>();
        agent.speed = walkSpeed;

        sfxSource = GetComponent<AudioSource>();
        sfxSource.spatialBlend = 1f;
        sfxSource.rolloffMode  = AudioRolloffMode.Linear;
        sfxSource.minDistance  = 2f;
        sfxSource.maxDistance  = 12f;

        walkSource = gameObject.AddComponent<AudioSource>();
        walkSource.spatialBlend = 1f;
        walkSource.rolloffMode  = AudioRolloffMode.Linear;
        walkSource.minDistance  = 2f;
        walkSource.maxDistance  = 18f;
        walkSource.clip         = walkSound;
        walkSource.loop         = true;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player             = playerObj.transform;
            playerHealthScript = playerObj.GetComponent<PlayerHealth>();
        }

        if (Camera.main != null) mainCamera = Camera.main.transform;
        if (bossHealthUIRoot != null) bossHealthUIRoot.SetActive(true);
        if ((bossHealthBarFill != null || bossHealthText != null || bossHealthUIRoot != null) && uiCanvasObject != null)
            uiCanvasObject.SetActive(false);
        SetHealthBarFillImmediate(1f);

        if (bossMusic != null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.clip        = bossMusic;
            musicSource.loop        = true;
            musicSource.spatialBlend = 0f;  // 2D so it's heard everywhere
            musicSource.volume      = 0.6f;
            musicSource.Play();
        }

        idleAudioTimer = Random.Range(3f, 7f);
        UpdateHealthUI();
    }

    void Update()
    {
        UpdateHealthBar();

        if (isDead || player == null) return;

        HandleAudio();
        HandleHealthRegen();

        if (!hasSeenPlayer && FlatDist(transform.position, player.position) <= aggroRadius)
            hasSeenPlayer = true;

        if (!hasSeenPlayer) return;

        // Check phase 2 transition
        if (!phase2Triggered && currentHealth / maxHealth <= phase2Threshold)
            TriggerPhase2();

        if (isActing) return;

        WrathAction best = EvaluateUtility();
        ExecuteAction(best);
    }

    void LateUpdate()
    {
        if (bossHealthBarFill != null)
            return;

        if (uiCanvasObject != null && mainCamera != null)
            uiCanvasObject.transform.LookAt(uiCanvasObject.transform.position + mainCamera.forward);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Phase 2 — Permanent Enrage
    // ─────────────────────────────────────────────────────────────────────────

    private void TriggerPhase2()
    {
        phase2Triggered = true;
        currentPhase    = 2;

        // Unlock roar immediately
        nextRoar = 0f;

        // Stat boosts applied after the roar finishes (inside DoRoar)
        // cdSlam gets more frequent in phase 2
        cdSlam = Mathf.Max(2.5f, cdSlam * 0.6f);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Utility AI — scoring
    // ─────────────────────────────────────────────────────────────────────────

    private WrathAction EvaluateUtility()
    {
        float dist  = FlatDist(transform.position, player.position);
        float hpPct = currentHealth / maxHealth;
        float now   = Time.time;

        float[] scores = new float[System.Enum.GetValues(typeof(WrathAction)).Length];

        scores[(int)WrathAction.Idle]  = ScoreIdle();
        scores[(int)WrathAction.Walk]  = ScoreWalk(dist);
        scores[(int)WrathAction.Run]   = ScoreRun(dist, hpPct);
        scores[(int)WrathAction.Swipe] = ScoreSwipe(dist, hpPct, now);
        scores[(int)WrathAction.Punch] = ScorePunch(dist, hpPct, now);
        scores[(int)WrathAction.Slam]  = ScoreSlam(dist, hpPct, now);
        scores[(int)WrathAction.Roar]  = ScoreRoar(hpPct, now);

        // Momentum penalty — prevents back-to-back same action
        scores[(int)lastAction] *= momentumPenalty;

        int   bestIdx   = 0;
        float bestScore = scores[0];
        for (int i = 1; i < scores.Length; i++)
        {
            if (scores[i] > bestScore) { bestScore = scores[i]; bestIdx = i; }
        }

        return (WrathAction)bestIdx;
    }

    // ── Scoring functions ─────────────────────────────────────────────────────

    private float ScoreIdle()
        => wIdle * 0.1f;

    // Walk — linear, preferred at mid range, zero inside attack radius
    private float ScoreWalk(float dist)
    {
        if (dist <= attackRadius) return 0f;
        return wWalk * RLinear(1f - Norm(dist, attackRadius, aggroRadius));
    }

    // Run — quadratic, strongly preferred when far; more urgent in phase 2
    private float ScoreRun(float dist, float hpPct)
    {
        if (dist <= attackRadius * 1.5f) return 0f;
        float distScore = RQuadratic(Norm(dist, attackRadius, aggroRadius));
        float phase2    = currentPhase >= 2 ? 1.4f : 1.0f;
        return wRun * distScore * phase2;
    }

    // Swipe — inverse quadratic (best up close), more aggressive at low HP
    private float ScoreSwipe(float dist, float hpPct, float now)
    {
        if (now < nextSwipe)     return 0f;
        if (dist > attackRadius) return 0f;
        float distScore = RInvQuadratic(Norm(dist, 0f, attackRadius));
        float anger     = 0.6f + 0.4f * RInverse(hpPct);  // angrier = more likely to swipe
        return wSwipe * distScore * anger;
    }

    // Punch — slightly longer reach, peaks when swipe is on cooldown
    private float ScorePunch(float dist, float hpPct, float now)
    {
        if (now < nextPunch)              return 0f;
        if (dist > attackRadius * 1.15f)  return 0f;
        float distScore = RInvQuadratic(Norm(dist, 0f, attackRadius * 1.15f));
        float anger     = 0.5f + 0.5f * RInverse(hpPct);
        float gap       = (now < nextSwipe) ? 1.4f : 1.0f;  // fill swipe cooldown gap
        return wPunch * distScore * anger * gap;
    }

    // Slam — mid-peak (best at mid range for AoE coverage), unlocked always
    // but higher weight in phase 2 when cdSlam is shorter
    private float ScoreSlam(float dist, float hpPct, float now)
    {
        if (now < nextSlam) return 0f;
        // Slam is useful both at close range (player can't escape AoE) and mid range
        float distScore  = RInvQuadratic(Norm(dist, 0f, slamRadius * 2f));
        float angerScore = RInverse(hpPct);  // more likely when angry/hurt
        float phase2     = currentPhase >= 2 ? 1.5f : 1.0f;
        return wSlam * distScore * (0.4f + 0.6f * angerScore) * phase2;
    }

    // Roar — logistic spike when entering phase 2; fires exactly once per phase transition
    private float ScoreRoar(float hpPct, float now)
    {
        if (now < nextRoar) return 0f;
        // Only score if phase 2 just triggered (nextRoar was set to 0 in TriggerPhase2)
        float angerSpike = RLogistic(1f - hpPct, steepness: 12f, midpoint: 0.5f);
        return wRoar * angerSpike;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Response curves  (normalised [0,1] input → [0,1] output)
    // ─────────────────────────────────────────────────────────────────────────

    // f(x) = x
    private float RLinear(float x)       => Mathf.Clamp01(x);

    // f(x) = x²
    private float RQuadratic(float x)    { x = Mathf.Clamp01(x); return x * x; }

    // f(x) = 1 - x
    private float RInverse(float x)      => Mathf.Clamp01(1f - x);

    // f(x) = 1 - x²
    private float RInvQuadratic(float x) { x = Mathf.Clamp01(x); return 1f - x * x; }

    // f(x) = 1 / (1 + e^(-k*(x-m)))
    private float RLogistic(float x, float steepness = 10f, float midpoint = 0.5f)
        => 1f / (1f + Mathf.Exp(-steepness * (x - midpoint)));

    // ─────────────────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private float Norm(float value, float min, float max)
    {
        if (max <= min) return 0f;
        return Mathf.Clamp01((value - min) / (max - min));
    }

    private float FlatDist(Vector3 a, Vector3 b)
        => Vector3.Distance(new Vector3(a.x, 0f, a.z), new Vector3(b.x, 0f, b.z));

    private void FacePlayer()
    {
        if (player == null) return;
        Vector3 dir = player.position - transform.position;
        dir.y = 0f;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(
                transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 8f);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Action execution
    // ─────────────────────────────────────────────────────────────────────────

    private void ExecuteAction(WrathAction action)
    {
        currentAction = action;

        switch (action)
        {
            case WrathAction.Idle:  DoIdle();                       break;
            case WrathAction.Walk:  DoWalk();                       break;
            case WrathAction.Run:   DoRun();                        break;
            case WrathAction.Swipe: StartCoroutine(DoSwipe());      break;
            case WrathAction.Punch: StartCoroutine(DoPunch());      break;
            case WrathAction.Slam:  StartCoroutine(DoSlam());       break;
            case WrathAction.Roar:  StartCoroutine(DoRoar());       break;
        }

        lastAction = action;
        UpdateAnimations();
        UpdateWalkAudio();
    }

    private void DoIdle()
    {
        agent.isStopped = true;
        agent.velocity  = Vector3.zero;
    }

    private void DoWalk()
    {
        agent.speed     = walkSpeed;
        agent.isStopped = false;
        agent.SetDestination(player.position);
        FacePlayer();
    }

    private void DoRun()
    {
        agent.speed     = currentPhase >= 2 ? runSpeed * 1.6f : runSpeed;
        agent.isStopped = false;
        agent.SetDestination(player.position);
        FacePlayer();
    }

    private IEnumerator DoSwipe()
    {
        isActing = true;
        agent.isStopped = true;
        FacePlayer();

        animator.SetTrigger("swipe");
        nextSwipe = Time.time + cdSwipe;

        yield return new WaitForSeconds(attackDmgDelay);

        if (!isDead && player != null)
        {
            if (FlatDist(transform.position, player.position) <= attackRadius + 0.5f)
            {
                if (playerHealthScript != null)
                    playerHealthScript.TakeDamage(damageSwipe * damageMultiplier);
                if (hitSound != null) sfxSource.PlayOneShot(hitSound);
            }
            else
            {
                if (missSound != null) sfxSource.PlayOneShot(missSound);
            }
        }

        yield return new WaitForSeconds(2.667f - attackDmgDelay);
        isActing = false;
    }

    private IEnumerator DoPunch()
    {
        isActing = true;
        agent.isStopped = true;
        FacePlayer();

        animator.SetTrigger("punch");
        nextPunch = Time.time + cdPunch;

        yield return new WaitForSeconds(attackDmgDelay + 0.1f);

        if (!isDead && player != null)
        {
            if (FlatDist(transform.position, player.position) <= attackRadius * 1.15f + 0.5f)
            {
                if (playerHealthScript != null)
                    playerHealthScript.TakeDamage(damagePunch * damageMultiplier);
                if (hitSound != null) sfxSource.PlayOneShot(hitSound);
            }
            else
            {
                if (missSound != null) sfxSource.PlayOneShot(missSound);
            }
        }

        yield return new WaitForSeconds(1.1f - (attackDmgDelay + 0.1f));
        isActing = false;
    }

    // Ground slam — wind-up, then AoE damage check inside slamRadius
    private IEnumerator DoSlam()
    {
        isActing = true;
        agent.isStopped = true;
        FacePlayer();

        animator.SetTrigger("slam");
        nextSlam = Time.time + cdSlam;

        yield return new WaitForSeconds(1.85f);  // ~halfway through jump attack = impact point

        // Spawn VFX at ground level
        if (slamVFXPrefab != null)
            Instantiate(slamVFXPrefab, transform.position, Quaternion.identity);

        if (slamSound != null) sfxSource.PlayOneShot(slamSound);

        // AoE damage — hits player if within slam radius
        if (!isDead && player != null)
        {
            float dist = Vector3.Distance(transform.position, player.position);
            if (dist <= slamRadius && playerHealthScript != null)
                playerHealthScript.TakeDamage(damageSlam * damageMultiplier);
        }

        yield return new WaitForSeconds(3.7f - 1.85f);
        isActing = false;
    }

    // Roar — plays once when entering phase 2, then applies permanent stat boosts
    private IEnumerator DoRoar()
    {
        isActing = true;
        agent.isStopped = true;

        animator.SetTrigger("roar");
        nextRoar = Time.time + cdRoar;  // large cooldown — effectively a one-shot

        if (roarSound != null) sfxSource.PlayOneShot(roarSound);

        yield return new WaitForSeconds(5.4f);

        // Permanent phase 2 enrage boosts
        damageMultiplier *= 1.8f;
        agent.speed       = Mathf.Min(agent.speed * 1.6f, runSpeed * 2f);
        attackDmgDelay    = Mathf.Max(0.2f, attackDmgDelay * 0.7f);   // faster attack timing
        cdSwipe           = Mathf.Max(0.8f, cdSwipe  / 1.5f);
        cdPunch           = Mathf.Max(1.0f, cdPunch  / 1.5f);
        animator.speed    = Mathf.Min(animator.speed * 1.3f, 2.0f);

        isActing = false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Damage & Death
    // ─────────────────────────────────────────────────────────────────────────

    public void TakeDamage(float amount)
    {
        if (isDead || isInvincible) return;

        lastDamageTime = Time.time;
        hasSeenPlayer  = true;
        currentHealth -= amount;
        currentHealth  = Mathf.Max(0f, currentHealth);
        UpdateHealthUI();

        // Brief invincibility after each hit — rewards timing over button spam
        StartCoroutine(HitInvincibility());

        if (currentHealth <= 0f) Die();
    }

    private IEnumerator HitInvincibility()
    {
        isInvincible = true;
        yield return new WaitForSeconds(hitInvincibilityDuration);
        isInvincible = false;
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        StopAllCoroutines();
        isActing     = false;
        isInvincible = false;

        animator.SetTrigger("die");

        if (agent.enabled) agent.enabled = false;
        GetComponent<Collider>().enabled  = false;

        TMP_Text targetText = bossHealthText != null ? bossHealthText : healthText;
        if (targetText != null) targetText.text = "";
        if (walkSource  != null) walkSource.Stop();
        if (sfxSource   != null) sfxSource.Stop();

        if (healthPotionPrefab != null && Random.Range(0f, 100f) < healthPotChance)
            Instantiate(healthPotionPrefab, transform.position + Vector3.up * 0.2f, Quaternion.identity);

        HandleArenaDefeatRewards();
        StartCoroutine(HideUIAfterDeath());
    }

    private IEnumerator HideUIAfterDeath()
    {
        yield return new WaitForSeconds(deathAnimationDuration);
        if (bossHealthUIRoot != null) bossHealthUIRoot.SetActive(false);
        else if (uiCanvasObject != null) uiCanvasObject.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Passive systems
    // ─────────────────────────────────────────────────────────────────────────

    private void HandleHealthRegen()
    {
        // Wrath does not regen — she only gets angrier
    }

    private void HandleAudio()
    {
        float vertDist  = Mathf.Abs(player.position.y - transform.position.y);
        bool  sameFloor = vertDist < 2.5f;
        sfxSource.mute  = !sameFloor;
        walkSource.mute = !sameFloor;

        idleAudioTimer -= Time.deltaTime;
        if (idleAudioTimer <= 0f)
        {
            if (idleSound != null && sameFloor) sfxSource.PlayOneShot(idleSound);
            idleAudioTimer = Random.Range(4f, 8f);
        }
    }

    private void UpdateAnimations()
    {
        animator.SetBool("isWalking", currentAction == WrathAction.Walk);
        animator.SetBool("isRunning", currentAction == WrathAction.Run);
    }

    private void UpdateWalkAudio()
    {
        bool moving = currentAction == WrathAction.Walk || currentAction == WrathAction.Run;
        if (moving  && !walkSource.isPlaying) walkSource.Play();
        if (!moving && walkSource.isPlaying)  walkSource.Pause();
    }

    private void UpdateHealthBar()
    {
        Image targetFill = bossHealthBarFill != null ? bossHealthBarFill : healthBarFill;
        if (targetFill == null) return;
        float target = currentHealth / maxHealth;
        targetFill.fillAmount = Mathf.Lerp(targetFill.fillAmount, target, Time.deltaTime * healthDrainSpeed);
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

    // ─────────────────────────────────────────────────────────────────────────
    //  Debug Gizmos
    // ─────────────────────────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aggroRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadius);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, slamRadius);
    }

    private void HandleArenaDefeatRewards()
    {
        RestoreArenaLights();

        if (bossChestPrefabRef != null && bossChestSpawnPointRef != null)
            Instantiate(bossChestPrefabRef, bossChestSpawnPointRef.position, bossChestSpawnPointRef.rotation);

        if (gateAudioSourceRef != null)
        {
            if (largeGateClipRef != null) gateAudioSourceRef.PlayOneShot(largeGateClipRef);
            else gateAudioSourceRef.Play();
        }

        if (basementDoorLeftRef != null)
            StartCoroutine(MoveDoorZ(basementDoorLeftRef, -doorMoveDistanceZRef, doorMoveDurationRef));
        if (basementDoorRightRef != null)
            StartCoroutine(MoveDoorZ(basementDoorRightRef, doorMoveDistanceZRef, doorMoveDurationRef));
    }

    private void ApplyBossLightState()
    {
        if (arenaLights == null) return;
        for (int i = 0; i < arenaLights.Length; i++)
        {
            if (arenaLights[i] == null) continue;
            arenaLights[i].color = bossLightColor;
            float baseIntensity = (originalLightIntensities != null && i < originalLightIntensities.Length)
                ? originalLightIntensities[i]
                : arenaLights[i].intensity;
            arenaLights[i].intensity = baseIntensity * lightIntensityMultiplier;
        }
    }

    private void RestoreArenaLights()
    {
        if (arenaLights == null) return;
        for (int i = 0; i < arenaLights.Length; i++)
        {
            if (arenaLights[i] == null) continue;
            if (originalLightColors != null && i < originalLightColors.Length)
                arenaLights[i].color = originalLightColors[i];
            if (originalLightIntensities != null && i < originalLightIntensities.Length)
                arenaLights[i].intensity = originalLightIntensities[i];
        }
    }

    private IEnumerator MoveDoorZ(Transform door, float zOffset, float duration)
    {
        if (door == null) yield break;

        Vector3 start = door.position;
        Vector3 end = start + new Vector3(0f, 0f, zOffset);
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.1f, duration);

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            door.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        door.position = end;
    }
}
