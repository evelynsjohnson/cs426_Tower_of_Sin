using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class OpenDoor : MonoBehaviour
{
    [Tooltip("The position the door will move to when opened (relative to current position).")]
    public Vector3 openPositionOffset = new Vector3(0, 0, 0);

    [Tooltip("The rotation the door will rotate to when opened (relative to current rotation).")]
    public Vector3 openRotationOffset = new Vector3(0, 90, 0);

    [Tooltip("How long the door takes to open/close in seconds.")]
    public float animationDuration = 1.0f;

    [Tooltip("How close the player needs to be to interact with the door.")]
    public float interactionDistance = 3.0f;

    [Header("Audio")]
    public AudioClip doorOpenSound;
    public AudioClip doorCloseSound;

    private Vector3 closedPosition;
    private Vector3 targetOpenPosition;

    private Quaternion closedRotation;
    private Quaternion targetOpenRotation;

    private bool isOpen = false;
    private bool isAnimating = false;
    private AudioSource audioSource;
    private Transform playerTransform;

    void Start()
    {
        closedPosition = transform.position;
        targetOpenPosition = closedPosition + openPositionOffset;

        closedRotation = transform.rotation;
        targetOpenRotation = closedRotation * Quaternion.Euler(openRotationOffset);

        audioSource = GetComponent<AudioSource>();

        // Find the player by tag
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    void Update()
    {
        if (isAnimating || playerTransform == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= interactionDistance)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                StartCoroutine(ToggleDoor());
            }
        }
    }

    private IEnumerator ToggleDoor()
    {
        isAnimating = true;
        isOpen = !isOpen;

        Vector3 startPosition = transform.position;
        Vector3 endPosition = isOpen ? targetOpenPosition : closedPosition;

        Quaternion startRotation = transform.rotation;
        Quaternion endRotation = isOpen ? targetOpenRotation : closedRotation;

        // Play sound
        AudioClip soundToPlay = isOpen ? doorOpenSound : doorCloseSound;
        if (soundToPlay != null)
        {
            audioSource.PlayOneShot(soundToPlay);
        }

        // Determine how long we need to wait for the sound
        float soundDuration = soundToPlay != null ? soundToPlay.length : 0f;

        // Animate the door
        float elapsedTime = 0f;
        while (elapsedTime < animationDuration)
        {
            float t = elapsedTime / animationDuration;
            // animation ease-in and ease-out
            t = Mathf.SmoothStep(0, 1, t);

            transform.position = Vector3.Lerp(startPosition, endPosition, t);
            transform.rotation = Quaternion.Slerp(startRotation, endRotation, t);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = endPosition;
        transform.rotation = endRotation;

        // wait a bit
        float remainingSoundTime = Mathf.Max(0, soundDuration - animationDuration);
        yield return new WaitForSeconds(remainingSoundTime + 1.0f);

        isAnimating = false;
    }
}