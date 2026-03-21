using System;
using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Stats")]
    public int maxHP = 50;
    public int atk = 10;

    [Header("Death")]
    [Tooltip("Seconds to wait after death before the GameObject is destroyed (let the death animation finish)")]
    public float deathDelay = 2.5f;

    public bool IsDead { get; private set; }

    // Fires the moment the enemy's HP hits 0
    public event Action OnDeath;

    private int currentHP;
    private UIManager playerUI;

    protected void Start()
    {
        currentHP = maxHP;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerUI = player.GetComponentInChildren<UIManager>();
            if (playerUI == null)
                playerUI = FindFirstObjectByType<UIManager>();
        }
    }

    public void TakeDamage(int amount)
    {
        if (IsDead) return;

        currentHP -= amount;

        if (currentHP <= 0)
            Die();
    }

    void Die()
    {
        IsDead = true;

        // Drop loot
        if (playerUI != null)
            playerUI.AddPotion();

        // Notify listeners (EnemyAnimator will play the death clip)
        OnDeath?.Invoke();

        // Destroy after the animation has had time to play
        StartCoroutine(DestroyAfterDelay());
    }

    IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(deathDelay);
        Destroy(gameObject);
    }
}
