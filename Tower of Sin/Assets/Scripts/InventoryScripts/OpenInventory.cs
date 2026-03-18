using UnityEngine;

public class OpenInventory : MonoBehaviour
{
    public Transform player;
    public float activationDistance = 3f;
    public GameObject objectToShow;

    void Update()
    {
        if (player == null || objectToShow == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (!objectToShow.activeSelf && distance <= activationDistance && Input.GetKeyDown(KeyCode.E))
        {
            objectToShow.SetActive(true);
        }
    }
}