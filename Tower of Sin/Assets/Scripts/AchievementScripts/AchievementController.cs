using UnityEngine;

public class AchievementController : MonoBehaviour
{
    public GameObject achievementCanvas;

    private bool isCanvasOpen = false;

    void Start()
    {
        if (achievementCanvas != null)
        {
            achievementCanvas.SetActive(false);
        }
    }

    // 🔥 This gets called by the UI button
    public void OnAchievementButtonClicked()
    {
        ToggleAchievements();
    }

    public void ToggleAchievements()
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