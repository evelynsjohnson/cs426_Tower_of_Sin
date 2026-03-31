using UnityEngine;

public class SettingsController : MonoBehaviour
{

    public GameObject optionsCanvas;

    private bool isOptionsCanvasOpen = false;

    void Start()
    {
        // Ensure the canvas is hidden when the game starts
        if (optionsCanvas != null)
        {
            optionsCanvas.SetActive(false);
        }
    }

    void Update()
    {
        if (Time.timeScale == 0f && !isOptionsCanvasOpen) return; // other UI is open
        if (Input.GetKeyDown(KeyCode.G))
        {
            ToggleAchievements();
        }
    }

    private void ToggleAchievements()
    {
        isOptionsCanvasOpen = !isOptionsCanvasOpen;

        optionsCanvas.SetActive(isOptionsCanvasOpen);

        if (isOptionsCanvasOpen)
        {
            Time.timeScale = 0f;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Time.timeScale = 1f;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}