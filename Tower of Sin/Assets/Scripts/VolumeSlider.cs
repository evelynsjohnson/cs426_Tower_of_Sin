using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class VolumeSlider : MonoBehaviour
{
    public AudioMixer audioMixer;
    public Slider slider;

    void Start()
    {
        float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 0.5f);
        slider.value = savedVolume;
        SetVolume(savedVolume);

        slider.onValueChanged.AddListener(SetVolume);
    }

    public void SetVolume(float value)
    {
        // Convert linear slider (0–1) to logarithmic decibels
        float dB = Mathf.Log10(value) * 20f;
        audioMixer.SetFloat("MasterVolume", dB);

        PlayerPrefs.SetFloat("MasterVolume", value);
    }
}