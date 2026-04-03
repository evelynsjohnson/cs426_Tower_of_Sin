using UnityEngine;

public class ShowObj : MonoBehaviour
{
    private bool lastState = true;

    void Update()
    {
        bool shouldShow = (FloorTextController.floorNumber == 0);

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

        Debug.Log("Floor is " + FloorTextController.floorNumber + ". Children set to: " + state);
    }
}