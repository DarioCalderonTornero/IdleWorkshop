using UnityEngine;

[DefaultExecutionOrder(BootOrder.Manager)]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    //AudioSource
    [Header("AudioSources")]
    [SerializeField] private AudioSource musicAudioSource;
    [SerializeField] private AudioSource SFXAudioSource;

    //Music Clips
    [Header("Music Clips")]
    [SerializeField] private AudioClip music1Clip;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        PlayMusic(music1Clip, 1f);
    }

    public void PlayMusic(AudioClip clip, float volume)
    {
        if (clip == null) return;

        musicAudioSource.clip = clip;
        musicAudioSource.volume = volume;
        musicAudioSource.loop = true;

        musicAudioSource.Play();
    }

    public void StopMusic()
    {
        musicAudioSource.Stop();
    }

    public void PlaySFX(AudioClip clip, float volume)
    {
        if (clip == null) return;

        musicAudioSource.PlayOneShot(clip, volume);
    }
}
