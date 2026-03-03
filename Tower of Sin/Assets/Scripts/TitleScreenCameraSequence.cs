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

    private int currentShotIndex = 0;

    void Start()
    {
        if (shots.Count == 0)
        {
            Debug.LogWarning("No camera shots assigned!");
            return;
        }

        // Start screen fully black
        Color c = fadeImage.color;
        c.a = 1f;
        fadeImage.color = c;

        StartCoroutine(PlayCameraSequence());
    }

    IEnumerator PlayCameraSequence()
    {
        while (true)
        {
            CameraShot shot = shots[currentShotIndex];

            // Snap to start position
            mainCamera.transform.position = shot.startPoint.position;
            mainCamera.transform.rotation = shot.startPoint.rotation;

            // Calculate how long this shot will take based on distance
            float distance = Vector3.Distance(shot.startPoint.position, shot.endPoint.position);
            float animDuration = shot.moveSpeed > 0 ? distance / shot.moveSpeed : 3f;

            // SAFETY: Force the animation to be at least 2.5 seconds long. 
            // We need 1s to fade clear, 1s to fade black, and at least 0.5s of middle time!
            animDuration = Mathf.Max(animDuration, 2.5f);

            float elapsed = 0f;

            // ONE single loop to perfectly control movement and fading together
            while (elapsed < animDuration)
            {
                elapsed += Time.deltaTime;

                // 1. Move Camera Smoothly
                float t = elapsed / animDuration;
                float smoothT = t * t * (3f - 2f * t); // Smoothstep easing

                mainCamera.transform.position = Vector3.Lerp(shot.startPoint.position, shot.endPoint.position, smoothT);
                mainCamera.transform.rotation = Quaternion.Slerp(shot.startPoint.rotation, shot.endPoint.rotation, smoothT);

                // 2. Control the Fade perfectly based on exact time
                Color color = fadeImage.color;

                if (elapsed <= 1f)
                {
                    // The First 1 Second: Fade from Black (1) to Clear (0)
                    color.a = Mathf.Lerp(1f, 0f, elapsed / 1f);
                }
                else if (elapsed >= animDuration - 1f)
                {
                    // The Last 1 Second: Fade from Clear (0) to Black (1)
                    float fadeOutElapsed = elapsed - (animDuration - 1f);
                    color.a = Mathf.Lerp(0f, 1f, fadeOutElapsed / 1f);
                }
                else
                {
                    // In between the first and last second: Stay completely clear
                    color.a = 0f;
                }

                fadeImage.color = color;

                yield return null; // Wait for next frame
            }

            // 3. The shot is fully over. Force perfectly Black just in case.
            Color finalColor = fadeImage.color;
            finalColor.a = 1f;
            fadeImage.color = finalColor;

            // 4. Hold the screen totally black for 1 second before snapping to the next camera
            yield return new WaitForSeconds(1f);

            // Go to the next shot in the list
            currentShotIndex = (currentShotIndex + 1) % shots.Count;
        }
    }
}