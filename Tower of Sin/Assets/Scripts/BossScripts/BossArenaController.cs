using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossArenaController : MonoBehaviour
{
    [Header("Doors")]
    [SerializeField] private Transform basementDoorLeft;
    [SerializeField] private Transform basementDoorRight;
    [SerializeField] private float doorMoveDistanceZ = 1f;
    [SerializeField] private float doorMoveDuration = 3f;
    [SerializeField] private bool moveDoorsInLocalSpace = true;

    [Header("Gate Audio")]
    [SerializeField] private AudioSource gateAudioSource;
    [SerializeField] private AudioClip largeGateClip;

    [Header("Lights")]
    [SerializeField] private Transform lightsRoot;
    [SerializeField] private Color defaultLightColor = Color.white;
    [SerializeField] private float defaultLightIntensityMultiplier = 1f;

    [Header("Chest")]
    [SerializeField] private GameObject bossChestPrefab;
    [SerializeField] private Transform bossChestSpawnPoint;

    [Header("Music")]
    [SerializeField] private AudioSource backgroundMusicSource;
    [SerializeField] private AudioClip defaultMusicClip;
    [SerializeField][Range(0f, 1f)] private float defaultMusicVolume = 1f;

    [Header("Boss UI")]
    [SerializeField] private Image bossHealthBarFill;
    [SerializeField] private TMP_Text bossHealthText;
    [SerializeField] private GameObject bossHealthUIRoot;

    private Light[] arenaLights = new Light[0];
    private float[] originalLightIntensities = new float[0];

    private Vector3 leftDoorClosedLocalPos;
    private Vector3 rightDoorClosedLocalPos;
    private Vector3 leftDoorClosedWorldPos;
    private Vector3 rightDoorClosedWorldPos;

    private Coroutine leftDoorRoutine;
    private Coroutine rightDoorRoutine;

    private bool chestSpawned = false;
    private AudioClip previousMusicClip;
    private float previousMusicVolume = 1f;

    private void Awake()
    {
        CacheDoorClosedPositions();
        CacheLights();

        if (backgroundMusicSource != null)
        {
            previousMusicClip = backgroundMusicSource.clip;
            previousMusicVolume = backgroundMusicSource.volume;
        }

        ResetArenaInstant();
    }

    private void CacheDoorClosedPositions()
    {
        if (basementDoorLeft != null)
        {
            leftDoorClosedLocalPos = basementDoorLeft.localPosition;
            leftDoorClosedWorldPos = basementDoorLeft.position;
        }

        if (basementDoorRight != null)
        {
            rightDoorClosedLocalPos = basementDoorRight.localPosition;
            rightDoorClosedWorldPos = basementDoorRight.position;
        }
    }

    private void CacheLights()
    {
        if (lightsRoot != null)
            arenaLights = lightsRoot.GetComponentsInChildren<Light>(true);
        else
            arenaLights = new Light[0];

        originalLightIntensities = new float[arenaLights.Length];

        for (int i = 0; i < arenaLights.Length; i++)
        {
            if (arenaLights[i] == null) continue;
            originalLightIntensities[i] = arenaLights[i].intensity;
        }
    }

    public void ResetArenaInstant()
    {
        chestSpawned = false;

        CloseDoorsInstant();
        ResetLightsToDefault();
        RestoreDefaultMusic();

        if (bossHealthUIRoot != null)
            bossHealthUIRoot.SetActive(false);
    }

    public void OnBossSpawned(Color bossLightColor, float bossLightIntensityMultiplier, AudioClip bossMusicClip, float bossMusicVolume = 1f)
    {
        chestSpawned = false;

        CloseDoorsInstant();
        ApplyBossLights(bossLightColor, bossLightIntensityMultiplier);
        PlayBossMusic(bossMusicClip, bossMusicVolume);

        if (bossHealthUIRoot != null)
            bossHealthUIRoot.SetActive(true);
    }

    public void OnBossDied()
    {
        OpenDoors();
        SpawnBossChestOnce();
        ResetLightsToDefault();
        RestoreDefaultMusic();

        if (bossHealthUIRoot != null)
            bossHealthUIRoot.SetActive(false);
    }

    private void CloseDoorsInstant()
    {
        if (basementDoorLeft != null)
        {
            if (moveDoorsInLocalSpace)
                basementDoorLeft.localPosition = leftDoorClosedLocalPos;
            else
                basementDoorLeft.position = leftDoorClosedWorldPos;
        }

        if (basementDoorRight != null)
        {
            if (moveDoorsInLocalSpace)
                basementDoorRight.localPosition = rightDoorClosedLocalPos;
            else
                basementDoorRight.position = rightDoorClosedWorldPos;
        }
    }

    private void OpenDoors()
    {
        if (gateAudioSource != null)
        {
            if (largeGateClip != null)
                gateAudioSource.PlayOneShot(largeGateClip);
            else
                gateAudioSource.Play();
        }

        if (basementDoorLeft != null)
        {
            if (leftDoorRoutine != null) StopCoroutine(leftDoorRoutine);
            leftDoorRoutine = StartCoroutine(MoveDoorZ(basementDoorLeft, -doorMoveDistanceZ, doorMoveDuration));
        }

        if (basementDoorRight != null)
        {
            if (rightDoorRoutine != null) StopCoroutine(rightDoorRoutine);
            rightDoorRoutine = StartCoroutine(MoveDoorZ(basementDoorRight, doorMoveDistanceZ, doorMoveDuration));
        }
    }

    private IEnumerator MoveDoorZ(Transform door, float zOffset, float duration)
    {
        if (door == null) yield break;

        Vector3 start = moveDoorsInLocalSpace ? door.localPosition : door.position;
        Vector3 end = start + new Vector3(0f, 0f, zOffset);

        float t = 0f;
        float safeDuration = Mathf.Max(0.0001f, duration);

        while (t < safeDuration)
        {
            t += Time.deltaTime;
            Vector3 next = Vector3.Lerp(start, end, t / safeDuration);

            if (moveDoorsInLocalSpace)
                door.localPosition = next;
            else
                door.position = next;

            yield return null;
        }

        if (moveDoorsInLocalSpace)
            door.localPosition = end;
        else
            door.position = end;
    }

    private void ApplyBossLights(Color bossLightColor, float intensityMultiplier)
    {
        for (int i = 0; i < arenaLights.Length; i++)
        {
            if (arenaLights[i] == null) continue;

            arenaLights[i].color = bossLightColor;
            arenaLights[i].intensity = originalLightIntensities[i] * intensityMultiplier;
        }
    }

    private void ResetLightsToDefault()
    {
        for (int i = 0; i < arenaLights.Length; i++)
        {
            if (arenaLights[i] == null) continue;

            float original = (i < originalLightIntensities.Length) ? originalLightIntensities[i] : 1f;
            arenaLights[i].color = defaultLightColor;
            arenaLights[i].intensity = original * defaultLightIntensityMultiplier;
        }
    }

    private void SpawnBossChestOnce()
    {
        if (chestSpawned || bossChestPrefab == null || bossChestSpawnPoint == null)
            return;

        Instantiate(bossChestPrefab, bossChestSpawnPoint.position, bossChestSpawnPoint.rotation);
        chestSpawned = true;
    }

    private void PlayBossMusic(AudioClip bossMusicClip, float bossMusicVolume)
    {
        if (backgroundMusicSource == null || bossMusicClip == null)
            return;

        previousMusicClip = backgroundMusicSource.clip;
        previousMusicVolume = backgroundMusicSource.volume;

        backgroundMusicSource.clip = bossMusicClip;
        backgroundMusicSource.loop = true;
        backgroundMusicSource.volume = bossMusicVolume;
        backgroundMusicSource.Play();
    }

    private void RestoreDefaultMusic()
    {
        if (backgroundMusicSource == null)
            return;

        AudioClip clipToUse = defaultMusicClip != null ? defaultMusicClip : previousMusicClip;
        float volumeToUse = defaultMusicClip != null ? defaultMusicVolume : previousMusicVolume;

        backgroundMusicSource.clip = clipToUse;
        backgroundMusicSource.loop = true;
        backgroundMusicSource.volume = volumeToUse;

        if (clipToUse != null)
            backgroundMusicSource.Play();
    }

    public Image GetBossHealthBarFill() => bossHealthBarFill;
    public TMP_Text GetBossHealthText() => bossHealthText;
    public GameObject GetBossHealthUIRoot() => bossHealthUIRoot;
}