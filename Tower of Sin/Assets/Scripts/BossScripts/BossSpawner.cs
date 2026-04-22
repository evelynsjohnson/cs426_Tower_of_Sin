using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossSpawner : MonoBehaviour
{
    public GameObject[] bossPrefabs;
    public int currentFloor = 5;

    public Transform bossSpawnPoint;
    public Transform bossSpawnPointLedge;
    public string gluttonyNameContains = "Gluttony";
    public string greedNameContains = "piratesking_skeleton";

    public Transform lightsRoot;
    public Transform basementDoorLeft;
    public Transform basementDoorRight;
    public AudioSource gateAudioSource;
    public AudioClip largeGateClip;

    [Header("Boss Music")]
    public AudioSource backgroundMusicSource;

    public GameObject bossChestPrefab;
    public Transform bossChestSpawnPoint;

    public Image bossHealthBarFill;
    public TMP_Text bossHealthText;
    public GameObject bossHealthUIRoot;

    public float doorMoveDistanceZ = 1f;
    public float doorMoveDuration = 3f;

    public Transform roomCenter;

    private void Start()
    {
        if (bossPrefabs == null || bossPrefabs.Length == 0 || bossSpawnPoint == null || bossSpawnPointLedge == null)
        {
            Debug.LogWarning("BossSpawner is missing references.");
            return;
        }

        GameObject chosen = bossPrefabs[Random.Range(0, bossPrefabs.Length)];

        if (chosen == null)
        {
            Debug.LogWarning("Chosen boss prefab was null.");
            return;
        }

        string chosenName = chosen.name.ToLower();

        bool isGreed = chosenName.Contains(greedNameContains.ToLower());
        bool isGluttony = chosenName.Contains(gluttonyNameContains.ToLower());

        Vector3 spawnPosition = bossSpawnPoint.position;
        Quaternion spawnRotation = bossSpawnPoint.rotation;

        if (isGreed)
        {
            spawnPosition = bossSpawnPoint.position + new Vector3(-5f, 0f, 0f);
            spawnRotation = bossSpawnPoint.rotation;
        }
        else if (isGluttony)
        {
            spawnPosition = bossSpawnPointLedge.position;
            spawnRotation = bossSpawnPointLedge.rotation;
        }

        GameObject spawnedBoss = Instantiate(chosen, spawnPosition, spawnRotation);

        // Try EnvyAI first
        EnvyAI envyAI = spawnedBoss.GetComponent<EnvyAI>();
        if (envyAI == null)
            envyAI = spawnedBoss.GetComponentInChildren<EnvyAI>();

        // Try other boss AIs if EnvyAI wasn't found
        GreedAI greedAI = null;
        SlothAI slothAI = null;
        LustAI lustAI = null;
        PrideAI prideAI = null;
        WrathAI wrathAI = null;
        if (envyAI == null)
        {
            greedAI = spawnedBoss.GetComponent<GreedAI>();
            if (greedAI == null)
                greedAI = spawnedBoss.GetComponentInChildren<GreedAI>();

            if (greedAI == null)
            {
                slothAI = spawnedBoss.GetComponent<SlothAI>();
                if (slothAI == null)
                    slothAI = spawnedBoss.GetComponentInChildren<SlothAI>();
            }

            if (greedAI == null && slothAI == null)
            {
                lustAI = spawnedBoss.GetComponent<LustAI>();
                if (lustAI == null)
                    lustAI = spawnedBoss.GetComponentInChildren<LustAI>();

                if (lustAI == null)
                {
                    prideAI = spawnedBoss.GetComponent<PrideAI>();
                    if (prideAI == null)
                        prideAI = spawnedBoss.GetComponentInChildren<PrideAI>();

                    if (prideAI == null)
                    {
                        wrathAI = spawnedBoss.GetComponent<WrathAI>();
                        if (wrathAI == null)
                            wrathAI = spawnedBoss.GetComponentInChildren<WrathAI>();
                    }
                }
            }
        }

        Light[] arenaLights = new Light[0];
        if (lightsRoot != null)
            arenaLights = lightsRoot.GetComponentsInChildren<Light>(true);

        if (envyAI != null)
        {
            envyAI.SetFloor(currentFloor);

            envyAI.SetupArenaReferences(
                arenaLights,
                basementDoorLeft,
                basementDoorRight,
                gateAudioSource,
                largeGateClip,
                backgroundMusicSource,
                bossChestPrefab,
                bossChestSpawnPoint,
                bossHealthBarFill,
                bossHealthText,
                bossHealthUIRoot,
                doorMoveDistanceZ,
                doorMoveDuration
            );
        }
        else if (greedAI != null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

            greedAI.SetFloor(currentFloor);

            greedAI.SetSceneReferences(
                playerObj != null ? playerObj.transform : null,
                bossSpawnPoint,
                bossSpawnPointLedge,
                roomCenter,
                bossHealthBarFill,
                bossHealthText,
                bossHealthUIRoot
            );

            greedAI.SetupArenaReferences(
                arenaLights,
                basementDoorLeft,
                basementDoorRight,
                gateAudioSource,
                largeGateClip,
                backgroundMusicSource,
                bossChestPrefab,
                bossChestSpawnPoint,
                bossHealthBarFill,
                bossHealthText,
                bossHealthUIRoot,
                doorMoveDistanceZ,
                doorMoveDuration
            );
        }
        else if (slothAI != null)
        {
            slothAI.SetFloor(currentFloor);
            slothAI.SetupArenaReferences(
                bossHealthBarFill,
                bossHealthText,
                bossHealthUIRoot,
                arenaLights,
                basementDoorLeft,
                basementDoorRight,
                gateAudioSource,
                largeGateClip,
                bossChestPrefab,
                bossChestSpawnPoint,
                doorMoveDistanceZ,
                doorMoveDuration
            );
        }
        else if (lustAI != null)
        {
            lustAI.SetFloor(currentFloor);
            lustAI.SetupArenaReferences(
                bossHealthBarFill,
                bossHealthText,
                bossHealthUIRoot,
                arenaLights,
                basementDoorLeft,
                basementDoorRight,
                gateAudioSource,
                largeGateClip,
                bossChestPrefab,
                bossChestSpawnPoint,
                doorMoveDistanceZ,
                doorMoveDuration
            );
        }
        else if (prideAI != null)
        {
            prideAI.SetFloor(currentFloor);
            prideAI.SetupArenaReferences(
                bossHealthBarFill,
                bossHealthText,
                bossHealthUIRoot,
                arenaLights,
                basementDoorLeft,
                basementDoorRight,
                gateAudioSource,
                largeGateClip,
                bossChestPrefab,
                bossChestSpawnPoint,
                doorMoveDistanceZ,
                doorMoveDuration
            );
        }
        else if (wrathAI != null)
        {
            wrathAI.SetFloor(currentFloor);
            wrathAI.SetupArenaReferences(
                bossHealthBarFill,
                bossHealthText,
                bossHealthUIRoot,
                arenaLights,
                basementDoorLeft,
                basementDoorRight,
                gateAudioSource,
                largeGateClip,
                bossChestPrefab,
                bossChestSpawnPoint,
                doorMoveDistanceZ,
                doorMoveDuration
            );
        }
        else
        {
            Debug.LogWarning("No supported boss AI script found on spawned boss: " + spawnedBoss.name);
        }
    }
}
