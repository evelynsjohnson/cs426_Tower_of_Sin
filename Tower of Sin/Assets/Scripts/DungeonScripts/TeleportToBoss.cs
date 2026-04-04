using UnityEngine;
using UnityEngine.SceneManagement;

public class TeleportToBoss : MonoBehaviour
{
    public string bossSceneName = "Boss_Scene";
    public void Teleport()
    {
        int currentFloor = FloorTextController.floorNumber;
        if (currentFloor == 0 )
        {
            FloorTextController.floorNumber = 4;
        }
        else
        {
            FloorTextController.floorNumber = ((currentFloor / 5) + 1) * 5 - 1;
            // this formula isn't quite right because the floor num also gets updated somewhere else (?)
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