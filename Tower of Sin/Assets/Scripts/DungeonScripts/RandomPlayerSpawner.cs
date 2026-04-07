using UnityEngine;
using UnityEngine.SceneManagement;

public class RandomPlayerSpawner : MonoBehaviour
{
    public Transform[] spawnPoints;

    public void SpawnPlayerRandomly()
    {
        if (SceneManager.GetActiveScene().name == "Dungeon_Scene")
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null && spawnPoints.Length > 0)
            {
                int randomIndex = Random.Range(0, spawnPoints.Length);
                Transform spawn = spawnPoints[randomIndex];

                CharacterController cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;

                player.transform.position = spawn.position;
                // temporarily remove rotation line for debugging

                if (cc != null) cc.enabled = true;
            }
        }
    }
}