using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class TitleScreenButtons : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    public enum ButtonType
    {
        Begin,
        Options,
        Credits
    }

    public ButtonType buttonType;

    public RawImage targetImage;
    public Texture normalTexture;
    public Texture hoverTexture;
    public GameObject optionsCanvas;
    public GameObject creditsCanvas;
    public GameObject mainCanvas;

    [Header("Cinematic Intro Settings (Begin Button)")]
    public Transform mainCamera;
    public Transform cameraTargetLocation;
    public Transform objectToMoveUp;
    public Vector3 upwardMovementOffset = new Vector3(0, 10f, 0);

    public float movementDuration = 3f;
    [Range(0f, 1f)]
    public float fadeStartFraction = 0.8f;

    public float uiFadeDuration = 1.0f;

    public float gateAudioDelay = 0.5f;

    public Image blackScreenOverlay;
    public AudioSource introAudio;
    public AudioSource gateAudio;

    public AudioSource bgMusic;

    public GameObject[] uiElementsToHide;

    private bool isTransitioning = false;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isTransitioning) return;

        if (targetImage != null && hoverTexture != null)
            targetImage.texture = hoverTexture;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isTransitioning) return;

        if (targetImage != null && normalTexture != null)
            targetImage.texture = normalTexture;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isTransitioning) return;

        switch (buttonType)
        {
            case ButtonType.Begin:
                targetImage.texture = normalTexture;
                StartCoroutine(PlayIntroSequence());
                break;

            case ButtonType.Options:
                if (optionsCanvas != null)
                {
                    optionsCanvas.SetActive(true);
                    mainCanvas.SetActive(false);
                    targetImage.texture = normalTexture;
                }
                break;

            case ButtonType.Credits:
                if (creditsCanvas != null)
                {
                    creditsCanvas.SetActive(true);
                    mainCanvas.SetActive(false);
                    targetImage.texture = normalTexture;
                }
                break;
        }
    }

    private IEnumerator PlayIntroSequence()
    {
        isTransitioning = true;

        if (introAudio != null) introAudio.Play();

        // starting positions
        Vector3 startCamPos = mainCamera.position;
        Quaternion startCamRot = mainCamera.rotation;

        Vector3 startObjPos = objectToMoveUp.position;
        Vector3 targetObjPos = startObjPos + upwardMovementOffset;

        // black screen overlay
        if (blackScreenOverlay != null)
        {
            Color c = blackScreenOverlay.color;
            c.a = 0f;
            blackScreenOverlay.color = c;
            blackScreenOverlay.gameObject.SetActive(true);
        }

        // UI elements and BG music for fading
        List<Graphic> graphicsToFade = new List<Graphic>();
        List<float> startAlphas = new List<float>();

        if (uiElementsToHide != null)
        {
            foreach (GameObject uiElement in uiElementsToHide)
            {
                if (uiElement != null)
                {
                    Graphic[] graphics = uiElement.GetComponentsInChildren<Graphic>();
                    foreach (Graphic g in graphics)
                    {
                        graphicsToFade.Add(g);
                        startAlphas.Add(g.color.a);
                    }
                }
            }
        }

        float startBgVolume = (bgMusic != null) ? bgMusic.volume : 0f;
        float elapsedTime = 0f;
        bool hasGateAudioPlayed = false;

        // animate camera, object, UI fade, and music
        while (elapsedTime < movementDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / movementDuration; // Timer for camera/object (0.0 to 1.0)

            float uiFadeProgress = uiFadeDuration > 0f ? Mathf.Clamp01(elapsedTime / uiFadeDuration) : 1f;

            // gate audio
            if (!hasGateAudioPlayed && elapsedTime >= gateAudioDelay)
            {
                if (gateAudio != null) gateAudio.Play();
                hasGateAudioPlayed = true;
            }

            // Move Camera
            if (mainCamera != null && cameraTargetLocation != null)
            {
                mainCamera.position = Vector3.Lerp(startCamPos, cameraTargetLocation.position, t);
                mainCamera.rotation = Quaternion.Lerp(startCamRot, cameraTargetLocation.rotation, t);
            }

            // Move Object
            if (objectToMoveUp != null)
            {
                objectToMoveUp.position = Vector3.Lerp(startObjPos, targetObjPos, t);
            }

            // Fade out UI using its own timer
            for (int i = 0; i < graphicsToFade.Count; i++)
            {
                if (graphicsToFade[i] != null)
                {
                    Color c = graphicsToFade[i].color;
                    c.a = Mathf.Lerp(startAlphas[i], 0f, uiFadeProgress);
                    graphicsToFade[i].color = c;
                }
            }

            // Fade out BG Music 
            if (bgMusic != null)
            {
                bgMusic.volume = Mathf.Lerp(startBgVolume, 0f, t);
            }

            // Increase black screen opacity at the end
            if (t >= fadeStartFraction && blackScreenOverlay != null)
            {
                float fadeProgress = (t - fadeStartFraction) / (1f - fadeStartFraction);
                Color c = blackScreenOverlay.color;
                c.a = Mathf.Lerp(0f, 1f, fadeProgress);
                blackScreenOverlay.color = c;
            }

            yield return null;
        }

        if (blackScreenOverlay != null)
        {
            Color finalColor = blackScreenOverlay.color;
            finalColor.a = 1f;
            blackScreenOverlay.color = finalColor;
        }

        if (bgMusic != null) bgMusic.volume = 0f;

        foreach (Graphic g in graphicsToFade)
        {
            if (g != null) g.enabled = false;
        }

        // wait for the intro audio to finish
        if (introAudio != null)
        {
            yield return new WaitWhile(() => introAudio.isPlaying);
        }

        // Switch scenes
        SceneManager.LoadScene("Prison_Scene");
    }
}