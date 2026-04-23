using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Runtime.Serialization;
using System.Security.AccessControl;
using System;
using System.Diagnostics;

using TMPro;
using System.Drawing;


public class ItemButton : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public ItemData item;

    public GameObject itemStatsPanel;
    public TextMeshProUGUI itemStatsText;
    public RawImage itemPicture;

    public void OnPointerClick(PointerEventData eventData)
    {
        InvManager.Instance.Equip(item);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        UnityEngine.Debug.Log("Activation");
        ShowStats();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UnityEngine.Debug.Log("Deactivation");
        HideStats();
    }

    public void ShowStats()
    {
        UnityEngine.Debug.Log("Tring to show stats");
        if (itemStatsPanel != null && item.icon != null)
        {
            UnityEngine.Debug.Log("Start of trying");
            itemStatsPanel.SetActive(true);

            string itemText = item.name + "\nATK: +" + item.damage + "\nDEF: +" + item.defense + "\nHP: +" + item.health;

            itemStatsText.text = itemText;
            UnityEngine.Color hue = getColor(item.rank);
            itemPicture.texture = item.icon;
            itemPicture.color = hue;
            UnityEngine.Debug.Log("End of trying");
        }

    }

    public void HideStats()
    {
        if (itemStatsPanel != null && item.icon != null)
        {
            itemStatsPanel.SetActive(false);
        }

    }

    UnityEngine.Color getColor(Rarity rank)
    {
        switch (rank)
        {
            case Rarity.Common:
                return UnityEngine.Color.white;
            case Rarity.Uncommon:
                return UnityEngine.Color.green;
            case Rarity.Rare:
                return UnityEngine.Color.blue;
            case Rarity.Epic:
                return UnityEngine.Color.magenta;
            case Rarity.Legendary:
                return UnityEngine.Color.yellow;
            default:
                return UnityEngine.Color.white;
        }
    }
}
