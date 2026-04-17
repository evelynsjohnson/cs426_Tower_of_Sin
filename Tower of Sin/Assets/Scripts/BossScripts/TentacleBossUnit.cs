using UnityEngine;
using UnityEngine.UI;

public class TentacleBossUnit : MonoBehaviour
{
    [SerializeField] private float maxHP = 100f;
    [SerializeField] private float currentHP = 100f;
    [SerializeField] private Image hpFill;
    [SerializeField] private Transform player;

    private GreedAI owner;

    public void Initialize(GreedAI greed, float hp, Transform targetPlayer)
    {
        owner = greed;
        maxHP = hp;
        currentHP = hp;
        player = targetPlayer;
        UpdateUI();
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0f) return;

        currentHP -= amount;
        currentHP = Mathf.Max(0f, currentHP);
        UpdateUI();

        if (currentHP <= 0f)
        {
            if (owner != null)
                owner.NotifyTentacleDied(gameObject);

            Destroy(gameObject);
        }
    }

    private void UpdateUI()
    {
        if (hpFill != null)
            hpFill.fillAmount = maxHP > 0f ? currentHP / maxHP : 0f;
    }
}