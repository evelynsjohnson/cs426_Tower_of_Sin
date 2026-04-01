using UnityEngine;

public class PrisonZombieScript : MonoBehaviour
{
    public GameObject zombiePrefab;

    void Start()
    {
        // Loop through all child objects attached to this spawner
        foreach (Transform childSpawnPoint in transform)
        {
            // chance to spawn a zombie
            // 1.0f is 0%, 0.0f is 100% chance to spawn
            if (Random.value >= 0.4f)
            {
                Instantiate(zombiePrefab, childSpawnPoint.position, childSpawnPoint.rotation);
            }
        }
    }
}