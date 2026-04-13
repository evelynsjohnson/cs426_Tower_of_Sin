using UnityEngine;

public class BossSpawner : MonoBehaviour
{
    public GameObject[] bossPrefabs;

    void Start()
    {
        if (bossPrefabs == null || bossPrefabs.Length == 0)
        {
            Debug.LogWarning("BossSpawner: no boss prefabs assigned.");
            return;
        }

        GameObject chosen = bossPrefabs[Random.Range(0, bossPrefabs.Length)];

        foreach (Transform spawnPoint in transform)
        {
            Instantiate(chosen, spawnPoint.position, spawnPoint.rotation);
        }
    }
}
