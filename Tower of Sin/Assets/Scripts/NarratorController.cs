using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class NarratorController : MonoBehaviour
{
    [System.Serializable]
    public class SentenceAudio
    {
        [TextArea(2, 5)]
        public string sentence;
        public AudioClip clip;
    }

    [Header("Scene Trigger")]
    public string targetSceneName = "Prison_Scene";
    public bool playOnlyOncePerSession = true;

    [Header("Audio Sources")]
    public AudioSource voiceSource;
    public AudioSource bgSource;

    [Header("Audio Clips")]
    public AudioClip introBg;
    public AudioClip logoSound;
    public List<SentenceAudio> sentenceSequence = new List<SentenceAudio>();

    public CanvasGroup introCanvasGroup;
    public TextMeshProUGUI txt1;
    public TextMeshProUGUI txt2;
    public TextMeshProUGUI txt3;
    public RawImage logoImage;
    public TextMeshProUGUI narratorText;
    public TextMeshProUGUI skipText;

    public float initialBlackDelay = 1f;
    public float fadeDuration = .5f;
    public float betweenTitles = 0.25f;
    public float gapBetweenSentences = 0.05f;

    [Range(0f, 1f)] public float logoSoundVolume = 0.35f;
    public float logoFadeDelayAfterSoundStarts = 0f;
    public float logoHold = 1f;

    [Range(0f, 1f)] public float introBgVolume = 1f;

    public float spacePause = 0.06f;
    public float wordSpeedMultiplier = 1.25f;
    public bool pauseGameplayDuringCutscene = true;

    private bool isPlaying;
    private bool skipRequested;
    private static bool hasPlayed;

    private void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        HideEverything();
        Check(SceneManager.GetActiveScene());
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        if (isPlaying && Input.GetKeyDown(KeyCode.S))
            Skip();
    }

    private void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        Check(s);
    }

    private void Check(Scene scene)
    {
        StopAllCoroutines();

        isPlaying = false;
        skipRequested = false;
        HideEverything();

        if (voiceSource) voiceSource.Stop();
        if (bgSource) bgSource.Stop();

        bool shouldPlay =
            scene.name == targetSceneName &&
            FloorTextController.floorNumber == 0 &&
            (!playOnlyOncePerSession || !hasPlayed);

        if (shouldPlay)
            StartCoroutine(Sequence());
    }

    private IEnumerator Sequence()
    {
        isPlaying = true;
        hasPlayed = true;

        if (pauseGameplayDuringCutscene)
            Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (introCanvasGroup != null)
        {
            introCanvasGroup.gameObject.SetActive(true);
            introCanvasGroup.alpha = 1f;
        }

        ResetVisuals();

        if (skipRequested) { End(); yield break; }

        if (bgSource != null && introBg != null)
        {
            bgSource.clip = introBg;
            bgSource.loop = false;
            bgSource.volume = introBgVolume;
            bgSource.Play();
        }

        yield return Wait(betweenTitles);
        if (skipRequested) { End(); yield break; }

        yield return Title(txt1);
        if (skipRequested) { End(); yield break; }

        yield return Wait(betweenTitles);
        if (skipRequested) { End(); yield break; }

        yield return Title(txt2);
        if (skipRequested) { End(); yield break; }

        yield return Wait(betweenTitles);
        if (skipRequested) { End(); yield break; }

        yield return Title(txt3);
        if (skipRequested) { End(); yield break; }

        if (bgSource != null && bgSource.isPlaying)
        {
            while (bgSource.isPlaying)
            {
                if (skipRequested) { End(); yield break; }
                yield return null;
            }
        }

        PlayVoice(logoSound, logoSoundVolume);
        if (skipRequested) { End(); yield break; }

        yield return FadeLogo(0f, 3f, fadeDuration + 2f);
        if (skipRequested) { End(); yield break; }

        if (voiceSource != null)
        {
            while (voiceSource.isPlaying)
            {
                if (skipRequested) { End(); yield break; }
                yield return null;
            }
        }

        yield return FadeLogo(1f, 0f, fadeDuration - .5f);
        if (skipRequested) { End(); yield break; }

        if (narratorText != null)
        {
            narratorText.alpha = 1f;
            narratorText.text = "";
        }

        foreach (var s in sentenceSequence)
        {
            if (s == null || s.clip == null || string.IsNullOrWhiteSpace(s.sentence))
                continue;

            yield return TypeAppend(s.sentence, s.clip);

            if (skipRequested)
            {
                End();
                yield break;
            }

            yield return Wait(gapBetweenSentences);

            if (skipRequested)
            {
                End();
                yield break;
            }
        }

        yield return FadeCanvas(introCanvasGroup, 1f, 0f, fadeDuration);
        End();
    }

    private IEnumerator Title(TextMeshProUGUI t)
    {
        if (t == null)
            yield break;

        yield return FadeTMP(t, 0f, 1f, fadeDuration);

        float hold = Mathf.Clamp(t.text.Length / 18f, 1.5f, 5f);

        if (t == txt2)
            hold = Mathf.Max(0f, hold - 1f);

        yield return Wait(hold);

        yield return FadeTMP(t, 1f, 0f, fadeDuration);
    }

    private IEnumerator TypeAppend(string sentence, AudioClip clip)
    {
        if (narratorText == null || voiceSource == null || clip == null)
            yield break;

        if (!string.IsNullOrEmpty(narratorText.text))
            narratorText.text += " ";

        int nonWhitespaceCount = CountChars(sentence);
        float baseDelay = clip.length / Mathf.Max(1, nonWhitespaceCount);

        float letterDelay = baseDelay / Mathf.Max(1f, wordSpeedMultiplier);

        PlayVoice(clip, 1f);

        for (int i = 0; i < sentence.Length; i++)
        {
            if (skipRequested)
                yield break;

            char ch = sentence[i];
            narratorText.text += ch;

            if (char.IsWhiteSpace(ch))
                yield return Wait(spacePause);
            else
                yield return Wait(letterDelay);
        }

        while (voiceSource.isPlaying)
        {
            if (skipRequested)
                yield break;

            yield return null;
        }
    }

    private void PlayVoice(AudioClip clip, float volume = 1f)
    {
        if (voiceSource == null || clip == null)
            return;

        voiceSource.clip = clip;
        voiceSource.loop = false;
        voiceSource.volume = volume;
        voiceSource.Play();
    }

    private IEnumerator FadeLogo(float from, float to, float duration)
    {
        if (logoImage == null)
            yield break;

        float t = 0f;
        Color c = logoImage.color;
        c.a = from;
        logoImage.color = c;

        while (t < duration)
        {
            if (skipRequested)
                yield break;

            t += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(from, to, t / duration);
            logoImage.color = c;
            yield return null;
        }

        c.a = to;
        logoImage.color = c;
    }

    private IEnumerator FadeTMP(TextMeshProUGUI t, float a, float b, float duration)
    {
        if (t == null)
            yield break;

        float time = 0f;
        t.alpha = a;

        while (time < duration)
        {
            if (skipRequested)
                yield break;

            time += Time.unscaledDeltaTime;
            t.alpha = Mathf.Lerp(a, b, time / duration);
            yield return null;
        }

        t.alpha = b;
    }

    private IEnumerator FadeCanvas(CanvasGroup c, float a, float b, float duration)
    {
        if (c == null)
            yield break;

        float time = 0f;
        c.alpha = a;

        while (time < duration)
        {
            if (skipRequested)
                yield break;

            time += Time.unscaledDeltaTime;
            c.alpha = Mathf.Lerp(a, b, time / duration);
            yield return null;
        }

        c.alpha = b;
    }

    private IEnumerator Wait(float t)
    {
        float e = 0f;
        while (e < t)
        {
            if (skipRequested)
                yield break;

            e += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private int CountChars(string s)
    {
        int c = 0;
        foreach (char ch in s)
            if (!char.IsWhiteSpace(ch)) c++;
        return c;
    }

    private void Skip()
    {
        skipRequested = true;
        StopAllCoroutines();

        if (voiceSource) voiceSource.Stop();
        if (bgSource) bgSource.Stop();

        End();
    }

    private void End()
    {
        isPlaying = false;
        skipRequested = false;

        if (pauseGameplayDuringCutscene)
            Time.timeScale = 1f;

        HideEverything();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void ResetVisuals()
    {
        if (txt1 != null) txt1.alpha = 0f;
        if (txt2 != null) txt2.alpha = 0f;
        if (txt3 != null) txt3.alpha = 0f;

        if (logoImage != null)
        {
            Color c = logoImage.color;
            c.a = 0f;
            logoImage.color = c;
        }

        if (narratorText != null)
        {
            narratorText.alpha = 0f;
            narratorText.text = "";
        }

        if (skipText != null)
            skipText.alpha = 1f;
    }

    private void HideEverything()
    {
        if (introCanvasGroup != null)
        {
            introCanvasGroup.alpha = 0f;
            introCanvasGroup.gameObject.SetActive(false);
        }
    }
}