using UnityEngine;
using UnityEngine.SceneManagement;

public class PortalTeleporter : MonoBehaviour
{
    private string sceneToLoad;

    private static float lastTeleportTime = -999f; // persists across scenes
    private float teleportCooldown = 5f;

    private void OnTriggerEnter(Collider other)
    {
        FirstPersonMovement player = other.GetComponentInParent<FirstPersonMovement>();

        if (player == null)
            return;

        // only let the actual root object trigger it
        if (player.gameObject != other.transform.root.gameObject)
            return;

        if (Time.time - lastTeleportTime < teleportCooldown)
            return;

        lastTeleportTime = Time.time;

        string curSceneName = SceneManager.GetActiveScene().name;

        if (curSceneName == "Dungeon_Scene")
        {
            FloorTextController.floorNumber++;

            if (FloorTextController.floorNumber % 5 == 0)
                sceneToLoad = "Boss_Scene";
            else
                sceneToLoad = "Dungeon_Scene";

            Debug.Log("Teleported to dungeon scene: " + FloorTextController.floorNumber);
        }
        else if (curSceneName == "Boss_Scene")
        {
            sceneToLoad = "Prison_Scene";
        }
        else if (curSceneName == "Prison_Scene")
        {
            FloorTextController.floorNumber++;
            sceneToLoad = "Dungeon_Scene";
        }
        else if (curSceneName == "Death_Realm")
        {
            FloorTextController.floorNumber = 0;
            sceneToLoad = "Prison_Scene";
        }

        SceneManager.LoadScene(sceneToLoad);
    }
}