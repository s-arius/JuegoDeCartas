using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    public AudioClip flipClip;
    public AudioClip failClip;
    public AudioClip pointClip;
    public AudioClip backgroundClip;

    private AudioSource sfxSource;
    private AudioSource musicSource;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        sfxSource = gameObject.AddComponent<AudioSource>();
        musicSource = gameObject.AddComponent<AudioSource>();

        musicSource.loop = true;
    }

    void Start()
    {
        if (backgroundClip != null)
        {
            musicSource.clip = backgroundClip;
            musicSource.Play();
        }
    }

    public void PlayFlip() => sfxSource.PlayOneShot(flipClip);
    public void PlayFail() => sfxSource.PlayOneShot(failClip);
    public void PlayPoint() => sfxSource.PlayOneShot(pointClip);
}
