using UnityEngine;
using System.Collections.Generic;

public class PrisonZombieScript : MonoBehaviour
{
    public GameObject zombiePrefab;

    private List<GameObject> activeZombies = new List<GameObject>();

    public void SpawnZombies()
    {
        foreach (GameObject z in activeZombies)
        {
            if (z != null) Destroy(z);
        }
        activeZombies.Clear();

        foreach (Transform childSpawnPoint in transform)
        {
            if (Random.value >= 0.4f)
            {
                GameObject newZombie = Instantiate(zombiePrefab, childSpawnPoint.position, childSpawnPoint.rotation);
                activeZombies.Add(newZombie);
            }
        }
    }
}