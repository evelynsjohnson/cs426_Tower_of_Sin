using UnityEngine;

public class PortalManager : MonoBehaviour
{
    public Transform portalsContainer;
    private Portal_Controller[] dungeonPortals;

    public void RandomizePortals()
    {
        if (portalsContainer == null) return;

        dungeonPortals = portalsContainer.GetComponentsInChildren<Portal_Controller>(true);

        if (dungeonPortals.Length == 0) return;

        int winningIndex = Random.Range(0, dungeonPortals.Length);

        for (int i = 0; i < dungeonPortals.Length; i++)
        {
            bool isWinning = (i == winningIndex);

            dungeonPortals[i].TogglePortal(isWinning);

            Transform teleporter = dungeonPortals[i].transform.Find("TeleporterCube");

            if (teleporter != null)
            {
                teleporter.gameObject.SetActive(isWinning);
            }
        }
    }
}