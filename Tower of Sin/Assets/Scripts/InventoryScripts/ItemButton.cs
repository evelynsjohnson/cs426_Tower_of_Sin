using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Runtime.Serialization;
using System.Security.AccessControl;
using System;


public class ItemButton : MonoBehaviour, IPointerClickHandler
{
    public ItemData item;

    public void OnPointerClick(PointerEventData eventData)
    {
        InvManager.Instance.Equip(item);
    }
}
