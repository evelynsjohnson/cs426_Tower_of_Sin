using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using TMPro;

// Refactored to use BossArenaController for doors/chest/lights/music
public class EnvyAI : MonoBehaviour
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

    [SerializeField] private AudioClip ticktockLoop;
    [SerializeField] private AudioClip robotNoise;
    [SerializeField] private AudioClip robotNoise2;
    [SerializeField] private AudioClip introAudio;
    [SerializeField] private AudioClip phaseTwoAudio;
    [SerializeField] private AudioClip deathAudio;
    [SerializeField] public List<AudioClip> attackVoiceClips = new List<AudioClip>();
    [SerializeField] private AudioClip bossMusicClip;
    [SerializeField][Range(0f, 1f)] private float bossMusicVolume = 1f;

    [SerializeField][Range(0f, 1f)] private float ticktockVolume = 1f;
    [SerializeField][Range(0f, 1f)] private float robotNoiseVolume = 1f;
    [SerializeField][Range(0f, 1f)] private float robotNoise2Volume = 1f;
    [SerializeField][Range(0f, 1f)] private float voiceVolume = 1f;
    [SerializeField] private float introTriggerRange = 25f;
    [SerializeField] private Vector2 randomRobotNoiseDelay = new Vector2(5f, 10f);
    [SerializeField] private Vector2 randomAttackVoiceDelay = new Vector2(10f, 20f);
    [SerializeField] private float audioMinDistance = 2f;
    [SerializeField] private float audioMaxDistance = 30f;

    [SerializeField] private float baseMaxHealth = 650f;
    private float currentHealth;
    private float maxHealth;
    private BossPhase currentPhase = BossPhase.Phase1;
    private int currentFloor = 5;

    [SerializeField] private Animator animator;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private GameObject envycirclePrefab;
    [SerializeField] private Transform player;
    [SerializeField] private string playerTag = "Player";

    [SerializeField] private float teleportSearchRadius = 25f;
    [SerializeField] private float navMeshSampleDistance = 8f;
    [SerializeField] private float maxTeleportHeightDifference = 1.5f;
    [SerializeField] private int teleportAttempts = 20;

    [SerializeField] private AudioMixerGroup sfxMixerGroup;
    [SerializeField] private AudioMixerGroup narrationMixerGroup;

    [SerializeField] private float delayAfterTeleport = 2f;
    [SerializeField] private string attackTriggerName = "attack1";
    [SerializeField] private Vector3 circleSpawnOffset = Vector3.zero;

    [SerializeField] private float engageRange = 15f;
    [SerializeField] private float attackCooldown = 7f;
    [SerializeField] private float phase1DelayAfterEachCircleEnds = 2f;
    [SerializeField] private float phase2DelayBetweenSpawns = 1f;
    [SerializeField] private float phase2DelayAfterLastCircleEnds = 2f;

    [SerializeField] private float circleDelayBeforeHit = 1.2f;
    [SerializeField] private float circleDamageRadius = 4.75f;
    [SerializeField] private float circleDamage = 30f;
    [SerializeField] private float circleLifetime = 2.5f;

    [SerializeField] private bool drawDebugRange = true;

    [SerializeField] private bool alwaysFacePlayer = true;
    [SerializeField] private float faceTurnSpeed = 720f;

    private Image bossHealthBarFill;
    private TMP_Text bossHealthText;
    private GameObject bossHealthUIRoot;

    private BossArenaController arenaController;

    private AudioSource ticktockSource;
    private AudioSource robotNoiseSource;
    private AudioSource robotNoise2Source;
    private AudioSource voiceSource;

    private Coroutine aiLoopRoutine;
    private Coroutine robotNoiseRoutine;
    private Coroutine robotNoise2Routine;
    private Coroutine attackVoiceRoutine;
    private Coroutine deathFadeRoutine;

    private float nextAttackTime = 0f;
    private bool isBusy = false;
    private bool phase2Started = false;
    private bool isDead = false;
    private bool introPlayed = false;
    private bool phaseTwoVoicePending = false;
    private bool deathVoiceStarted = false;
    private bool introStarted = false;
    private bool introFinished = false;

    private int nextAttackVoiceIndex = 0;

    private Renderer[] cachedRenderers;
    private readonly Dictionary<Material, Color> originalMaterialColors = new Dictionary<Material, Color>();

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

        if (agent != null)
            agent.updateRotation = false;

        FindPlayerIfNeeded();
        SetupAudioSources();
        CacheRenderers();
    }

    private void Start()
    {
        SetupHealthForFloor(currentFloor);
        UpdateBossUI();

        if (bossHealthUIRoot != null)
            bossHealthUIRoot.SetActive(true);

        StartAmbientBossAudio();

        if (aiLoopRoutine != null)
            StopCoroutine(aiLoopRoutine);
        aiLoopRoutine = StartCoroutine(BossLoop());

        if (attackVoiceRoutine != null)
            StopCoroutine(attackVoiceRoutine);
        attackVoiceRoutine = StartCoroutine(AttackVoiceLoop());
    }

    private void Update()
    {
        if (!alwaysFacePlayer || isDead)
            return;

        FindPlayerIfNeeded();

        if (player != null)
            FaceTarget(player.position);
    }

    public void SetArenaController(BossArenaController controller)
    {
        arenaController = controller;

        if (arenaController != null)
        {
            bossHealthBarFill = arenaController.GetBossHealthBarFill();
            bossHealthText = arenaController.GetBossHealthText();
            bossHealthUIRoot = arenaController.GetBossHealthUIRoot();

            arenaController.OnBossSpawned(aliveLightColor, 1f, bossMusicClip, bossMusicVolume);
        }

        UpdateBossUI();
    }

    public void SetBossUIReferences(Image healthBarFill, TMP_Text healthText, GameObject healthUIRoot)
    {
        bossHealthBarFill = healthBarFill;
        bossHealthText = healthText;
        bossHealthUIRoot = healthUIRoot;
        UpdateBossUI();
    }

    private void SetupAudioSources()
    {
        ticktockSource = Create3DAudioSource("TicktockSource", sfxMixerGroup);
        robotNoiseSource = Create3DAudioSource("RobotNoiseSource", sfxMixerGroup);
        robotNoise2Source = Create3DAudioSource("RobotNoise2Source", sfxMixerGroup);
        voiceSource = Create3DAudioSource("VoiceSource", narrationMixerGroup);

        if (voiceSource != null)
        {
            voiceSource.spatialBlend = 0.2f;
            voiceSource.minDistance = 8f;
            voiceSource.maxDistance = 60f;
        }
    }

    private AudioSource Create3DAudioSource(string sourceName, AudioMixerGroup mixerGroup)
    {
        GameObject child = new GameObject(sourceName);
        child.transform.SetParent(transform, false);

        AudioSource source = child.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = audioMinDistance;
        source.maxDistance = audioMaxDistance;
        source.dopplerLevel = 0f;

        source.outputAudioMixerGroup = mixerGroup;

        return source;
    }

    private void CacheRenderers()
    {
        cachedRenderers = GetComponentsInChildren<Renderer>(true);
        originalMaterialColors.Clear();

        foreach (Renderer rend in cachedRenderers)
        {
            if (rend == null) continue;

            Material[] mats = rend.materials;
            foreach (Material mat in mats)
            {
                if (mat == null || originalMaterialColors.ContainsKey(mat)) continue;

                if (mat.HasProperty("_BaseColor"))
                    originalMaterialColors[mat] = mat.GetColor("_BaseColor");
                else if (mat.HasProperty("_Color"))
                    originalMaterialColors[mat] = mat.GetColor("_Color");
            }
        }
    }

    private void StartAmbientBossAudio()
    {
        if (ticktockLoop != null && ticktockSource != null)
        {
            ticktockSource.clip = ticktockLoop;
            ticktockSource.volume = ticktockVolume;
            ticktockSource.loop = true;
            ticktockSource.Play();
        }

        if (robotNoiseRoutine != null)
            StopCoroutine(robotNoiseRoutine);
        robotNoiseRoutine = StartCoroutine(RandomBossNoiseLoop(robotNoiseSource, robotNoise, robotNoiseVolume));

        if (robotNoise2Routine != null)
            StopCoroutine(robotNoise2Routine);
        robotNoise2Routine = StartCoroutine(RandomBossNoiseLoop(robotNoise2Source, robotNoise2, robotNoise2Volume));
    }

    private IEnumerator RandomBossNoiseLoop(AudioSource source, AudioClip clip, float volume)
    {
        if (source == null || clip == null)
            yield break;

        while (!isDead)
        {
            float wait = Random.Range(randomRobotNoiseDelay.x, randomRobotNoiseDelay.y);
            yield return new WaitForSeconds(wait);

            if (isDead) yield break;

            source.PlayOneShot(clip, volume);
        }
    }

    private IEnumerator AttackVoiceLoop()
    {
        while (!isDead)
        {
            if (!introPlayed || phaseTwoVoicePending || deathVoiceStarted || attackVoiceClips.Count == 0)
            {
                yield return null;
                continue;
            }

            float wait = Random.Range(randomAttackVoiceDelay.x, randomAttackVoiceDelay.y);
            yield return new WaitForSeconds(wait);

            while (!isDead && (voiceSource == null || voiceSource.isPlaying || phaseTwoVoicePending || deathVoiceStarted))
                yield return null;

            if (isDead || phaseTwoVoicePending || deathVoiceStarted)
                continue;

            AudioClip nextClip = attackVoiceClips[nextAttackVoiceIndex];
            nextAttackVoiceIndex = (nextAttackVoiceIndex + 1) % attackVoiceClips.Count;

            if (nextClip != null && voiceSource != null)
            {
                voiceSource.clip = nextClip;
                voiceSource.volume = voiceVolume;
                voiceSource.loop = false;
                voiceSource.Play();
            }
        }
    }

    private IEnumerator BossLoop()
    {
        while (!isDead)
        {
            FindPlayerIfNeeded();

            if (!introPlayed && player != null)
            {
                float introDist = Vector3.Distance(transform.position, player.position);
                if (introDist <= introTriggerRange)
                    PlayIntroAudioOnce();
            }

            if (!introFinished)
            {
                yield return null;
                continue;
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

    private void PlayIntroAudioOnce()
    {
        if (introStarted)
            return;

        introStarted = true;
        StartCoroutine(PlayIntroRoutine());
    }

    private IEnumerator PlayIntroRoutine()
    {
        if (voiceSource != null)
            voiceSource.Stop();

        if (introAudio != null && voiceSource != null)
        {
            voiceSource.clip = introAudio;
            voiceSource.volume = voiceVolume;
            voiceSource.loop = false;
            voiceSource.Play();

            while (!isDead && voiceSource.isPlaying)
                yield return null;
        }

        introPlayed = true;
        introFinished = true;
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
        introPlayed = false;
        introStarted = false;
        introFinished = false;
        phaseTwoVoicePending = false;
        deathVoiceStarted = false;
        nextAttackVoiceIndex = 0;
        nextAttackTime = 0f;
        isBusy = false;
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
        if (isDead || damage <= 0f || !introFinished)
            return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        UpdateBossUI();

        if (!phase2Started && currentHealth <= maxHealth * 0.75f)
            EnterPhase2();

        if (currentHealth <= 0f)
            Die();
    }

    private void EnterPhase2()
    {
        phase2Started = true;
        currentPhase = BossPhase.Phase2;
        phaseTwoVoicePending = true;
        StartCoroutine(PlayPhaseTwoAudioWhenClear());
    }

    private IEnumerator PlayPhaseTwoAudioWhenClear()
    {
        while (!isDead && voiceSource != null && voiceSource.isPlaying)
            yield return null;

        if (isDead)
            yield break;

        if (phaseTwoAudio != null && voiceSource != null)
        {
            voiceSource.clip = phaseTwoAudio;
            voiceSource.volume = voiceVolume;
            voiceSource.loop = false;
            voiceSource.Play();

            while (!isDead && voiceSource.isPlaying)
                yield return null;
        }

        phaseTwoVoicePending = false;
    }

    private void Die()
    {
        if (isDead) return;
        circleDamage = 0f;
        isDead = true;
        currentPhase = BossPhase.Dead;
        deathVoiceStarted = true;
        isBusy = false;

        //if (aiLoopRoutine != null) StopCoroutine(aiLoopRoutine);
        //if (robotNoiseRoutine != null) StopCoroutine(robotNoiseRoutine);
        //if (robotNoise2Routine != null) StopCoroutine(robotNoise2Routine);
        //if (attackVoiceRoutine != null) StopCoroutine(attackVoiceRoutine);

        StopAllCoroutines();

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        if (ticktockSource != null)
            ticktockSource.Stop();
        if (robotNoiseSource != null)
            robotNoiseSource.Stop();
        if (robotNoise2Source != null)
            robotNoise2Source.Stop();

        UpdateBossUI();

        if (arenaController != null)
            arenaController.OnBossDied();

        PlayDeathAudioAndFadeBoss();

        Collider[] cols = GetComponentsInChildren<Collider>();
        foreach (Collider col in cols)
        {
            col.enabled = false;
        }


    }

    private void PlayDeathAudioAndFadeBoss()
    {
        if (deathAudio != null && voiceSource != null)
        {
            voiceSource.Stop();
            voiceSource.clip = deathAudio;
            voiceSource.volume = voiceVolume;
            voiceSource.loop = false;
            voiceSource.Play();

            float startFadeDelay = deathAudio.length * 0.5f;
            float fadeDuration = (deathAudio.length * 0.5f) + 5f;

            if (deathFadeRoutine != null)
                StopCoroutine(deathFadeRoutine);

            deathFadeRoutine = StartCoroutine(FadeBossOutRoutine(startFadeDelay, fadeDuration));
        }
        else
        {
            if (deathFadeRoutine != null)
                StopCoroutine(deathFadeRoutine);

            deathFadeRoutine = StartCoroutine(FadeBossOutRoutine(0f, 5f));
        }
    }

    private IEnumerator FadeBossOutRoutine(float delayBeforeFade, float fadeDuration)
    {
        if (delayBeforeFade > 0f)
            yield return new WaitForSeconds(delayBeforeFade);

        foreach (var kvp in originalMaterialColors)
            PrepareMaterialForFade(kvp.Key);

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, fadeDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float alpha = Mathf.Lerp(1f, 0f, t);

            foreach (var kvp in originalMaterialColors)
            {
                Material mat = kvp.Key;
                Color baseColor = kvp.Value;

                if (mat == null) continue;

                Color faded = baseColor;
                faded.a = alpha;

                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", faded);

                if (mat.HasProperty("_Color"))
                    mat.SetColor("_Color", faded);
            }

            yield return null;
        }

        foreach (Renderer rend in cachedRenderers)
        {
            if (rend != null)
                rend.enabled = false;
        }
    }

    private void PrepareMaterialForFade(Material mat)
    {
        if (mat == null)
            return;

        if (mat.HasProperty("_Surface"))
        {
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        if (mat.HasProperty("_Mode"))
        {
            mat.SetFloat("_Mode", 2f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
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
        FindPlayerIfNeeded();
        if (player != null)
            SnapFaceTarget(player.position);

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
        {
            GameObject audioObj = new GameObject("CircleSpawnSFX");
            audioObj.transform.position = spawnPos;

            AudioSource source = audioObj.AddComponent<AudioSource>();
            source.clip = circleSpawnClip;
            source.volume = circleSpawnVolume;
            source.outputAudioMixerGroup = sfxMixerGroup;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = audioMinDistance;
            source.maxDistance = audioMaxDistance;
            source.dopplerLevel = 0f;

            source.Play();
            Destroy(audioObj, circleSpawnClip.length + 0.25f);
        }

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
        float currentY = transform.position.y;

        for (int i = 0; i < teleportAttempts; i++)
        {
            Vector2 random2D = Random.insideUnitCircle * teleportSearchRadius;
            Vector3 randomPoint = transform.position + new Vector3(random2D.x, 0f, random2D.y);

            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, navMeshSampleDistance, NavMesh.AllAreas))
            {
                float yDiff = Mathf.Abs(hit.position.y - currentY);

                if (yDiff <= maxTeleportHeightDifference)
                {
                    chosenPosition = hit.position;
                    found = true;
                    break;
                }
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

        if (flatDir.sqrMagnitude <= 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(flatDir.normalized);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            faceTurnSpeed * Time.deltaTime
        );
    }

    private void SnapFaceTarget(Vector3 targetPosition)
    {
        Vector3 flatDir = targetPosition - transform.position;
        flatDir.y = 0f;

        if (flatDir.sqrMagnitude <= 0.001f)
            return;

        transform.rotation = Quaternion.LookRotation(flatDir.normalized);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebugRange) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, engageRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, introTriggerRange);
    }
}