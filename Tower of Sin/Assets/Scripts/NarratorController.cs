using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class NarratorController : MonoBehaviour
{
    public AudioClip ah_another_sinful_soul;
    public AudioClip cast_your_gaze;
    public AudioClip you_can_navigate;

    private AudioSource audioSource;
    private static NarratorController instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        int floorNumber = FloorTextController.floorNumber;

        if (scene.name == "Prison_Scene" && (floorNumber <= 1))
        {
            StopAllCoroutines();
            StartCoroutine(PlayIntroSequence());
        }
    }

    private IEnumerator PlayIntroSequence()
    {
        if (ah_another_sinful_soul == null) yield break;

        audioSource.PlayOneShot(ah_another_sinful_soul);
        yield return new WaitForSeconds(ah_another_sinful_soul.length);

        audioSource.PlayOneShot(cast_your_gaze);
        yield return new WaitForSeconds(cast_your_gaze.length);

        audioSource.PlayOneShot(you_can_navigate);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}