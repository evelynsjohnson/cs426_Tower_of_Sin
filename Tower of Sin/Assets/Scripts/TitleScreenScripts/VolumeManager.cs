using UnityEngine;
using UnityEngine.UI;

public class VolumeManager : MonoBehaviour
{
    public Slider volumeSlider;

    [Range(0f, 1f)]
    public float defaultVolume = 0.25f;

    void Start()
    {
        if (volumeSlider != null)
        {
            volumeSlider.value = defaultVolume;
        }

        // Immediately apply
        SetVolume(defaultVolume);

        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }
    }

    public void SetVolume(float volume)
    {
        AudioListener.volume = volume;
    }
}