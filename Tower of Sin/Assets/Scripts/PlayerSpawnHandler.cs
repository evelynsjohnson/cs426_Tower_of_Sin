using UnityEngine;

public class PlayerSpawnHandler : MonoBehaviour
{
    private void Start()
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
        else
        {
            Debug.LogWarning("PlayerSpawnHandler: Could not find an object tagged 'Player' in this scene!");
        }
    }
}