using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Runtime.Serialization;
using System.Security.AccessControl;
using System;

using TMPro;

[System.Serializable]
public class ItemData
{
    public ItemType type;
    public Texture icon;
    public int health;
    public int damage;
    public int defense;
    public string name;

    public Rarity rank;
}

public enum ItemType
{
    Helmet, Chest, Pant, Boot, Weapon, Neck, Ring
}

public enum Rarity
{
    Common, Uncommon, Rare, Epic, Legendary
}