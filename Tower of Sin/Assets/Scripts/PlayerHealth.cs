using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    public float baseHealth = 200f;

    public float maxHealth;
    private float currentHealth;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    public TextMeshProUGUI baseHealthText;
    public TextMeshProUGUI healthBonusText;
    public TextMeshProUGUI finalHealthText;

    public TextMeshProUGUI healthUIText;
    public Image healthBarFill;
    public float drainSpeed = 5f;

    public Animator playerAnimator;
    public string deathTriggerName = "death";
    public float deathAnimationDuration = 2.0f;

    public CanvasGroup gameplayUICanvasGroup;
    public CanvasGroup deathScreenCanvasGroup;
    public float uiFadeDuration = 1.5f;
    public TextMeshProUGUI deathStatsText;

    public string prisonSceneName = "Prison_Scene";
    private int lastAutoHealFloor = -1;
    private string previousSceneName = "";

    private bool isDead = false;
    private bool deathRoutineRunning = false;

    private float defense = 0;
    public TextMeshProUGUI finalDefenseText;

    private FirstPersonMovement movement;
    private Rigidbody rb;

    public System.Action OnDamageTaken;

    void Awake()
    {
        movement = GetComponent<FirstPersonMovement>();
        rb = GetComponent<Rigidbody>();

        if (playerAnimator == null)
            playerAnimator = GetComponentInChildren<Animator>();
    }

    void Start()
    {
        FullResetStatsAndHealth();

        if (deathScreenCanvasGroup != null)
        {
            deathScreenCanvasGroup.alpha = 0f;
            deathScreenCanvasGroup.interactable = false;
            deathScreenCanvasGroup.blocksRaycasts = false;
        }

        if (gameplayUICanvasGroup != null)
        {
            gameplayUICanvasGroup.alpha = 1f;
            gameplayUICanvasGroup.interactable = true;
            gameplayUICanvasGroup.blocksRaycasts = true;
        }

        UpdateUI();
    }

    void OnEnable()
    {
        previousSceneName = SceneManager.GetActiveScene().name;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryMilestoneFullHeal(scene.name, previousSceneName);
        previousSceneName = scene.name;
    }

    void Update()
    {
        if (healthBarFill != null)
        {
            float targetFill = maxHealth > 0 ? currentHealth / maxHealth : 0f;
            healthBarFill.fillAmount = Mathf.Lerp(
                healthBarFill.fillAmount,
                targetFill,
                Time.deltaTime * drainSpeed
            );
        }

    }

    private void TryMilestoneFullHeal(string sceneName, string fromSceneName)
    {
        if (isDead || deathRoutineRunning)
            return;

        bool isPrisonScene = sceneName == prisonSceneName || sceneName.Contains(prisonSceneName);
        if (!isPrisonScene)
            return;

        bool cameFromBoss = fromSceneName == "Boss_Scene" || fromSceneName.Contains("Boss_Scene");
        if (!cameFromBoss)
            return;

        int floor = FloorTextController.floorNumber;

        // Heals on floors 0, 5, 10, 15, etc.
        bool shouldHealThisFloor = (floor >= 0) && (floor % 5 == 0);

        if (!shouldHealThisFloor)
            return;

        if (lastAutoHealFloor == floor)
            return;

        currentHealth = maxHealth;

        if (healthBarFill != null)
            healthBarFill.fillAmount = 1f;

        UpdateUI();
        lastAutoHealFloor = floor;
    }

    public void TakeDamage(float damage)
    {
        if (isDead || deathRoutineRunning) return;

        float reducedDamage = damage - (defense * 0.25f);
        reducedDamage = Mathf.Max(1f, reducedDamage); // optional minimum damage

        currentHealth -= reducedDamage;
        OnDamageTaken?.Invoke(); // for health vignette

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            UpdateUI();
            StartCoroutine(HandleDeathSequence());
            return;
        }

        UpdateUI();
    }

    private IEnumerator HandleDeathSequence()
    {
        if (deathRoutineRunning) yield break;
        deathRoutineRunning = true;
        isDead = true;

        if (movement == null)
            movement = GetComponent<FirstPersonMovement>();

        if (movement != null)
            movement.DisableControlOnDeath();

        if (rb != null)
            rb.linearVelocity = Vector3.zero;

        UpdateDeathStatsText();

        if (playerAnimator != null)
        {
            playerAnimator.ResetTrigger(deathTriggerName);
            playerAnimator.SetTrigger(deathTriggerName);
        }

        yield return new WaitForSeconds(deathAnimationDuration);

        if (deathScreenCanvasGroup != null)
        {
            bool canFade = gameplayUICanvasGroup != null && gameplayUICanvasGroup != deathScreenCanvasGroup;

            if (canFade)
            {
                yield return StartCoroutine(FadeBetweenUI(gameplayUICanvasGroup, deathScreenCanvasGroup, uiFadeDuration));
            }
            else
            {
                deathScreenCanvasGroup.gameObject.SetActive(true);
                deathScreenCanvasGroup.alpha = 1f;
                deathScreenCanvasGroup.interactable = true;
                deathScreenCanvasGroup.blocksRaycasts = true;
            }
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void UpdateDeathStatsText()
    {
        if (deathStatsText == null)
            return;

        int floorAtDeath = Mathf.Max(0, FloorTextController.floorNumber);
        deathStatsText.text = $"[Game Stats]\nFloor Reached: {floorAtDeath}";
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

        currentHealth += heal;
        if (currentHealth > maxHealth)
            currentHealth = maxHealth;

        UpdateUI();
    }

    void UpdateUI()
    {
        if (healthUIText != null)
            healthUIText.text = (int)currentHealth + "/" + (int)maxHealth;
    }

    public void UpdateHealth()
    {
        float bonus = float.Parse(healthBonusText.text);
        maxHealth = baseHealth + bonus;
        currentHealth = maxHealth;

        defense = float.Parse(finalDefenseText.text);

        if (finalHealthText != null)
            finalHealthText.text = maxHealth.ToString();

        if (baseHealthText != null)
            baseHealthText.text = "+" + baseHealth.ToString();

        UpdateUI();
    }

    public void FullResetStatsAndHealth()
    {
        isDead = false;
        deathRoutineRunning = false;
        lastAutoHealFloor = -1;

        float bonus = 0f;
        if (healthBonusText != null)
            float.TryParse(healthBonusText.text, out bonus);

        maxHealth = baseHealth + bonus;
        currentHealth = maxHealth;

        if (finalDefenseText != null)
            float.TryParse(finalDefenseText.text, out defense);
        else
            defense = 0f;

        if (finalHealthText != null)
            finalHealthText.text = maxHealth.ToString();

        if (baseHealthText != null)
            baseHealthText.text = "+" + baseHealth.ToString();

        if (healthBarFill != null)
            healthBarFill.fillAmount = 1f;

        UpdateUI();
    }

    public void ResetForNewRun()
    {
        FullResetStatsAndHealth();

        if (movement == null)
            movement = GetComponent<FirstPersonMovement>();

        if (movement != null)
            movement.ResetPlayerForNewRun();

        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (playerAnimator == null)
            playerAnimator = GetComponentInChildren<Animator>();

        if (playerAnimator != null)
        {
            playerAnimator.ResetTrigger(deathTriggerName);
            playerAnimator.Rebind();
            playerAnimator.Update(0f);
            playerAnimator.Play("Idle", 0, 0f);
        }

        if (gameplayUICanvasGroup != null)
        {
            gameplayUICanvasGroup.alpha = 1f;
            gameplayUICanvasGroup.interactable = true;
            gameplayUICanvasGroup.blocksRaycasts = true;
            gameplayUICanvasGroup.gameObject.SetActive(true);
        }

        if (deathScreenCanvasGroup != null)
        {
            deathScreenCanvasGroup.alpha = 0f;
            deathScreenCanvasGroup.interactable = false;
            deathScreenCanvasGroup.blocksRaycasts = false;
            deathScreenCanvasGroup.gameObject.SetActive(false);
        }

        isDead = false;
        deathRoutineRunning = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
