using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossSpawner : MonoBehaviour
{
    public GameObject[] bossPrefabs;
    public int currentFloor = 5;

    public Transform bossSpawnPoint;
    public Transform bossSpawnPointLedge;

    public Light bossLight;
    public Transform basementDoorLeft;
    public Transform basementDoorRight;
    public AudioSource gateAudioSource;
    public AudioClip largeGateClip;

    public GameObject bossChestPrefab;
    public Transform bossChestSpawnPoint;

    public Image bossHealthBarFill;
    public TMP_Text bossHealthText;
    public GameObject bossHealthUIRoot;

    public float doorMoveDistanceZ = 1f;
    public float doorMoveDuration = 1f;
    public string gluttonyNameContains = "Gluttony";

    private void Start()
    {
        if (bossPrefabs == null || bossPrefabs.Length == 0)
        {
            Debug.LogWarning("BossSpawner: no boss prefabs assigned.");
            return;
        }

        if (bossSpawnPoint == null)
        {
            Debug.LogWarning("BossSpawner: bossSpawnPoint is not assigned.");
            return;
        }

        if (bossSpawnPointLedge == null)
        {
            Debug.LogWarning("BossSpawner: bossSpawnPointLedge is not assigned.");
            return;
        }

        GameObject chosen = bossPrefabs[Random.Range(0, bossPrefabs.Length)];

        bool isGluttony =
            chosen != null &&
            chosen.name.ToLower().Contains(gluttonyNameContains.ToLower());

        Transform selectedSpawnPoint = isGluttony ? bossSpawnPointLedge : bossSpawnPoint;

        GameObject spawnedBoss = Instantiate(
            chosen,
            selectedSpawnPoint.position,
            selectedSpawnPoint.rotation
        );

        AngelBossAI bossAI = spawnedBoss.GetComponent<AngelBossAI>();
        if (bossAI == null)
            bossAI = spawnedBoss.GetComponentInChildren<AngelBossAI>();

        if (bossAI == null)
        {
            Debug.LogWarning("BossSpawner: Spawned boss is missing AngelBossAI.");
            return;
        }

        bossAI.SetFloor(currentFloor);

        bossAI.SetupArenaReferences(
            bossLight,
            basementDoorLeft,
            basementDoorRight,
            gateAudioSource,
            largeGateClip,
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