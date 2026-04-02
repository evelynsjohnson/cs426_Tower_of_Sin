using UnityEngine;

public class DungeonManager : MonoBehaviour
{
    [Header("Dungeon Components")]
    public RandomPlayerSpawner randomPlayerSpawner;
    public PlayerSpawnHandler fixedPlayerSpawner;
    public PortalManager portalManager;
    public LootSpawner lootSpawner;
    public PrisonZombieScript zombieSpawner;

    void Start()
    {
        GenerateDungeon();
    }

    public void GenerateDungeon()
    {
        if (fixedPlayerSpawner != null) fixedPlayerSpawner.SpawnPlayer();
        if (randomPlayerSpawner != null) randomPlayerSpawner.SpawnPlayerRandomly();
        if (portalManager != null) portalManager.RandomizePortals();
        if (lootSpawner != null) lootSpawner.RandomizeLoot();
        if (zombieSpawner != null) zombieSpawner.SpawnZombies();

    }
}