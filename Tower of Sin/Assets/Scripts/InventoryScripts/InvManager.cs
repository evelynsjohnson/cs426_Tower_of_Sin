using System.Collections.Generic;
using UnityEngine;
using System;
using System.ComponentModel;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Runtime.Serialization;
using System.Security.AccessControl;
using TMPro;



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

    public TextMeshProUGUI attackBonusText; //Inv UI attack bonus text
    public TextMeshProUGUI defenseBonusText; //Inv UI defense bonus text
    public TextMeshProUGUI healthBonusText; //Inv UI health bonus text
    public TextMeshProUGUI finalAttackText; //Inv UI final attack text
    public TextMeshProUGUI finalDefenseText; //Inv UI final defense text
    public TextMeshProUGUI finalHealthText; //Inv UI final health text

    private PlayerHealth healthManager;

    private FirstPersonMovement attackManager;

    public List<ItemData> inventory = new List<ItemData>();

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
                if (helmet.texture.Equals(item.icon)) //uneqip items
                {
                    helmet.texture = helmetBase;
                }
                else
                {
                    helmet.texture = item.icon;
                }
                updateStats(item, helmet.texture.Equals(item.icon));
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
                updateStats(item, chest.texture.Equals(item.icon));
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
                updateStats(item, pant.texture.Equals(item.icon));
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
                updateStats(item, boot.texture.Equals(item.icon));
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
                updateStats(item, weapon.texture.Equals(item.icon));
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
                updateStats(item, neck.texture.Equals(item.icon));
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
                updateStats(item, ring1.texture.Equals(item.icon));
                break;
        }
        healthManager = GetComponentInParent<PlayerHealth>();
        healthManager.UpdateHealth();

        attackManager = GetComponentInParent<FirstPersonMovement>();
        attackManager.UpdateAttack();
    }

    private void updateStats(ItemData item, bool add)
    {
        if (!add)
        {
            attackBonusText.text = "+" + (float.Parse(attackBonusText.text) - item.damage).ToString();
            defenseBonusText.text = "+" + (float.Parse(defenseBonusText.text) - item.defense).ToString();
            healthBonusText.text = "+" + (float.Parse(healthBonusText.text) - item.health).ToString();

            finalAttackText.text = "+" + (float.Parse(finalAttackText.text) - item.damage).ToString();
            finalDefenseText.text = "+" + (float.Parse(finalDefenseText.text) - item.defense).ToString();
            finalHealthText.text = "+" + (float.Parse(finalHealthText.text) - item.health).ToString();
        }
        else
        {
            attackBonusText.text = "+" + (float.Parse(attackBonusText.text) + item.damage).ToString();
            defenseBonusText.text = "+" + (float.Parse(defenseBonusText.text) + item.defense).ToString();
            healthBonusText.text = "+" + (float.Parse(healthBonusText.text) + item.health).ToString();

            finalAttackText.text = "+" + (float.Parse(finalAttackText.text) + item.damage).ToString();
            finalDefenseText.text = "+" + (float.Parse(finalDefenseText.text) + item.defense).ToString();
            finalHealthText.text = "+" + (float.Parse(finalHealthText.text) + item.health).ToString();
        }

    }

    public void AddItem(ItemData item)
    {
        inventory.Add(item);
    }

    public List<ItemData> getInventory()
    {
        return inventory;
    }
}