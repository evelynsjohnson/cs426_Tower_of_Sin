using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class TitleScreenAudio : MonoBehaviour
{
    public AudioClip titleMusic;

    void Start()
    {
        AudioSource source = GetComponent<AudioSource>();
        source.clip = titleMusic;
        source.loop = true;
        source.playOnAwake = false;
        source.Play();
    }
}