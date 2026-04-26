using UnityEngine;

public class DeathRealmReset : MonoBehaviour
{
    void Start()
    {
        PlayerHealth ph = Object.FindFirstObjectByType<PlayerHealth>();

        if (ph != null)
        {
            ph.ResetForNewRun();
            Debug.Log("[DeathRealmReset] Player reset successfully.");
        }
        else
        {
            Debug.LogError("[DeathRealmReset] No PlayerHealth found in Death_Realm.");
        }
    }
}