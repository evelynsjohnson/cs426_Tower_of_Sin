using UnityEngine;
using UnityEngine.UI;

// access the Text Mesh Pro namespace
using TMPro;

public class UIManager : MonoBehaviour
{

    public int weapon;

    public TMP_Text PotionsText;
    private int numPotions;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        weapon = 0;
        numPotions = 0;
        PotionsText.text = "" + numPotions + "/5";
    }

    // Update is called once per frame
    void AddPotion()
    {
        numPotions++;
        PotionsText.text = "" + numPotions + "/5";
    }
    void SubPotion()
    {
        numPotions--;
        PotionsText.text = "" + numPotions + "/5";
    }

    void changeWeapon(int newWeapon)
    {
        weapon = newWeapon;
    }
}
