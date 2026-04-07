using UnityEngine;
using System.Collections;

public class DungeonManager : MonoBehaviour
{
    public PlayerSpawnHandler playerSpawnHandler;
    public RandomPlayerSpawner randomPlayerSpawner;
    public PortalManager portalManager;
    public LootSpawner lootSpawner;
    public PrisonZombieScript zombieSpawner;

    void Start()
    {
        StartCoroutine(GenerateNextFrame());
    }

    IEnumerator GenerateNextFrame()
    {
        yield return null;
        GenerateDungeon();
    }

    public void GenerateDungeon()
    {
        //Debug.Log("GenerateDungeon called");

        //if (playerSpawnHandler != null)
        //{
        //    Debug.Log("Calling SpawnPlayer");
        //    playerSpawnHandler.SpawnPlayer();
        //}
        //else
        //{
        //    Debug.Log("playerSpawnHandler is NULL");
        //}

        if (randomPlayerSpawner != null)
        {
            randomPlayerSpawner.SpawnPlayerRandomly();
        }
        else
        {
            Debug.Log("randomPlayerSpawner is NULL");
        }

        if (portalManager != null) portalManager.RandomizePortals();
        if (lootSpawner != null) lootSpawner.RandomizeLoot();
        if (zombieSpawner != null) zombieSpawner.SpawnZombies();
    }
}