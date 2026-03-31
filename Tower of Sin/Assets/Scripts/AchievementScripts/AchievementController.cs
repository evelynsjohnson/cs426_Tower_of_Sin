using UnityEngine;

public class AchievementController : MonoBehaviour
{
    public GameObject achievementCanvas;

    private bool isCanvasOpen = false;

    void Start()
    {
        // Ensure the canvas is hidden when the game starts
        if (achievementCanvas != null)
        {
            achievementCanvas.SetActive(false);
        }
    }

    void Update()
    {
        if (Time.timeScale == 0f && !isCanvasOpen) return;
        if (Input.GetKeyDown(KeyCode.T))
        {
            ToggleAchievements();
        }
    }

    private void ToggleAchievements()
    {
        isCanvasOpen = !isCanvasOpen;

        achievementCanvas.SetActive(isCanvasOpen);

        if (isCanvasOpen)
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