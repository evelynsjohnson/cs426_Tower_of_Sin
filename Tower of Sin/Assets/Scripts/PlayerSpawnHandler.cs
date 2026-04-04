using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSpawnHandler : MonoBehaviour
{
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SpawnPlayer();
    }

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
}