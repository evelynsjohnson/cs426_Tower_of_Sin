using UnityEngine;
using UnityEngine.SceneManagement;

public class TeleportToBoss : MonoBehaviour
{
    public string bossSceneName = "Boss_Scene";
    public void Teleport()
    {
        int currentFloor = FloorTextController.floorNumber;

        if (bossSceneName == "Boss_Scene")
        {
            if (currentFloor == 0)
            {
                FloorTextController.floorNumber = 5;
            }
            else
            {
                FloorTextController.floorNumber = ((currentFloor / 5) + 1) * 5;
            }

        }


        SceneManager.LoadScene(bossSceneName);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Teleport();
        }
    }
}