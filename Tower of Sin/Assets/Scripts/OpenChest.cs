using System.Collections;


using UnityEngine;


using System.Collections.Generic;


[RequireComponent(typeof(AudioSource))]


public class OpenChest : MonoBehaviour
{
    [Tooltip("The position the chest will move to when opened (relative to current position).")]
    public UnityEngine.Vector3 openPositionOffset = new Vector3(0, 0, 0);

    [Tooltip("The rotation the chest will rotate to when opened (relative to current rotation).")]
    public UnityEngine.Vector3 openRotationOffset = new Vector3(0, 90, 0);

    [Tooltip("How long the xhest takes to open/close in seconds.")]
    public float animationDuration = 1.0f;


    [Tooltip("How close the player needs to be to interact with the chest.")]
    public float interactionDistance = 3.0f;

    [Header("Audio")]
    public AudioClip chestOpenSound;
    public AudioClip chestCloseSound;

    public UIManager playerUI;

    private UnityEngine.Vector3 closedPosition;
    private UnityEngine.Vector3 targetOpenPosition;

    private UnityEngine.Quaternion closedRotation;
    private UnityEngine.Quaternion targetOpenRotation;

    private bool isOpen = false;


    private bool isAnimating = false;

    private AudioSource audioSource;
    private Transform playerTransform;
    private InvManager inventory;
    private bool spawnedItem = false;
    private int itmeToSpawn;

    private int maxIndex;
    public List<ItemData> lootItems = new List<ItemData>();

    public GameObject itemPrefab;

    public GameObject spawnPoint;


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
        itmeToSpawn = Random.Range(0, lootItems.Count);
    }



    void Update()
    {
        if (isAnimating || playerTransform == null) return;

        float distanceToPlayer = UnityEngine.Vector3.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= interactionDistance)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                StartCoroutine(ToggleChest());
            }
        }
    }



    private IEnumerator ToggleChest()


    {
        isAnimating = true;

        isOpen = !isOpen;


        UnityEngine.Vector3 startPosition = transform.position;


        UnityEngine.Vector3 endPosition = isOpen ? targetOpenPosition : closedPosition;


        Quaternion startRotation = transform.rotation;


        Quaternion endRotation = isOpen ? targetOpenRotation : closedRotation;

        // Play sound
        AudioClip soundToPlay = isOpen ? chestOpenSound : chestCloseSound;


        if (soundToPlay != null)
        {
            audioSource.PlayOneShot(soundToPlay);
        }

        // Determine how long we need to wait for the sound
        float soundDuration = soundToPlay != null ? soundToPlay.length : 0f;

        // Animate the chest
        float elapsedTime = 0f;


        while (elapsedTime < animationDuration)
        {
            float t = elapsedTime / animationDuration;

            // animation ease-in and ease-out
            t = Mathf.SmoothStep(0, 1, t);

            transform.position = UnityEngine.Vector3.Lerp(startPosition, endPosition, t);


            transform.rotation = Quaternion.Slerp(startRotation, endRotation, t);

            elapsedTime += Time.deltaTime;

            yield return null;
        }

        if (!spawnedItem)
        {
            spawnedItem = true;

            ItemData item = lootItems[itmeToSpawn];

            GameObject droppedItem = Instantiate(itemPrefab, spawnPoint.transform.position, Quaternion.identity);
            SpawnedItem itemScript = droppedItem.GetComponent<SpawnedItem>();
            itemScript.SetItem(item);
        }

        transform.position = endPosition;

        transform.rotation = endRotation;

        // wait a bit
        float remainingSoundTime = Mathf.Max(0, soundDuration - animationDuration);

        yield return new WaitForSeconds(remainingSoundTime + 1.0f);

        isAnimating = false;
    }
}