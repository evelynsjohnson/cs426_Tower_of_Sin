using UnityEngine;

public class GenericUIController : MonoBehaviour
{
    public GameObject myCanvas;
    private bool isOpen = false;

    void Start()
    {
        if (myCanvas != null) myCanvas.SetActive(false);
    }

    public void OpenUI()
    {
        if (isOpen) return;

        isOpen = true;
        myCanvas.SetActive(true);

        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseUI()
    {
        isOpen = false;
        myCanvas.SetActive(false);

        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}