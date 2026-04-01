    using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class TargetDummy : MonoBehaviour
{
    public float playerMaxHP = 200f;  
    private float maxHP;
    private float currentHP;

    public Image healthBarFill;
    public TMP_Text healthText;
    public RectTransform textSpawnPoint;
    public GameObject textPrefab;

    private float lastHitTime;
    private Vector3 originalPos;
    private Coroutine jiggleRoutine;

    void Start()
    {
        maxHP = playerMaxHP * 2;
        currentHP = maxHP;
        originalPos = transform.localPosition;
        UpdateUI();
    }

    void Update()
    {
        if (Time.time - lastHitTime > 5f && currentHP < maxHP)
        {
            float regenPerSecond = maxHP / 10f;

            currentHP += regenPerSecond * Time.deltaTime;

            if (currentHP > maxHP) currentHP = maxHP;

            UpdateUI();
        }
    }

    public void TakeDamage(float amount)
    {
        lastHitTime = Time.time;
        currentHP = Mathf.Max(currentHP - amount, 0);

        UpdateUI();
        SpawnDamageText(amount, Color.red);

        if (jiggleRoutine != null) StopCoroutine(jiggleRoutine);
        jiggleRoutine = StartCoroutine(HitBackEffect());
    }

    void UpdateUI()
    {
        if (healthBarFill) healthBarFill.fillAmount = currentHP / maxHP;
        if (healthText) healthText.text = $"{currentHP:F1} / {maxHP:F1}";
    }

    void SpawnDamageText(float amount, Color color)
    {
        GameObject textObj = Instantiate(textPrefab, textSpawnPoint);

        textObj.transform.localPosition = Vector3.zero;
        textObj.transform.localRotation = Quaternion.identity;
        textObj.transform.localScale = Vector3.one;

        var tmp = textObj.GetComponent<TMP_Text>();
        tmp.text = amount.ToString("F1");
        tmp.color = color;

        StartCoroutine(AnimateText(textObj, tmp));
    }

    IEnumerator AnimateText(GameObject obj, TMP_Text txt)
    {
        RectTransform rt = obj.GetComponent<RectTransform>();
        float duration = 1.2f;
        float elapsed = 0;

        Vector2 direction = new Vector2(Random.Range(-50f, 50f), 100f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / duration;

            rt.anchoredPosition += direction * Time.deltaTime;

            // Fade out
            txt.alpha = Mathf.Lerp(1, 0, percent);

            yield return null;
        }
        Destroy(obj);
    }

    IEnumerator HitBackEffect()
    {
        Vector3 hitDir = -transform.forward * 0.15f;

        transform.localPosition = originalPos + hitDir;
        yield return new WaitForSeconds(0.05f);

        transform.localPosition = originalPos;
    }
}