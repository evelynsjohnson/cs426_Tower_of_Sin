using UnityEngine;

public class FirstPersonLook : MonoBehaviour
{
    [SerializeField] Transform character;
    public float sensitivity = 2f;
    public float smoothing = 1.5f;

    Vector2 velocity;
    Vector2 frameVelocity;

    FirstPersonMovement movement;

    void Reset()
    {
        FirstPersonMovement fpm = GetComponentInParent<FirstPersonMovement>();
        if (fpm != null)
            character = fpm.transform;
    }

    void Awake()
    {
        movement = GetComponentInParent<FirstPersonMovement>();

        if (character == null && movement != null)
            character = movement.transform;
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (character != null)
            velocity.x = character.localRotation.eulerAngles.y;
    }

    void Update()
    {
        if (movement != null && movement.uiMode)
        {
            frameVelocity = Vector2.zero;
            return;
        }

        if (Time.timeScale == 0f) return;

        Vector2 mouseDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        Vector2 rawFrameVelocity = Vector2.Scale(mouseDelta, Vector2.one * sensitivity);
        frameVelocity = Vector2.Lerp(frameVelocity, rawFrameVelocity, 1f / smoothing);
        velocity += frameVelocity;
        velocity.y = Mathf.Clamp(velocity.y, -90f, 90f);

        transform.localRotation = Quaternion.AngleAxis(-velocity.y, Vector3.right);

        if (character != null)
            character.localRotation = Quaternion.AngleAxis(velocity.x, Vector3.up);
    }

    public void SetLookRotation(Quaternion worldRotation)
    {
        Vector3 euler = worldRotation.eulerAngles;

        float yaw = euler.y;
        float pitch = euler.x;

        if (pitch > 180f)
            pitch -= 360f;

        velocity.x = yaw;
        velocity.y = Mathf.Clamp(-pitch, -90f, 90f);

        frameVelocity = Vector2.zero;

        transform.localRotation = Quaternion.AngleAxis(-velocity.y, Vector3.right);

        if (character != null)
            character.localRotation = Quaternion.AngleAxis(velocity.x, Vector3.up);
    }
}