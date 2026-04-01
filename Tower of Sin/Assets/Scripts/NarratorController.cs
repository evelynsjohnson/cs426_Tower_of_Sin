using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

[RequireComponent(typeof(AudioSource))]
public class NarratorController : MonoBehaviour
{
    public AudioClip ah_another_sinful_soul;
    public AudioClip cast_your_gaze;
    public AudioClip you_can_navigate;

    public CanvasGroup subtitleCanvasGroup;
    public TextMeshProUGUI welcomeText;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI narratorText;

    [TextArea(3, 10)]
    public string introMonologue;

    private AudioSource audioSource;
    private static NarratorController instance;
    private bool isSequencePlaying = false;

    void Awake()
    {
        if (instance == null) { instance = this; DontDestroyOnLoad(gameObject); audioSource = GetComponent<AudioSource>(); }
        else { Destroy(gameObject); }
    }

    void Start() { SceneManager.sceneLoaded += OnSceneLoaded; CheckAndPlayIntro(SceneManager.GetActiveScene()); }

    void Update()
    {
        if (isSequencePlaying && Input.GetKeyDown(KeyCode.S))
        {
            SkipIntro();
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode) { CheckAndPlayIntro(scene); }

    private void CheckAndPlayIntro(Scene scene)
    {
        StopAllCoroutines();
        if (audioSource != null) audioSource.Stop();

        isSequencePlaying = false;

        if (subtitleCanvasGroup != null)
        {
            subtitleCanvasGroup.alpha = 0f;
            subtitleCanvasGroup.gameObject.SetActive(false);
        }

        Time.timeScale = 1f;

        if (scene.name == "Prison_Scene" && FloorTextController.floorNumber == 0)
        {
            StartCoroutine(PlayIntroSequence());
        }
    }

    private IEnumerator PlayIntroSequence()
    {
        isSequencePlaying = true;

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        subtitleCanvasGroup.alpha = 1f;
        subtitleCanvasGroup.gameObject.SetActive(true);
        welcomeText.alpha = 0;
        titleText.alpha = 0;
        narratorText.alpha = 0;
        narratorText.text = "";

        yield return new WaitForSecondsRealtime(1f);

        yield return StartCoroutine(FadeText(welcomeText, 1.5f, true));
        yield return new WaitForSecondsRealtime(1f);

        yield return StartCoroutine(FadeText(titleText, 1.5f, true));
        yield return new WaitForSecondsRealtime(2f);

        narratorText.alpha = 1;
        audioSource.PlayOneShot(ah_another_sinful_soul);

        float timePerChar = (ah_another_sinful_soul.length - 1f) / introMonologue.Length;
        for (int i = 0; i < introMonologue.Length; i++)
        {
            narratorText.text += introMonologue[i];
            yield return new WaitForSecondsRealtime(timePerChar);
        }

        yield return new WaitForSecondsRealtime(2f);

        float elapsedTime = 0f;
        while (elapsedTime < 2f)
        {
            elapsedTime += Time.unscaledDeltaTime;
            subtitleCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / 2f);
            yield return null;
        }

        EndIntro();
        StartCoroutine(PlayRemainingClips());
    }

    private void SkipIntro()
    {
        StopAllCoroutines();
        audioSource.Stop();

        subtitleCanvasGroup.alpha = 0f;
        subtitleCanvasGroup.gameObject.SetActive(false);

        EndIntro();

        StartCoroutine(PlayRemainingClips());
    }

    private IEnumerator PlayRemainingClips()
    {
        yield return new WaitForSeconds(1f);

        if (cast_your_gaze)
        {
            audioSource.PlayOneShot(cast_your_gaze);
            yield return new WaitForSeconds(cast_your_gaze.length + 1);
        }

        if (you_can_navigate)
        {
            audioSource.PlayOneShot(you_can_navigate);
        }
    }

    private void EndIntro()
    {
        isSequencePlaying = false;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        subtitleCanvasGroup.gameObject.SetActive(false);
    }

    private IEnumerator FadeText(TextMeshProUGUI text, float duration, bool fadeIn)
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            text.alpha = fadeIn ? (elapsedTime / duration) : (1f - (elapsedTime / duration));
            yield return null;
        }
        text.alpha = fadeIn ? 1f : 0f;
    }

    private void OnDestroy() { SceneManager.sceneLoaded -= OnSceneLoaded; }
}