using UnityEngine;
using UnityEngine.EventSystems;

public class GenericUIController : MonoBehaviour
{
    public GameObject myCanvas;

    public void OpenUI()
    {
        if (myCanvas == null || myCanvas.activeSelf) return;

        myCanvas.SetActive(true);
        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

    }

    public void CloseUI()
    {
        myCanvas.SetActive(false);
        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}