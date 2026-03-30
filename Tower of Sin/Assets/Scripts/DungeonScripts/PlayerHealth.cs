using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 200f;
    private float currentHealth;

    public TextMeshProUGUI healthUIText;
    public Image healthBarFill;

    public float drainSpeed = 5f;

    void Start()
    {
        currentHealth = maxHealth;

        if (healthBarFill != null) healthBarFill.fillAmount = 1f;

        UpdateUI();
    }

    void Update()
    {
        if (healthBarFill != null)
        {
            float targetFill = currentHealth / maxHealth;

            healthBarFill.fillAmount = Mathf.Lerp(healthBarFill.fillAmount, targetFill, Time.deltaTime * drainSpeed);
        }
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            FloorTextController.floorNumber = 0;
            UpdateUI();
            SceneManager.LoadScene("Death_Realm");
        }
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