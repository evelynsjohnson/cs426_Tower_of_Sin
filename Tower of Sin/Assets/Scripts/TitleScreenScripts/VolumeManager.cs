using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class VolumeManager : MonoBehaviour
{
    public AudioMixer audioMixer;
    public Slider volumeSlider;

    [Range(0.0001f, 1f)]
    public float defaultVolume = 0.25f;

    void Start()
    {
        if (volumeSlider != null)
        {
            volumeSlider.value = defaultVolume;

            SetVolume(defaultVolume);

            volumeSlider.onValueChanged.AddListener(SetVolume);
        }
    }

    public void SetVolume(float value)
    {
        float clamped = Mathf.Clamp(value, 0.0001f, 1f);
        float dB = Mathf.Log10(clamped) * 20f;
        audioMixer.SetFloat("MasterVolume", dB);
    }
}