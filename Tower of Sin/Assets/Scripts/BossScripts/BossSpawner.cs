using UnityEngine;

public class BossSpawner : MonoBehaviour
{
    public GameObject bossPrefab;

    void Start()
    {
        // Loop through all child objects attached to this spawner
        foreach (Transform bossSpawnPoint in transform)
        {
            Instantiate(bossPrefab, bossSpawnPoint.position, bossSpawnPoint.rotation);
        }

    }
}