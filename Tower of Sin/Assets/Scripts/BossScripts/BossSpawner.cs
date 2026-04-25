using UnityEngine;

public class BossSpawner : MonoBehaviour
{
    [Header("Boss Pool")]
    public GameObject[] bossPrefabs;
    private int currentFloor => FloorTextController.floorNumber;

    [Header("Spawn Points")]
    public Transform bossSpawnPoint;
    public Transform bossSpawnPointLedge;
    public string gluttonyNameContains = "Gluttony";
    public string greedNameContains = "piratesking_skeleton";

    [Header("Scene References")]
    public Transform roomCenter;
    public BossArenaController bossArenaController;

    private void Start()
    {
        if (bossPrefabs == null || bossPrefabs.Length == 0 || bossSpawnPoint == null || bossSpawnPointLedge == null)
        {
            Debug.LogWarning("BossSpawner is missing references.");
            return;
        }

        if (bossArenaController != null)
            bossArenaController.ResetArenaInstant();

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

        EnvyAI envyAI = spawnedBoss.GetComponent<EnvyAI>();
        if (envyAI == null)
            envyAI = spawnedBoss.GetComponentInChildren<EnvyAI>();

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

        if (envyAI != null)
        {
            envyAI.SetFloor(currentFloor);
            envyAI.SetArenaController(bossArenaController);
        }
        else if (greedAI != null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

            greedAI.SetFloor(currentFloor);
            greedAI.SetArenaController(bossArenaController);
        }
        //else if (slothAI != null)
        //{
        //    slothAI.SetFloor(currentFloor);
        //    Debug.LogWarning("SlothAI still needs to be refactored to use BossArenaController.");
        //}
        //else if (lustAI != null)
        //{

        //    lustAI.SetFloor(currentFloor);
        //    lustAI.SetupArenaReferences(
        //        bossHealthBarFill,
        //        bossHealthText,
        //        bossHealthUIRoot,
        //        arenaLights,
        //        basementDoorLeft,
        //        basementDoorRight,
        //        gateAudioSource,
        //        largeGateClip,
        //        bossChestPrefab,
        //        bossChestSpawnPoint,
        //        doorMoveDistanceZ,
        //        doorMoveDuration
        //    );
        //}
        //else if (prideAI != null)
        //{
        //    prideAI.SetupArenaReferences(
        //        bossHealthBarFill,
        //        bossHealthText,
        //        bossHealthUIRoot,
        //        arenaLights,
        //        basementDoorLeft,
        //        basementDoorRight,
        //        gateAudioSource,
        //        largeGateClip,
        //        bossChestPrefab,
        //        bossChestSpawnPoint,
        //        doorMoveDistanceZ,
        //        doorMoveDuration
        //    );
        //}
        //else if (wrathAI != null)
        //{
        //    wrathAI.SetupArenaReferences(
        //        bossHealthBarFill,
        //        bossHealthText,
        //        bossHealthUIRoot,
        //        arenaLights,
        //        basementDoorLeft,
        //        basementDoorRight,
        //        gateAudioSource,
        //        largeGateClip,
        //        bossChestPrefab,
        //        bossChestSpawnPoint,
        //        doorMoveDistanceZ,
        //        doorMoveDuration
        //    );
        //}
        else
        {
            Debug.LogWarning("No supported boss AI script found on spawned boss: " + spawnedBoss.name);
        }
    }
}