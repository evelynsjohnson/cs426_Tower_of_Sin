using System.Collections.Generic;
using UnityEngine;
using System;

[System.Serializable]
public class Achievement
{
    public string id;
    public string title; 
    [TextArea] public string description;
    public bool isHidden;

    // Tracked data
    public bool isUnlocked;
    public string unlockDate;

}

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }

    [Header("Achievement Database")]
    public List<Achievement> achievements = new List<Achievement>();

    // update when something unlocks
    public event Action OnAchievementUnlocked;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        LoadAchievements();
    }

    public void UnlockAchievement(string achievementId)
    {
        Achievement ach = achievements.Find(a => a.id == achievementId);

        if (ach != null && !ach.isUnlocked)
        {
            ach.isUnlocked = true;
            ach.unlockDate = DateTime.Now.ToString("MM/dd/yyyy, h:mm tt");
            SaveAchievements();

            // UI to update
            OnAchievementUnlocked?.Invoke();

            Debug.Log($"Achievement Unlocked: {ach.title}");
        }
    }

    private void SaveAchievements()
    {
        foreach (var ach in achievements)
        {
            PlayerPrefs.SetInt("Ach_" + ach.id, ach.isUnlocked ? 1 : 0);
            if (ach.isUnlocked)
            {
                PlayerPrefs.SetString("AchDate_" + ach.id, ach.unlockDate);
            }
        }
        PlayerPrefs.Save();
    }

    private void LoadAchievements()
    {
        foreach (var ach in achievements)
        {
            ach.isUnlocked = PlayerPrefs.GetInt("Ach_" + ach.id, 0) == 1;
            if (ach.isUnlocked)
            {
                ach.unlockDate = PlayerPrefs.GetString("AchDate_" + ach.id, "");
            }
        }
    }



}


