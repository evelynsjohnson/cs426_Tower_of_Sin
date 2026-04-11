using System.Collections.Generic;
using UnityEngine;
using System;
using System.ComponentModel;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Runtime.Serialization;
using System.Security.AccessControl;


public class InvManager : MonoBehaviour
{
    public MonoBehaviour playerMovementScript;
    public MonoBehaviour cameraMovementScript;
    public Rigidbody playerRigidbody;
    public Animator playerAnimator;

    public static InvManager Instance;

    public RawImage helmet;
    public RawImage chest;
    public RawImage pant;
    public RawImage boot;
    public RawImage weapon;
    public RawImage neck;
    public RawImage ring1;
    public RawImage ring2;

    public Texture helmetBase;
    public Texture chestBase;
    public Texture pantBase;
    public Texture bootBase;
    public Texture weaponBase;
    public Texture neckBase;
    public Texture ringBase;

    void OnEnable()
    {
        if (playerMovementScript != null) playerMovementScript.enabled = false;
        if (cameraMovementScript != null) cameraMovementScript.enabled = false;

        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }

        // Freeze the animation in place
        if (playerAnimator != null)
        {
            playerAnimator.speed = 0f;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void OnDisable()
    {
        if (playerMovementScript != null) playerMovementScript.enabled = true;
        if (cameraMovementScript != null) cameraMovementScript.enabled = true;

        // Resume the animation
        if (playerAnimator != null)
        {
            playerAnimator.speed = 1f;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Awake()
    {
        Instance = this;
    }

    public void Equip(ItemData item)
    {
        switch (item.type)
        {
            case ItemType.Helmet:
                if (helmet.texture.Equals(item.icon))
                {
                    helmet.texture = helmetBase;
                }
                else
                {
                    helmet.texture = item.icon;
                }
                break;
            case ItemType.Chest:
                if (chest.texture.Equals(item.icon))
                {
                    chest.texture = chestBase;
                }
                else
                {
                    chest.texture = item.icon;
                }
                break;
            case ItemType.Pant:
                if (pant.texture.Equals(item.icon))
                {
                    pant.texture = pantBase;
                }
                else
                {
                    pant.texture = item.icon;
                }
                break;
            case ItemType.Boot:
                if (boot.texture.Equals(item.icon))
                {
                    boot.texture = bootBase;
                }
                else
                {
                    boot.texture = item.icon;
                }
                break;
            case ItemType.Weapon:
                if (weapon.texture.Equals(item.icon))
                {
                    weapon.texture = weaponBase;
                }
                else
                {
                    weapon.texture = item.icon;
                }
                break;
            case ItemType.Neck:
                if (neck.texture.Equals(item.icon))
                {
                    neck.texture = neckBase;
                }
                else
                {
                    neck.texture = item.icon;
                }
                break;
            case ItemType.Ring:
                if (ring1.texture.Equals(item.icon))
                {
                    ring1.texture = ringBase;
                }
                else
                {
                    ring1.texture = item.icon;
                }
                break;
        }
    }
}