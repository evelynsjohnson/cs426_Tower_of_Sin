using UnityEngine;

public class PlayerSpawnHandler : MonoBehaviour
{
    public Transform spawnPoint;

    void Start()
    {
        SpawnPlayer();
    }

    public void SpawnPlayer()
    {
        GameObject player = GameObject.FindWithTag("Player");

        if (player == null)
        {
            Debug.LogWarning("No player found with tag Player");
            return;
        }

        if (spawnPoint == null)
        {
            Debug.LogWarning("Spawn point not assigned");
            return;
        }

        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
        }

        player.transform.position = spawnPoint.position;
        player.transform.rotation = Quaternion.Euler(0f, spawnPoint.eulerAngles.y, 0f);

        if (cc != null)
        {
            cc.enabled = true;
        }

        Debug.Log("Spawned player at " + spawnPoint.name);
    }
}