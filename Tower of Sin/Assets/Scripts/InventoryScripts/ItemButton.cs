using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Runtime.Serialization;
using System.Security.AccessControl;
using System;
using System.Diagnostics;

using TMPro;


public class ItemButton : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public ItemData item;

    public GameObject itemStatsPanel;
    public TextMeshProUGUI itemStatsText;

    public void OnPointerClick(PointerEventData eventData)
    {
        InvManager.Instance.Equip(item);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // UnityEngine.Debug.Log("Activation");
        ShowStats();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // UnityEngine.Debug.Log("Deactivation");
        HideStats();
    }

    public void ShowStats()
    {
        // UnityEngine.Debug.Log("Tring to show stats");
        if (itemStatsPanel != null)
        {
            // UnityEngine.Debug.Log("Step 1");
            itemStatsPanel.SetActive(true);

            string itemText = item.name + "\nATK: +" + item.damage + "\nDEF: +" + item.defense + "\nHP: +" + item.health;

            itemStatsText.text = itemText;
        }

    }

    public void HideStats()
    {
        if (itemStatsPanel != null)
        {
            itemStatsPanel.SetActive(false);
        }

    }
}
