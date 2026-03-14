using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TitleScreenCameraSequence : MonoBehaviour
{
    [Header("Core References")]
    public Camera mainCamera;
    public Image fadeImage;

    [Header("Shot Parents")]
    public Transform firstShotParent;
    public Transform randomShotsParent;

    [Header("Movement")]
    public float moveDuration = 5f; // ALWAYS 5 seconds

    [Header("Fade Settings")]
    public float fadeDuration = 1f;
    public float blackHoldDuration = 0.5f;

    private struct CameraShot
    {
        public Transform start;
        public Transform end;

        public CameraShot(Transform s, Transform e)
        {
            start = s;
            end = e;
        }
    }

    private CameraShot firstShot;
    private bool hasFirstShot = false;

    private List<CameraShot> allShots = new List<CameraShot>();
    private Queue<CameraShot> shuffleQueue = new Queue<CameraShot>();

    void Start()
    {
        if (mainCamera == null || fadeImage == null)
        {
            Debug.LogWarning("Missing camera or fade image.");
            return;
        }

        CollectShots();

        SetFade(1f);
        StartCoroutine(PlaySequence());
    }

    void CollectShots()
    {
        allShots.Clear();
        hasFirstShot = false;

        // FIRST SHOT
        if (firstShotParent != null)
        {
            Transform start = firstShotParent.Find("Start");
            Transform end = firstShotParent.Find("End");

            if (start != null && end != null)
            {
                firstShot = new CameraShot(start, end);
                hasFirstShot = true;
            }
            else
            {
                Debug.LogWarning("FirstShot missing Start or End.");
            }
        }

        // RANDOM SHOTS
        if (randomShotsParent != null)
        {
            foreach (Transform shot in randomShotsParent)
            {
                Transform start = shot.Find("Start");
                Transform end = shot.Find("End");

                if (start != null && end != null)
                    allShots.Add(new CameraShot(start, end));
            }
        }
    }

    IEnumerator PlaySequence()
    {
        if (!hasFirstShot && allShots.Count == 0)
            yield break;

        // ----- PLAY FIRST SHOT ONCE -----
        if (hasFirstShot)
        {
            yield return StartCoroutine(PlayShot(firstShot));
            allShots.Add(firstShot); // Add it into rotation AFTER playing
        }

        // ----- LOOP FOREVER -----
        while (true)
        {
            if (shuffleQueue.Count == 0)
                RefillShuffleQueue();

            CameraShot nextShot = shuffleQueue.Dequeue();
            yield return StartCoroutine(PlayShot(nextShot));
        }
    }

    void RefillShuffleQueue()
    {
        List<CameraShot> temp = new List<CameraShot>(allShots);

        // Fisher-Yates shuffle
        for (int i = 0; i < temp.Count; i++)
        {
            int randIndex = Random.Range(i, temp.Count);
            CameraShot t = temp[i];
            temp[i] = temp[randIndex];
            temp[randIndex] = t;
        }

        shuffleQueue = new Queue<CameraShot>(temp);
    }

    IEnumerator PlayShot(CameraShot shot)
    {
        yield return StartCoroutine(Fade(1f));

        mainCamera.transform.position = shot.start.position;
        mainCamera.transform.rotation = shot.start.rotation;

        yield return new WaitForSeconds(blackHoldDuration);

        yield return StartCoroutine(Fade(0f));

        yield return StartCoroutine(MoveCamera(shot));
    }

    IEnumerator MoveCamera(CameraShot shot)
    {
        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveDuration;
            float smoothT = t * t * (3f - 2f * t); // smoothstep

            mainCamera.transform.position =
                Vector3.Lerp(shot.start.position,
                             shot.end.position,
                             smoothT);

            mainCamera.transform.rotation =
                Quaternion.Slerp(shot.start.rotation,
                                 shot.end.rotation,
                                 smoothT);

            yield return null;
        }

        // Snap exact end
        mainCamera.transform.position = shot.end.position;
        mainCamera.transform.rotation = shot.end.rotation;
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