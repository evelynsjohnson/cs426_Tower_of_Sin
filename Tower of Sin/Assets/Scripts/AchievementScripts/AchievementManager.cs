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

    public bool isUnlocked;
    public string unlockDate;
}

[System.Serializable]
public class AchievementStats
{
    public int prideCount;
    public int greedCount;
    public int envyCount;
    public int gluttonyCount;
    public int lustCount;
    public int slothCount;
    public int wrathCount;

    public int deathCount;

    public int highestFloorReached;
    public int chestsOpened;
    public int enemiesKilled;
    public int minibossesKilled;
    public int sinBossesKilled;

    public int titlesUnlocked;
    public int legendaryItemsFound;

    public bool beatBossNoHit;
    public bool beatBossFast;
    public bool beatBossAtOneHP;
    public bool completedPacifistFloor;
    public bool reachedFloor100Naked;
    public bool completedSpeedFloor;
    public bool killed5Simultaneously;

    public bool allLegendaryCollected;
}

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }

    [Header("Achievement Database")]
    public List<Achievement> achievements = new List<Achievement>();

    [Header("Tracked Stats")]
    public AchievementStats stats = new AchievementStats();

    public event Action OnAchievementUnlocked;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // DontDestroyOnLoad(gameObject);

        LoadStats();
        LoadAchievements();
        CheckAllAchievements();
    }

    // Public stat registration

    public void RegisterPride(int amount = 1)
    {
        stats.prideCount += amount;
        SaveStats();
        CheckAllAchievements();
    }

    public void RegisterGreed(int amount = 1)
    {
        stats.greedCount += amount;
        SaveStats();
        CheckAllAchievements();
    }

    public void RegisterEnvy(int amount = 1)
    {
        stats.envyCount += amount;
        SaveStats();
        CheckAllAchievements();
    }

    public void RegisterGluttony(int amount = 1)
    {
        stats.gluttonyCount += amount;
        SaveStats();
        CheckAllAchievements();
    }

    public void RegisterLust(int amount = 1)
    {
        stats.lustCount += amount;
        SaveStats();
        CheckAllAchievements();
    }

    public void RegisterSloth(int amount = 1)
    {
        stats.slothCount += amount;
        SaveStats();
        CheckAllAchievements();
    }

    public void RegisterWrath(int amount = 1)
    {
        stats.wrathCount += amount;
        SaveStats();
        CheckAllAchievements();
    }

    public void RegisterDeath(int amount = 1)
    {
        stats.deathCount += amount;
        SaveStats();
        CheckAllAchievements();
    }

    public void RegisterFloorReached(int floor)
    {
        if (floor > stats.highestFloorReached)
        {
            stats.highestFloorReached = floor;
            SaveStats();
            CheckAllAchievements();
        }
    }

    public void RegisterChestOpened(int amount = 1)
    {
        stats.chestsOpened += amount;
        SaveStats();
        CheckAllAchievements();
    }

    public void RegisterEnemyKill(int amount = 1)
    {
        stats.enemiesKilled += amount;
        SaveStats();
        CheckAllAchievements();
    }

    public void RegisterMiniBossKill(int amount = 1)
    {
        stats.minibossesKilled += amount;
        SaveStats();
        CheckAllAchievements();
    }

    public void RegisterSinBossKill(int amount = 1)
    {
        stats.sinBossesKilled += amount;
        SaveStats();
        CheckAllAchievements();
    }

    public void RegisterTitleUnlocked(int amount = 1)
    {
        stats.titlesUnlocked += amount;
        SaveStats();
        CheckAllAchievements();
    }

    public void RegisterLegendaryFound(int amount = 1)
    {
        stats.legendaryItemsFound += amount;
        SaveStats();
        CheckAllAchievements();
    }

    public void RegisterAllLegendaryCollected()
    {
        stats.allLegendaryCollected = true;
        SaveStats();
        CheckAllAchievements();
    }

    public void RegisterBossNoHit()
    {
        stats.beatBossNoHit = true;
        SaveStats();
        CheckAllAchievements();
    }

    public void RegisterBossSpeed()
    {
        stats.beatBossFast = true;
        SaveStats();
        CheckAllAchievements();
    }

    public void RegisterBossOneHP()
    {
        stats.beatBossAtOneHP = true;
        SaveStats();
        CheckAllAchievements();
    }

    public void RegisterPacifistFloor()
    {
        stats.completedPacifistFloor = true;
        SaveStats();
        CheckAllAchievements();
    }

    public void RegisterFloor100Naked()
    {
        stats.reachedFloor100Naked = true;
        SaveStats();
        CheckAllAchievements();
    }

    public void RegisterFloorSpeed()
    {
        stats.completedSpeedFloor = true;
        SaveStats();
        CheckAllAchievements();
    }

    public void RegisterKill5Simultaneously()
    {
        stats.killed5Simultaneously = true;
        SaveStats();
        CheckAllAchievements();
    }

    // Unlock logic

    public void UnlockAchievement(string achievementId)
    {
        Achievement ach = achievements.Find(a => a.id == achievementId);

        if (ach != null && !ach.isUnlocked)
        {
            ach.isUnlocked = true;
            ach.unlockDate = DateTime.Now.ToString("MM/dd/yyyy, h:mm tt");
            SaveAchievements();

            OnAchievementUnlocked?.Invoke();
            Debug.Log($"Achievement Unlocked: {ach.title}");
        }
    }

    public void CheckAllAchievements()
    {
        CheckTierAchievements("pride", stats.prideCount, 1, 10, 50);
        CheckTierAchievements("greed", stats.greedCount, 1, 10, 50);
        CheckTierAchievements("envy", stats.envyCount, 1, 10, 50);
        CheckTierAchievements("gluttony", stats.gluttonyCount, 1, 10, 50);
        CheckTierAchievements("lust", stats.lustCount, 1, 10, 50);
        CheckTierAchievements("sloth", stats.slothCount, 1, 10, 50);
        CheckTierAchievements("wrath", stats.wrathCount, 1, 10, 50);

        CheckTierAchievements("death", stats.deathCount, 1, 10, 50, 100);
        CheckTierAchievements("enemy", stats.enemiesKilled, 50, 100, 200);
        CheckTierAchievements("chest", stats.chestsOpened, 1, 10, 50);
        CheckTierAchievements("miniboss", stats.minibossesKilled, 1, 10, 50);
        CheckTierAchievements("sinboss", stats.sinBossesKilled, 10, 50, 100);

        if (stats.highestFloorReached >= 50) UnlockAchievement("floor_50");
        if (stats.highestFloorReached >= 100) UnlockAchievement("floor_100");
        if (stats.highestFloorReached >= 200) UnlockAchievement("floor_200");
        if (stats.highestFloorReached >= 1000) UnlockAchievement("floor_1000");

        if (stats.titlesUnlocked >= 1) UnlockAchievement("title_1");
        if (stats.legendaryItemsFound >= 1) UnlockAchievement("legendary_1");
        if (stats.allLegendaryCollected) UnlockAchievement("legendary_all");

        if (stats.beatBossNoHit) UnlockAchievement("boss_nohit");
        if (stats.beatBossFast) UnlockAchievement("boss_speed");
        if (stats.beatBossAtOneHP) UnlockAchievement("boss_1hp");

        if (stats.completedPacifistFloor) UnlockAchievement("floor_pacifist");
        if (stats.reachedFloor100Naked) UnlockAchievement("floor_100_naked");
        if (stats.completedSpeedFloor) UnlockAchievement("floor_speed");

        if (stats.killed5Simultaneously) UnlockAchievement("kill_5_simul");
    }

    private void CheckTierAchievements(string baseId, int value, params int[] thresholds)
    {
        foreach (int threshold in thresholds)
        {
            if (value >= threshold)
            {
                UnlockAchievement(baseId + "_" + threshold);
            }
        }
    }

    // Save / Load achievements

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

    // Save / Load stats
    private void SaveStats()
    {
        PlayerPrefs.SetInt("Stat_prideCount", stats.prideCount);
        PlayerPrefs.SetInt("Stat_greedCount", stats.greedCount);
        PlayerPrefs.SetInt("Stat_envyCount", stats.envyCount);
        PlayerPrefs.SetInt("Stat_gluttonyCount", stats.gluttonyCount);
        PlayerPrefs.SetInt("Stat_lustCount", stats.lustCount);
        PlayerPrefs.SetInt("Stat_slothCount", stats.slothCount);
        PlayerPrefs.SetInt("Stat_wrathCount", stats.wrathCount);

        PlayerPrefs.SetInt("Stat_deathCount", stats.deathCount);
        PlayerPrefs.SetInt("Stat_highestFloorReached", stats.highestFloorReached);
        PlayerPrefs.SetInt("Stat_chestsOpened", stats.chestsOpened);
        PlayerPrefs.SetInt("Stat_enemiesKilled", stats.enemiesKilled);
        PlayerPrefs.SetInt("Stat_minibossesKilled", stats.minibossesKilled);
        PlayerPrefs.SetInt("Stat_sinBossesKilled", stats.sinBossesKilled);
        PlayerPrefs.SetInt("Stat_titlesUnlocked", stats.titlesUnlocked);
        PlayerPrefs.SetInt("Stat_legendaryItemsFound", stats.legendaryItemsFound);

        PlayerPrefs.SetInt("Stat_beatBossNoHit", stats.beatBossNoHit ? 1 : 0);
        PlayerPrefs.SetInt("Stat_beatBossFast", stats.beatBossFast ? 1 : 0);
        PlayerPrefs.SetInt("Stat_beatBossAtOneHP", stats.beatBossAtOneHP ? 1 : 0);
        PlayerPrefs.SetInt("Stat_completedPacifistFloor", stats.completedPacifistFloor ? 1 : 0);
        PlayerPrefs.SetInt("Stat_reachedFloor100Naked", stats.reachedFloor100Naked ? 1 : 0);
        PlayerPrefs.SetInt("Stat_completedSpeedFloor", stats.completedSpeedFloor ? 1 : 0);
        PlayerPrefs.SetInt("Stat_killed5Simultaneously", stats.killed5Simultaneously ? 1 : 0);
        PlayerPrefs.SetInt("Stat_allLegendaryCollected", stats.allLegendaryCollected ? 1 : 0);

        PlayerPrefs.Save();
    }

    private void LoadStats()
    {
        stats.prideCount = PlayerPrefs.GetInt("Stat_prideCount", 0);
        stats.greedCount = PlayerPrefs.GetInt("Stat_greedCount", 0);
        stats.envyCount = PlayerPrefs.GetInt("Stat_envyCount", 0);
        stats.gluttonyCount = PlayerPrefs.GetInt("Stat_gluttonyCount", 0);
        stats.lustCount = PlayerPrefs.GetInt("Stat_lustCount", 0);
        stats.slothCount = PlayerPrefs.GetInt("Stat_slothCount", 0);
        stats.wrathCount = PlayerPrefs.GetInt("Stat_wrathCount", 0);

        stats.deathCount = PlayerPrefs.GetInt("Stat_deathCount", 0);
        stats.highestFloorReached = PlayerPrefs.GetInt("Stat_highestFloorReached", 0);
        stats.chestsOpened = PlayerPrefs.GetInt("Stat_chestsOpened", 0);
        stats.enemiesKilled = PlayerPrefs.GetInt("Stat_enemiesKilled", 0);
        stats.minibossesKilled = PlayerPrefs.GetInt("Stat_minibossesKilled", 0);
        stats.sinBossesKilled = PlayerPrefs.GetInt("Stat_sinBossesKilled", 0);
        stats.titlesUnlocked = PlayerPrefs.GetInt("Stat_titlesUnlocked", 0);
        stats.legendaryItemsFound = PlayerPrefs.GetInt("Stat_legendaryItemsFound", 0);

        stats.beatBossNoHit = PlayerPrefs.GetInt("Stat_beatBossNoHit", 0) == 1;
        stats.beatBossFast = PlayerPrefs.GetInt("Stat_beatBossFast", 0) == 1;
        stats.beatBossAtOneHP = PlayerPrefs.GetInt("Stat_beatBossAtOneHP", 0) == 1;
        stats.completedPacifistFloor = PlayerPrefs.GetInt("Stat_completedPacifistFloor", 0) == 1;
        stats.reachedFloor100Naked = PlayerPrefs.GetInt("Stat_reachedFloor100Naked", 0) == 1;
        stats.completedSpeedFloor = PlayerPrefs.GetInt("Stat_completedSpeedFloor", 0) == 1;
        stats.killed5Simultaneously = PlayerPrefs.GetInt("Stat_killed5Simultaneously", 0) == 1;
        stats.allLegendaryCollected = PlayerPrefs.GetInt("Stat_allLegendaryCollected", 0) == 1;
    }
}