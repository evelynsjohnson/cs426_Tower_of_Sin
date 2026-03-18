using UnityEngine;

public class RandomPlayerSpawner : MonoBehaviour
{
    public Transform[] spawnPoints;

    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null && spawnPoints.Length > 0)
        {
            int randomIndex = Random.Range(0, spawnPoints.Length);

            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            player.transform.position = spawnPoints[randomIndex].position;

            player.transform.rotation = spawnPoints[randomIndex].rotation;

            if (cc != null) cc.enabled = true;
        }
    }
}