using UnityEngine;
using System.Collections.Generic;
using System;
using System.ComponentModel;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Runtime.Serialization;
using System.Security.AccessControl;
using TMPro;

using System.Diagnostics;
using System.Drawing;

public class OpenInventory : MonoBehaviour
{
    public Transform player;
    public float activationDistance = 3f;
    public GameObject objectToShow;

    public GameObject playerController;

    public List<ItemData> inventory;

    public GameObject slots;

    void Start()
    {
        playerController = GameObject.FindGameObjectWithTag("Player");
    }
    void Update()
    {
        if (player == null)
        {
            if (playerController != null)
            {
                player = playerController.transform;
            }
            else
            {
                playerController = GameObject.FindGameObjectWithTag("Player");
                if (playerController != null) player = playerController.transform;
            }
        }
        if (player == null || objectToShow == null) return;

        if (Time.timeScale == 0f && !objectToShow.activeSelf) return; // other UI is open

        float distance = Vector3.Distance(transform.position, player.position);

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (!objectToShow.activeSelf && distance <= activationDistance)
            {
                OpenTheInventory();
            }
            else if (objectToShow.activeSelf)
            {
                CloseTheInventory();
            }
        }

    }

    private void OpenTheInventory()
    {
        fillInventory();

        objectToShow.SetActive(true);

        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void CloseTheInventory()
    {
        objectToShow.SetActive(false);

        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void fillInventory()
    {
        InvManager manager = playerController.GetComponent<InvManager>();

        inventory = manager.getInventory();

        int index = 0;
        foreach (Transform row in slots.transform)
        {
            foreach (Transform slot in row)
            {
                if (index >= inventory.Count) return;
                RawImage image = slot.GetComponent<RawImage>();
                image.texture = inventory[index].icon;
                UnityEngine.Color temp = image.color;
                temp.a = 1.0f;
                image.color = temp;
                ItemButton button = slot.GetComponent<ItemButton>();
                button.item = inventory[index];
                index++;
            }
        }
    }
}