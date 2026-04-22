using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public class VolumeController : MonoBehaviour
{
    public AudioMixer mixer;

    [Header("Exposed Parameter Names")]
    public string masterVolumeParameter = "MasterVolume";
    public string musicVolumeParameter = "BGMusicVolume";
    public string sfxVolumeParameter = "SFXVolume";
    public string voicesVolumeParameter = "NarrationVolume";

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

    private const string MASTER_KEY = "MasterVolumeKey";
    private const string MUSIC_KEY = "MusicVolumeKey";
    private const string SFX_KEY = "SFXVolumeKey";
    private const string VOICES_KEY = "VoicesVolumeKey";

    void Awake()
    {
        BindSliders();
        LoadSavedVolumes();
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

    void LoadSavedVolumes()
    {
        float master = PlayerPrefs.GetFloat(MASTER_KEY, 0.75f);
        float music = PlayerPrefs.GetFloat(MUSIC_KEY, 0.75f);
        float sfx = PlayerPrefs.GetFloat(SFX_KEY, 0.75f);
        float voices = PlayerPrefs.GetFloat(VOICES_KEY, 0.75f);

        if (masterSlider != null) masterSlider.SetValueWithoutNotify(master);
        if (musicSlider != null) musicSlider.SetValueWithoutNotify(music);
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(sfx);
        if (voicesSlider != null) voicesSlider.SetValueWithoutNotify(voices);
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
        if (masterText != null) masterText.text = Mathf.RoundToInt(value * 100f) + "%";
        PlayerPrefs.SetFloat(MASTER_KEY, value);
        PlayerPrefs.Save();
    }

    public void SetMusicVolume(float value)
    {
        SetVolume(musicVolumeParameter, value);
        if (musicText != null) musicText.text = Mathf.RoundToInt(value * 100f) + "%";
        PlayerPrefs.SetFloat(MUSIC_KEY, value);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float value)
    {
        SetVolume(sfxVolumeParameter, value);
        if (sfxText != null) sfxText.text = Mathf.RoundToInt(value * 100f) + "%";
        PlayerPrefs.SetFloat(SFX_KEY, value);
        PlayerPrefs.Save();
    }

    public void SetVoicesVolume(float value)
    {
        SetVolume(voicesVolumeParameter, value);
        if (voicesText != null) voicesText.text = Mathf.RoundToInt(value * 100f) + "%";
        PlayerPrefs.SetFloat(VOICES_KEY, value);
        PlayerPrefs.Save();
    }

    //    // dB scale, from -80 to 0.
    //void SetVolume(string exposedParam, float sliderValue)
    //{
    //    if (mixer == null)
    //    {
    //        Debug.LogWarning("No AudioMixer assigned.");
    //        return;
    //    }

    //    sliderValue = Mathf.Clamp(sliderValue, 0.0001f, 1f);
    //    float dB = Mathf.Log10(sliderValue) * 20f;

    //    bool success = mixer.SetFloat(exposedParam, dB);
    //    if (!success)
    //        Debug.LogWarning("Could not set mixer parameter: " + exposedParam);
    //}

    // directly set dB from slider
    void SetVolume(string exposedParam, float value)
    {
        if (mixer == null)
        {
            Debug.LogWarning("No AudioMixer assigned.");
            return;
        }

        mixer.SetFloat(exposedParam, value);
    }
}