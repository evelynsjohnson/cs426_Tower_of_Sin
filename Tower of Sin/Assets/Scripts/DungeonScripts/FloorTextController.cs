using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FloorTextController : MonoBehaviour
{
    public TextMeshProUGUI floorText;

    public static int floorNumber = 0;

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdateUI(scene.name);
    }

    public void RefreshFloorText()
    {
        UpdateUI(SceneManager.GetActiveScene().name);
    }

    private void UpdateUI(string sceneName)
    {
        if (sceneName.Contains("Boss_Scene"))
        {
            floorText.text = "Floor " + floorNumber + " (Boss)";
        }
        else if (sceneName.Contains("Prison_Scene"))
        {
            floorText.text = "Prison (Next Floor: " + (floorNumber + 1) + ")";
        }
        else if (sceneName.Contains("Death_Realm"))
        {
            floorText.text = "Death Realm";
        }
        else if (sceneName.Contains("Dungeon_Scene"))
        {
            floorText.text = "Floor " + floorNumber;
        }
        else
        {
            floorText.text = "";
        }
    }
}