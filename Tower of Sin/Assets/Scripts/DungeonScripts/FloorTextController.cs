using TMPro;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FloorTextController : MonoBehaviour
{
    public TextMeshProUGUI floorText;

    public static int floorNumber = 0;

    void Start()
    {
        UnityEngine.SceneManagement.Scene currentScene = SceneManager.GetActiveScene();
        string curSceneName = currentScene.name;

        UpdateFloorText(curSceneName);
        UpdateUI(curSceneName);
    }

    public void UpdateFloorText(string sceneName)
    {
        if (sceneName.Contains("Dungeon_Scene") || sceneName.Contains("Boss_Scene"))
        {
            floorNumber++;
        }

        UpdateUI(sceneName);
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