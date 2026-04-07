using UnityEngine;

public class ShowObj : MonoBehaviour
{
    private bool lastState = true;

    void Update()
    {
        bool shouldShow = (FloorTextController.floorNumber <= 4);

        if (shouldShow != lastState)
        {
            ToggleAllChildren(shouldShow);
            lastState = shouldShow;
        }
    }

    private void ToggleAllChildren(bool state)
    {
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(state);
        }

    }
}