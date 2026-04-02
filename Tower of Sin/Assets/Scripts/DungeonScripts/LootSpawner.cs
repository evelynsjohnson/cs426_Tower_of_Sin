using UnityEngine;

public class LootSpawner : MonoBehaviour
{
    public Transform chestsContainer;

    [Range(0f, 100f)]
    public float spawnChance = 20f;

    public void RandomizeLoot()
    {
        if (chestsContainer == null) return;

        foreach (Transform chest in chestsContainer)
        {
            float roll = Random.Range(0f, 100f);

            if (roll <= spawnChance)
            {
                chest.gameObject.SetActive(true);
            }
            else
            {
                chest.gameObject.SetActive(false);
            }
        }
    }
}