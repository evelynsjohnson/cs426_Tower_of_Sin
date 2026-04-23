using UnityEngine;

using System.Collections.Generic;
using UnityEngine;
using System;
using System.ComponentModel;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Runtime.Serialization;
using System.Security.AccessControl;
using TMPro;

public class OpenInventory : MonoBehaviour
{
    public Transform player;
    public float activationDistance = 3f;
    public GameObject objectToShow;

    public GameObject playerController;

    public List<ItemData> inventory;

    void Update()
    {
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

        playerController = GameObject.FindGameObjectWithTag("Player");
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
        GameObject r1 = GameObject.FindGameObjectWithTag("InventorySlots");

        InvManager manager = playerController.GetComponent<InvManager>();

        inventory = manager.getInventory();
    }
}