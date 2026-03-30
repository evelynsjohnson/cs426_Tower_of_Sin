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

            audioSource = GetComponent<AudioSource>();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        CheckAndPlayIntro(SceneManager.GetActiveScene());
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CheckAndPlayIntro(scene);
    }
    private void CheckAndPlayIntro(Scene scene)
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
        if (ah_another_sinful_soul == null)
        {
            yield break;
        }

        audioSource.PlayOneShot(ah_another_sinful_soul);
        yield return new WaitForSeconds(ah_another_sinful_soul.length + 3);

        if (cast_your_gaze != null)
        {
            audioSource.PlayOneShot(cast_your_gaze);
            yield return new WaitForSeconds(cast_your_gaze.length + 3);
        }

        if (you_can_navigate != null)
        {
            audioSource.PlayOneShot(you_can_navigate);
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}