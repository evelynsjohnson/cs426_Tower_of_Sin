using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AchievementUIItem : MonoBehaviour
{
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descText;
    public TextMeshProUGUI dateText;
    public RawImage backgroundImage;

    public Texture lockedTexture;
    public Texture unlockedTexture;

    public void Setup(Achievement ach)
    {
        titleText.text = ach.isHidden && !ach.isUnlocked ? "???" : ach.title;
        descText.text = ach.isHidden && !ach.isUnlocked ? "Hidden Achievement" : ach.description;

        if (ach.isUnlocked)
        {
            dateText.text = $"Unlocked {ach.unlockDate}";
            backgroundImage.texture = unlockedTexture;
        }
        else
        {
            dateText.text = "";
            backgroundImage.texture = lockedTexture;
        }
    }
}