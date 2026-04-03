using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OptionsResetButton : MonoBehaviour
{
    [Header("Tab Screens")]
    public GameObject gameplayScreen;
    public GameObject videoScreen;
    public GameObject audioScreen;
    public GameObject keybindsScreen;

    [Header("Defaults")]
    public float defaultMasterVolume = 0.25f;
    public float defaultShakeAmount = .75f;

    public bool defaultFullscreen = true;
    public int defaultQualityIndex = 2;
    public int defaultResolutionIndex = 0;

    public void ResetCurrentTab()
    {
        if (gameplayScreen != null && gameplayScreen.activeInHierarchy)
        {
            ResetGameplay();
        }
        else if (videoScreen != null && videoScreen.activeInHierarchy)
        {
            ResetVideo();
        }
        else if (audioScreen != null && audioScreen.activeInHierarchy)
        {
            ResetAudio();
        }
        else if (keybindsScreen != null && keybindsScreen.activeInHierarchy)
        {
            ResetKeybinds();
        }
    }

    void ResetGameplay()
    {
        Slider[] sliders = gameplayScreen.GetComponentsInChildren<Slider>(true);
        Toggle[] toggles = gameplayScreen.GetComponentsInChildren<Toggle>(true);
        TMP_Dropdown[] tmpDropdowns = gameplayScreen.GetComponentsInChildren<TMP_Dropdown>(true);
        Dropdown[] dropdowns = gameplayScreen.GetComponentsInChildren<Dropdown>(true);

        foreach (Slider slider in sliders)
        {
            if (slider.name.ToLower().Contains("shake"))
            {
                slider.value = defaultShakeAmount;
            }
        }

        Debug.Log("Gameplay settings reset.");
    }

    void ResetVideo()
    {
        Slider[] sliders = videoScreen.GetComponentsInChildren<Slider>(true);
        Toggle[] toggles = videoScreen.GetComponentsInChildren<Toggle>(true);
        TMP_Dropdown[] tmpDropdowns = videoScreen.GetComponentsInChildren<TMP_Dropdown>(true);
        Dropdown[] dropdowns = videoScreen.GetComponentsInChildren<Dropdown>(true);

        foreach (Toggle toggle in toggles)
        {
            if (toggle.name.ToLower().Contains("fullscreen"))
            {
                toggle.isOn = defaultFullscreen;
            }
        }

        foreach (TMP_Dropdown dropdown in tmpDropdowns)
        {
            string n = dropdown.name.ToLower();

            if (n.Contains("quality"))
                dropdown.value = defaultQualityIndex;
            else if (n.Contains("resolution"))
                dropdown.value = defaultResolutionIndex;
        }

        foreach (Dropdown dropdown in dropdowns)
        {
            string n = dropdown.name.ToLower();

            if (n.Contains("quality"))
                dropdown.value = defaultQualityIndex;
            else if (n.Contains("resolution"))
                dropdown.value = defaultResolutionIndex;
        }

        Debug.Log("Video settings reset.");
    }

    void ResetAudio()
    {
        Slider[] sliders = audioScreen.GetComponentsInChildren<Slider>(true);

        foreach (Slider slider in sliders)
        {
            if (slider.name.ToLower().Contains("volume"))
            {
                slider.value = defaultMasterVolume;
            }
        }

        Debug.Log("Audio settings reset.");
    }

    void ResetKeybinds()
    {
        Debug.Log("Keybind reset not implemented yet.");
    }
}