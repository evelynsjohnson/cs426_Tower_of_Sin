using UnityEngine;

public class PortalManager : MonoBehaviour
{
    public Transform portalsContainer;

    private Portal_Controller[] dungeonPortals;

    void Start()
    {
        if (portalsContainer == null) return;

        dungeonPortals = portalsContainer.GetComponentsInChildren<Portal_Controller>(true);

        if (dungeonPortals.Length == 0) return;

        int winningIndex = Random.Range(0, dungeonPortals.Length);

        for (int i = 0; i < dungeonPortals.Length; i++)
        {
            if (i == winningIndex)
            {
                dungeonPortals[i].TogglePortal(true);
            }
            else
            {
                dungeonPortals[i].TogglePortal(false);
            }
        }
    }
}