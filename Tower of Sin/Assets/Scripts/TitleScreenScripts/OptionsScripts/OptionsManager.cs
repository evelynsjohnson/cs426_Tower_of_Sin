using UnityEngine;

public class OptionsManager : MonoBehaviour
{
    [Header("Drag all your TabButton objects here")]
    public TabButton[] tabButtons;

    [Header("The tab that opens by default")]
    public TabButton defaultGameplayTab;

    void OnEnable()
    {
        if (defaultGameplayTab != null)
        {
            OnTabSelected(defaultGameplayTab);
        }
        else if (tabButtons.Length > 0)
        {
            OnTabSelected(tabButtons[0]);
        }
    }

    public void OnTabSelected(TabButton selectedTab)
    {
        foreach (TabButton tab in tabButtons)
        {
            tab.SetActiveState(tab == selectedTab);
        }
    }
}