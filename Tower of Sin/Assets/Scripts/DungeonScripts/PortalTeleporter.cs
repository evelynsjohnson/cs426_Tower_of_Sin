using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PortalTeleporter : MonoBehaviour
{
    private string sceneToLoad;
    private bool isTeleporting = false;

    private void OnTriggerEnter(Collider other)
    {
        // <-- CHECK THE LOCK HERE
        if (other.CompareTag("Player") && !isTeleporting)
        {
            isTeleporting = true;

            int floorNumber = FloorTextController.floorNumber;
            Scene currentScene = SceneManager.GetActiveScene();
            string curSceneName = currentScene.name;

            if (curSceneName == "Dungeon_Scene")
            {
                if ((floorNumber + 1) % 5 == 0) // every 5th floor is a boss
                {
                    sceneToLoad = "Boss_Scene";
                }
                else // standard floor
                {
                    sceneToLoad = "Dungeon_Scene";
                }
            }
            else if (curSceneName == "Boss_Scene") // Boss to prison
            {
                sceneToLoad = "Prison_Scene";
            }
            else if (curSceneName == "Prison_Scene") // next 4 floors
            {
                sceneToLoad = "Dungeon_Scene";
            }
            else if (curSceneName == "Death_Realm") // Death to dungeon
            {
                sceneToLoad = "Dungeon_Scene";
                FloorTextController.floorNumber = 0;
            }

            SceneManager.LoadScene(sceneToLoad);
        }
    }
}