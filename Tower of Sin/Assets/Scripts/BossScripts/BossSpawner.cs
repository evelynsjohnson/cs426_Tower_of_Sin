using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossSpawner : MonoBehaviour
{
    public GameObject[] bossPrefabs;
    public int currentFloor = 5;

    public Transform bossSpawnPoint;
    public Transform bossSpawnPointGreed;
    public Transform bossSpawnPointLedge;
    public string gluttonyNameContains = "Gluttony";

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

    private void Start()
    {
        if (bossPrefabs == null ||  bossPrefabs.Length == 0 || bossSpawnPoint == null || bossSpawnPointLedge == null || bossSpawnPointGreed == null)
        {
            Debug.LogWarning("There's an error.");
            return;
        }

        GameObject chosen = bossPrefabs[Random.Range(0, bossPrefabs.Length)];

        bool isGreed =
            chosen != null &&
            chosen.name.ToLower().Contains("PiratesKing_Skeleton");

        bool isGluttony =
            chosen != null &&
            chosen.name.ToLower().Contains(gluttonyNameContains.ToLower());

        Transform selectedSpawnPoint = bossSpawnPoint;

        if (isGreed)
            selectedSpawnPoint = bossSpawnPointGreed;
        else if (isGluttony)
            selectedSpawnPoint = bossSpawnPointLedge;


        GameObject spawnedBoss = Instantiate(
            chosen,
            selectedSpawnPoint.position,
            selectedSpawnPoint.rotation
        );

        EnvyAI bossAI = spawnedBoss.GetComponent<EnvyAI>();

        if (bossAI == null)
            bossAI = spawnedBoss.GetComponentInChildren<EnvyAI>();

        Light[] arenaLights = new Light[0];
        if (lightsRoot != null)
            arenaLights = lightsRoot.GetComponentsInChildren<Light>(true);

        bossAI.SetFloor(currentFloor);

        bossAI.SetupArenaReferences(
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
}