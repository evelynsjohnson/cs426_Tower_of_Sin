using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class AchievementUIManager : MonoBehaviour
{
    [Header("UI References")]
    public Transform achievementContentParent;
    public GameObject achievementPrefab;

    [Header("Progress Bar References")]
    public Image progressBarFill;
    public TextMeshProUGUI earnedText;
    public TextMeshProUGUI percentText;

    private List<GameObject> spawnedUIItems = new List<GameObject>();

    private void Start()
    {
        // UI updates live
        if (AchievementManager.Instance != null)
        {
            AchievementManager.Instance.OnAchievementUnlocked += RefreshUI;
            RefreshUI(); // Initial pop
        }
    }

    private void OnDestroy()
    {
        if (AchievementManager.Instance != null)
        {
            AchievementManager.Instance.OnAchievementUnlocked -= RefreshUI;
        }
    }

    private void RefreshUI()
    {
        foreach (var item in spawnedUIItems) Destroy(item);
        spawnedUIItems.Clear();

        int unlockedCount = 0;
        int totalAchievements = AchievementManager.Instance.achievements.Count;

        // Spawn UI items
        foreach (Achievement ach in AchievementManager.Instance.achievements)
        {
            GameObject newObj = Instantiate(achievementPrefab, achievementContentParent);
            spawnedUIItems.Add(newObj);

            AchievementUIItem uiScript = newObj.GetComponent<AchievementUIItem>();
            uiScript.Setup(ach);

            if (ach.isUnlocked) unlockedCount++;
        }

        // Update Bar
        earnedText.text = $"{unlockedCount} OF {totalAchievements} ACHIEVEMENTS EARNED";

        float percentage = totalAchievements > 0 ? (float)unlockedCount / totalAchievements : 0;
        percentText.text = $"({Mathf.RoundToInt(percentage * 100)}%)";

        if (progressBarFill != null)
        {
            progressBarFill.fillAmount = percentage;
        }
    }
}