using UnityEngine;
using UnityEngine.SceneManagement;

public class TeleportToBoss : MonoBehaviour
{
    public string bossSceneName = "Boss_Scene";
    public void Teleport()
    {
        int currentFloor = FloorTextController.floorNumber;
        FloorTextController.floorNumber = ((currentFloor / 5) + 1) * 5;

        Debug.Log("Teleporting to Boss! New Floor Set To: " + FloorTextController.floorNumber);

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