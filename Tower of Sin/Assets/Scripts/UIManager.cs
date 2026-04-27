using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// access the Text Mesh Pro namespace
using TMPro;

public class UIManager : MonoBehaviour
{

    public int weapon;

    public TMP_Text PotionsText;

    public int maxPotions;
    private int numPotions;

    public TextMeshProUGUI pickupText;
    public GameObject pickupUI;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        weapon = 0;
        numPotions = 0;
        if (PotionsText != null)
            PotionsText.text = "" + numPotions + "/" + maxPotions;
    }

    // Update is called once per frame
    public void AddPotion()
    {
        if (numPotions < maxPotions)
        {
            numPotions++;
            if (PotionsText != null)
                PotionsText.text = "" + numPotions + "/" + maxPotions;
        }
    }
    void SubPotion()
    {
        numPotions--;
        if (PotionsText != null)
            PotionsText.text = "" + numPotions + "/" + maxPotions;
    }

    void changeWeapon(int newWeapon)
    {
        weapon = newWeapon;
    }

    public void ShowPickup(string message)
    {
        StartCoroutine(ShowPickupCoroutine(message));
    }

    private IEnumerator ShowPickupCoroutine(string message)
    {
        pickupText.text = message;
        pickupUI.SetActive(true);

        yield return new WaitForSeconds(2f);

        pickupUI.SetActive(false);
    }
}
