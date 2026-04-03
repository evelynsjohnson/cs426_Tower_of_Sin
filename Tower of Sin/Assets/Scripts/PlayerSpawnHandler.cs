using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSpawnHandler : MonoBehaviour
{
    public void SpawnPlayer()
    {
        GameObject player = GameObject.FindWithTag("Player");

        if (player != null)
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
            }

            player.transform.position = transform.position;
            player.transform.rotation = transform.rotation;

            if (cc != null)
            {
                cc.enabled = true;
            }
        }
    }

    private void Start()
    {
        SpawnPlayer();
    }

    private void Update()
    {
        //This code was making player not move in boss scene so I commented it out while testing

        if (SceneManager.GetActiveScene().name == "Boss_Scene")
        {
            GameObject player = GameObject.FindWithTag("Player");

            if (player != null)
            {
                CharacterController cc = player.GetComponent<CharacterController>();
                if (cc != null)
                {
                    cc.enabled = false;
                }

                player.transform.position = transform.position;
                player.transform.rotation = transform.rotation;

                if (cc != null)
                {
                    cc.enabled = true;
                }
            }
        }
    }
}