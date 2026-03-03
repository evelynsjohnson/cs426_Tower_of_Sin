using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct CameraShot
{
    public Transform startPoint;
    public Transform endPoint;
    [Tooltip("How fast the camera moves in units per second.")]
    public float moveSpeed;
}

public class TitleScreenCameraSequence : MonoBehaviour
{
    [Header("Core References")]
    public Camera mainCamera;
    public Image fadeImage;

    [Header("Sequence Settings")]
    public List<CameraShot> shots = new List<CameraShot>();

    [Header("Fade Settings")]
    public float fadeDuration = 1f;
    public float blackHoldDuration = 0.5f;

    private int currentShotIndex = 0;

    void Start()
    {
        if (shots.Count == 0)
        {
            Debug.LogWarning("No camera shots assigned!");
            return;
        }

        // Start fully black
        SetFade(1f);
        StartCoroutine(PlaySequence());
    }

    IEnumerator PlaySequence()
    {
        while (true)
        {
            CameraShot shot = shots[currentShotIndex];

            // Fade OUT to black before switching
            yield return StartCoroutine(Fade(1f));

            // Snap to start position while black
            mainCamera.transform.position = shot.startPoint.position;
            mainCamera.transform.rotation = shot.startPoint.rotation;

            yield return new WaitForSeconds(blackHoldDuration);

            // Fade IN from black
            yield return StartCoroutine(Fade(0f));

            // Move camera
            yield return StartCoroutine(MoveCamera(shot));

            currentShotIndex = (currentShotIndex + 1) % shots.Count;
        }
    }

    IEnumerator MoveCamera(CameraShot shot)
    {
        float distance = Vector3.Distance(
            shot.startPoint.position,
            shot.endPoint.position);

        float duration = shot.moveSpeed > 0
            ? distance / shot.moveSpeed
            : 3f;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Smoothstep easing
            float smoothT = t * t * (3f - 2f * t);

            mainCamera.transform.position =
                Vector3.Lerp(shot.startPoint.position,
                             shot.endPoint.position,
                             smoothT);

            mainCamera.transform.rotation =
                Quaternion.Slerp(shot.startPoint.rotation,
                                 shot.endPoint.rotation,
                                 smoothT);

            yield return null;
        }
    }

    IEnumerator Fade(float targetAlpha)
    {
        float startAlpha = fadeImage.color.a;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;

            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            SetFade(newAlpha);

            yield return null;
        }

        SetFade(targetAlpha);
    }

    void SetFade(float alpha)
    {
        Color c = fadeImage.color;
        c.a = alpha;
        fadeImage.color = c;
    }
}