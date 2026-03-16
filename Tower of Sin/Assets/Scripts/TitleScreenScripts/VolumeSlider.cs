using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class VolumeSlider : MonoBehaviour
{
    public AudioMixer audioMixer;
    public Slider slider;

    [Header("Settings")]
    public float defaultVolume = 0.25f;

    void Start()
    {
        slider.value = defaultVolume;

        SetVolume(defaultVolume);

        slider.onValueChanged.AddListener(SetVolume);
    }

    public void SetVolume(float value)
    {
        float clampedValue = Mathf.Clamp(value, 0.0001f, 1f);

        float dB = Mathf.Log10(clampedValue) * 20f;
        audioMixer.SetFloat("MasterVolume", dB);
    }
}