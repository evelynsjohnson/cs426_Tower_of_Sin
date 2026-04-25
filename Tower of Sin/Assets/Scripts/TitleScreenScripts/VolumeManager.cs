using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeController : MonoBehaviour
{
    public AudioMixer mixer;

    [Header("Exposed Parameter Names")]
    public string masterVolumeParameter = "MasterVolume";
    public string musicVolumeParameter = "BGMusicVolume";
    public string sfxVolumeParameter = "SFXVolume";
    public string voicesVolumeParameter = "NarrationVolume";

    [Header("Default Slider Value")]
    [Range(-80f, 20f)]
    public float defaultVolume = 0f;

    [Header("Sliders")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;
    public Slider voicesSlider;

    [Header("Optional Text")]
    public TMP_Text masterText;
    public TMP_Text musicText;
    public TMP_Text sfxText;
    public TMP_Text voicesText;

    void Awake()
    {
        BindSliders();
        //ResetSlidersToDefault();
        ApplyAllVolumes();

    }

    void OnEnable()
    {
        BindSliders();
        ApplyAllVolumes();
    }

    void BindSliders()
    {
        if (masterSlider != null)
        {
            masterSlider.onValueChanged.RemoveListener(SetMasterVolume);
            masterSlider.onValueChanged.AddListener(SetMasterVolume);
        }

        if (musicSlider != null)
        {
            musicSlider.onValueChanged.RemoveListener(SetMusicVolume);
            musicSlider.onValueChanged.AddListener(SetMusicVolume);
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveListener(SetSFXVolume);
            sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        }

        if (voicesSlider != null)
        {
            voicesSlider.onValueChanged.RemoveListener(SetVoicesVolume);
            voicesSlider.onValueChanged.AddListener(SetVoicesVolume);
        }
    }

    void ResetSlidersToDefault()
    {
        if (masterSlider != null) masterSlider.SetValueWithoutNotify(defaultVolume);
        if (musicSlider != null) musicSlider.SetValueWithoutNotify(defaultVolume);
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(defaultVolume);
        if (voicesSlider != null) voicesSlider.SetValueWithoutNotify(defaultVolume);
    }

    void ApplyAllVolumes()
    {
        if (masterSlider != null) SetMasterVolume(masterSlider.value);
        if (musicSlider != null) SetMusicVolume(musicSlider.value);
        if (sfxSlider != null) SetSFXVolume(sfxSlider.value);
        if (voicesSlider != null) SetVoicesVolume(voicesSlider.value);
    }

    public void SetMasterVolume(float value)
    {
        SetVolume(masterVolumeParameter, value);
        if (masterText != null) masterText.text = Mathf.RoundToInt(value) + " dB";
    }

    public void SetMusicVolume(float value)
    {
        SetVolume(musicVolumeParameter, value);
        if (musicText != null) musicText.text = Mathf.RoundToInt(value) + " dB";
    }

    public void SetSFXVolume(float value)
    {
        SetVolume(sfxVolumeParameter, value);
        if (sfxText != null) sfxText.text = Mathf.RoundToInt(value) + " dB";
    }

    public void SetVoicesVolume(float value)
    {
        SetVolume(voicesVolumeParameter, value);
        if (voicesText != null) voicesText.text = Mathf.RoundToInt(value) + " dB";
    }

    void SetVolume(string exposedParam, float value)
    {
        if (mixer == null)
        {
            Debug.LogWarning("No AudioMixer assigned.");
            return;
        }

        bool success = mixer.SetFloat(exposedParam, value);

        if (!success)
        {
            Debug.LogWarning("Could not set mixer parameter: " + exposedParam);
        }
    }
}