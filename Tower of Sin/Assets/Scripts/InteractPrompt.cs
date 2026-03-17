using UnityEngine;

public class InteractPrompt : MonoBehaviour
{
    [Header("Settings")]
    public Transform player;
    public float activationDistance = 3f;
    public bool lockYAxis = true;

    private SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (player == null && Camera.main != null)
            player = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (player == null) return;

        // 1. Distance Check
        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= activationDistance)
        {
            spriteRenderer.enabled = true;

            if (lockYAxis)
            {
                Vector3 targetPos = new Vector3(player.position.x, transform.position.y, player.position.z);
                transform.LookAt(targetPos);
                transform.Rotate(0, 180, 0);
            }
            else
            {
                transform.LookAt(transform.position + player.forward);
            }
        }
        else
        {
            spriteRenderer.enabled = false;
        }
    }
}