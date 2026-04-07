using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 200f;
    private float currentHealth;

    public TextMeshProUGUI healthUIText;
    public Image healthBarFill;
    public float drainSpeed = 5f;

    public Animator playerAnimator;
    public string deathTriggerName = "death";
    public float deathAnimationDuration = 2.0f;

    public CanvasGroup gameplayUICanvasGroup;
    public CanvasGroup deathScreenCanvasGroup;
    public float uiFadeDuration = 1.5f;

    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;

        if (healthBarFill != null)
            healthBarFill.fillAmount = 1f;

        if (deathScreenCanvasGroup != null)
        {
            deathScreenCanvasGroup.alpha = 0f;
            deathScreenCanvasGroup.interactable = false;
            deathScreenCanvasGroup.blocksRaycasts = false;
        }

        UpdateUI();
    }

    void Update()
    {
        if (healthBarFill != null)
        {
            float targetFill = currentHealth / maxHealth;
            healthBarFill.fillAmount = Mathf.Lerp(
                healthBarFill.fillAmount,
                targetFill,
                Time.deltaTime * drainSpeed
            );
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        //Debug.Log($"[PlayerHealth] HIT for {damage}. Current HP: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            UpdateUI();
            //Debug.Log("[PlayerHealth] PLAYER DIED");

            StartCoroutine(HandleDeathSequence());
            return;
        }

        UpdateUI();
    }

    private IEnumerator HandleDeathSequence()
    {
        isDead = true;

        // Reset floor and other run stats
        FloorTextController.floorNumber = 1;

        // death anim
        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger(deathTriggerName);
        }

        // wait for death animation to finish
        yield return new WaitForSeconds(deathAnimationDuration);

        // fade UIs
        yield return StartCoroutine(FadeBetweenUI(gameplayUICanvasGroup, deathScreenCanvasGroup, uiFadeDuration));

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private IEnumerator FadeBetweenUI(CanvasGroup fromUI, CanvasGroup toUI, float duration)
    {
        if (toUI != null)
        {
            toUI.alpha = 0f;
            toUI.gameObject.SetActive(true);
            toUI.interactable = false;
            toUI.blocksRaycasts = false;
        }

        float timer = 0f;

        float fromStartAlpha = fromUI != null ? fromUI.alpha : 0f;
        float toStartAlpha = toUI != null ? toUI.alpha : 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);

            if (fromUI != null)
                fromUI.alpha = Mathf.Lerp(fromStartAlpha, 0f, t);

            if (toUI != null)
                toUI.alpha = Mathf.Lerp(toStartAlpha, 1f, t);

            yield return null;
        }

        if (fromUI != null)
        {
            fromUI.alpha = 0f;
            fromUI.interactable = false;
            fromUI.blocksRaycasts = false;
        }

        if (toUI != null)
        {
            toUI.alpha = 1f;
            toUI.interactable = true;
            toUI.blocksRaycasts = true;
        }
    }

    public bool IsFullHealth()
    {
        return currentHealth >= maxHealth;
    }

    public void Heal(float heal)
    {
        if (isDead) return;

        float oldHealth = currentHealth;
        currentHealth += heal;

        if (currentHealth >= maxHealth)
        {
            currentHealth = maxHealth;
        }

        //Debug.Log($"[PlayerHealth] HEALED for {heal}. HP: {oldHealth} → {currentHealth}/{maxHealth}");
        UpdateUI();
    }

    void UpdateUI()
    {
        if (healthUIText != null)
        {
            healthUIText.text = (int)currentHealth + "/" + (int)maxHealth;
        }
    }
}