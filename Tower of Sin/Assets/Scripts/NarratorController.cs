using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

[RequireComponent(typeof(AudioSource))]
public class NarratorController : MonoBehaviour
{
    [Header("Audio Clips")]
    public AudioClip ah_another_sinful_soul;
    public AudioClip cast_your_gaze;
    public AudioClip you_can_navigate;

    [Header("UI Elements")]
    public CanvasGroup subtitleCanvasGroup;
    public TextMeshProUGUI welcomeText;  // "Welcome to the"
    public TextMeshProUGUI titleText;    // "Tower of Sin"
    public TextMeshProUGUI narratorText; // The typewriter text

    [TextArea(3, 10)]
    public string introMonologue;

    private AudioSource audioSource;
    private static NarratorController instance;

    void Awake()
    {
        if (instance == null) { instance = this; DontDestroyOnLoad(gameObject); audioSource = GetComponent<AudioSource>(); }
        else { Destroy(gameObject); }
    }

    void Start() { SceneManager.sceneLoaded += OnSceneLoaded; CheckAndPlayIntro(SceneManager.GetActiveScene()); }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode) { CheckAndPlayIntro(scene); }

    private void CheckAndPlayIntro(Scene scene)
    {
        if (scene.name == "Prison_Scene" && FloorTextController.floorNumber == 0)
        {
            StopAllCoroutines();
            StartCoroutine(PlayIntroSequence());
        }
    }

    private IEnumerator PlayIntroSequence()
    {
        subtitleCanvasGroup.alpha = 1f;
        welcomeText.alpha = 0;
        titleText.alpha = 0;
        narratorText.alpha = 0;
        narratorText.text = "";

        yield return new WaitForSeconds(1f);

        yield return StartCoroutine(FadeText(welcomeText, 1.5f, true));
        yield return new WaitForSeconds(1f);

        yield return StartCoroutine(FadeText(titleText, 1.5f, true));
        yield return new WaitForSeconds(2f);

        narratorText.alpha = 1;
        audioSource.PlayOneShot(ah_another_sinful_soul);


        float timePerChar = (ah_another_sinful_soul.length - 5f) / introMonologue.Length;
        for (int i = 0; i < introMonologue.Length; i++)
        {
            narratorText.text += introMonologue[i];
            yield return new WaitForSeconds(timePerChar);
        }

        yield return new WaitForSeconds(2f);

        float elapsedTime = 0f;
        while (elapsedTime < 2f)
        {
            elapsedTime += Time.deltaTime;
            subtitleCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / 2f);
            yield return null;
        }

        if (cast_your_gaze) { audioSource.PlayOneShot(cast_your_gaze); yield return new WaitForSeconds(cast_your_gaze.length + 1); }
        if (you_can_navigate) { audioSource.PlayOneShot(you_can_navigate); }
    }

    private IEnumerator FadeText(TextMeshProUGUI text, float duration, bool fadeIn)
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            text.alpha = fadeIn ? (elapsedTime / duration) : (1f - (elapsedTime / duration));
            yield return null;
        }
        text.alpha = fadeIn ? 1f : 0f;
    }

    private void OnDestroy() { SceneManager.sceneLoaded -= OnSceneLoaded; }
}