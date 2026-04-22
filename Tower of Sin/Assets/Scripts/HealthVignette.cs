using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HealthVignette : MonoBehaviour
{
    public RawImage vignetteImage;
    public PlayerHealth playerHealth;

    public float maxAlpha = 0.6f;
    public float minAlpha = 0f;

    public GameObject bloodSplatterImage;
    public float splatterFadeSpeed = .8f;

    private RawImage[] bloodSplatterRawImages;
    private Coroutine splatterRoutine;

    void Start()
    {
        if (playerHealth != null)
            playerHealth.OnDamageTaken += TriggerBlood;

        if (bloodSplatterImage != null)
            bloodSplatterRawImages = bloodSplatterImage.GetComponentsInChildren<RawImage>(true);
    }

    void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.OnDamageTaken -= TriggerBlood;
    }

    void Update()
    {
        if (playerHealth == null || vignetteImage == null) return;

        float healthPercent = playerHealth.CurrentHealth / playerHealth.MaxHealth;

        float alpha = 0f;

        if (healthPercent <= 0.75f)
        {
            float t = Mathf.InverseLerp(0.5f, 0f, healthPercent);
            alpha = Mathf.Lerp(minAlpha, maxAlpha, t);
        }

        Color c = vignetteImage.color;
        c.a = alpha;
        vignetteImage.color = c;
    }

    IEnumerator FlashBlood()
    {
        if (bloodSplatterRawImages == null || bloodSplatterRawImages.Length == 0)
            yield break;

        // Start fully visible
        SetBloodAlpha(1f);

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * splatterFadeSpeed;
            SetBloodAlpha(Mathf.Lerp(1f, 0f, t));
            yield return null;
        }

        SetBloodAlpha(0f);
    }

    void SetBloodAlpha(float alpha)
    {
        foreach (RawImage img in bloodSplatterRawImages)
        {
            if (img == null) continue;

            Color c = img.color;
            c.a = alpha;
            img.color = c;
        }
    }

    void TriggerBlood()
    {
        if (bloodSplatterImage == null) return;

        if (splatterRoutine != null)
            StopCoroutine(splatterRoutine);

        splatterRoutine = StartCoroutine(FlashBlood());
    }
}