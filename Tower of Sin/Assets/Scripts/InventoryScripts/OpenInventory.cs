using UnityEngine;

public class OpenInventory : MonoBehaviour
{
    public Transform player;
    public float activationDistance = 3f;
    public GameObject objectToShow;

    void Update()
    {
        if (player == null || objectToShow == null) return;

        if (Time.timeScale == 0f && !objectToShow.activeSelf) return; // other UI is open

        float distance = Vector3.Distance(transform.position, player.position);

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (!objectToShow.activeSelf && distance <= activationDistance)
            {
                OpenTheInventory();
            }
            else if (objectToShow.activeSelf)
            {
                CloseTheInventory();
            }
        }
    }

    private void OpenTheInventory()
    {
        objectToShow.SetActive(true);

        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void CloseTheInventory()
    {
        objectToShow.SetActive(false);

        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}