using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Runtime.Serialization;
using System.Security.AccessControl;
using System;

[System.Serializable]
public class ItemData
{
    public ItemType type;
    public Texture icon;
    public int health;
    public int damage;
    public int defense;
}

public enum ItemType
{
    Helmet, Chest, Pant, Boot, Weapon, Neck, Ring
}


public class ItemButton : MonoBehaviour, IPointerClickHandler
{
    public ItemData item;

    public void OnPointerClick(PointerEventData eventData)
    {
        InvManager.Instance.Equip(item);
    }
}
