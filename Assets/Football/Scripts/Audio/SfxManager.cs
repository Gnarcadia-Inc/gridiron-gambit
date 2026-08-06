
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SfxManager : MonoBehaviour
{
    public static SfxManager Instance { get; private set; }

    private bool sfxOn = true;

    public AudioSource audioSource;
    public AudioSource backupAudioSource;
    public AudioSource musicAudioSource;
    public AudioSource crowdAudioSource;

    public AudioClip themeMusic;

    public AudioClip whistleSound;
    public AudioClip throwSound;
    public AudioClip catchSound;
    public AudioClip gruntSound;

    public AudioClip huddleBreakSound;

    public AudioClip snapSound;

    public AudioClip greenSound;
    public AudioClip blueSound;
    public AudioClip hardCountSound;
    public AudioClip downReadySound;

    public AudioClip crowdAmbientSound;
    public AudioClip crowdCheeringSound;

    public AudioClip buttonSound;
    public AudioClip swooshSound;
    public AudioClip betSound;
    public AudioClip chipsSound;

    private Coroutine preSnapCoroutine;

    public float volume = 0.5f;
    public float clipVolume = 0.5f;

    private readonly List<AudioSource> worldSources = new();
    private int pooled3DSources = 6;
    private float minDistance = 200f;
    private float maxDistance = 400f;

    private bool engineStartupPlaying = false;
    private bool engineWasRunning = false;

    public Transform cam;

    private bool preSnapBypass = false;

    public Image volumeButtonImage;
    public Sprite volumeOffSprite;
    public Sprite volumeOnSprite;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        DontDestroyOnLoad(gameObject);

        for (int i = 0; i < pooled3DSources; i++)
        {
            worldSources.Add(CreateWorldSource("WorldAudio_" + i));
        }
    }

    public void VolumeButton()
    {
        PlayButtonSound();

        if (sfxOn)
        {
            volumeButtonImage.sprite = volumeOffSprite;

            StopThemeMusic();

            StopCrowdNoise();
        }
        else
        {
            volumeButtonImage.sprite = volumeOnSprite;

            PlayThemeMusic();

            PlayCrowdNoise();
        }

        sfxOn = !sfxOn;
    }

    public void PlayButtonSound()
    {
        audioSource.pitch = 1f;
        audioSource.volume = 0.25f;
        audioSource.clip = buttonSound;
        audioSource.time = 0.1f;
        audioSource.Play();
    }

    public void PlayInfoButtonSound()
    {
        if (!sfxOn) return;

        audioSource.pitch = 1f;
        audioSource.volume = 0.25f;
        audioSource.clip = buttonSound;
        audioSource.time = 0.1f;
        audioSource.Play();
    }

    public void PlayThemeMusic()
    {
        musicAudioSource.UnPause();
    }

    public void PlayCrowdNoise()
    {
        crowdAudioSource.UnPause();
    }

    public void StopThemeMusic()
    {
        musicAudioSource.Pause();
    }

    public void StopCrowdNoise()
    {
        crowdAudioSource.Pause();
    }

    public void PlayWhistleSound()
    {
        if (!sfxOn) return;

        audioSource.pitch = 1f;
        audioSource.volume = 0.1f;
        //audioSource.time = desiredFloatSeconds;
        audioSource.PlayOneShot(whistleSound);
    }

    public void PlayThrowSound()
    {
        if (!sfxOn) return;

        audioSource.pitch = 1f;
        audioSource.volume = 0.25f;
        //audioSource.time = desiredFloatSeconds;
        audioSource.PlayOneShot(throwSound);

        backupAudioSource.pitch = 1f;
        backupAudioSource.volume = 0.35f;
        //audioSource.time = desiredFloatSeconds;
        backupAudioSource.PlayOneShot(gruntSound);
    }

    public void PlayCatchSound()
    {
        if (!sfxOn) return;

        PlayClipAtPosition(cam.position, SFXType.Catch);
    }

    public void PlayHuddleBreakSound()
    {
        if (!sfxOn) return;

        PlayClipAtPosition(cam.position, SFXType.HuddleBreak);
    }

    public void PlaySnapSound()
    {
        if (!sfxOn) return;

        PlayClipAtPosition(cam.position, SFXType.Snap);
    }

    //UI
    public void PlayWhooshSound()
    {
        if (!sfxOn) return;

        audioSource.pitch = 1f;
        audioSource.volume = 0.5f;
        //audioSource.time = desiredFloatSeconds;
        audioSource.PlayOneShot(throwSound);
    }

    public void PlayBetIncreaseSound()
    {
        if (!sfxOn) return;

        audioSource.pitch = 0.85f;
        audioSource.volume = 0.4f;
        audioSource.clip = betSound;
        audioSource.time = 0.2f;
        audioSource.Play();
    }

    public void PlayBetDecreaseSound()
    {
        if (!sfxOn) return;

        audioSource.pitch = 0.82f;
        audioSource.volume = 0.4f;
        audioSource.clip = betSound;
        audioSource.time = 0.2f;
        audioSource.Play();
    }

    public void PlayQuickBetSound(float pitch)
    {
        if (!sfxOn) return;

        audioSource.pitch = pitch;
        audioSource.volume = 0.4f;
        audioSource.clip = betSound;
        audioSource.time = 0.2f;
        audioSource.Play();
    }

    public void PlayPlaceBetSound()
    {
        if (!sfxOn) return;

        audioSource.pitch = 0.9f;
        audioSource.volume = 0.1f;
        audioSource.PlayOneShot(chipsSound);
    }

    public void PlayPreSnapSound()
    {
        if (!sfxOn) return;

        int rand = Random.Range(0, 4);

        switch (rand)
        {
            case 0:
                PlayClipAtPosition(cam.position, SFXType.Green);
                break;
            case 1:
                PlayClipAtPosition(cam.position, SFXType.Blue);
                break;
            case 2:
                PlayClipAtPosition(cam.position, SFXType.HardCount);
                break;
            case 3:
                PlayClipAtPosition(cam.position, SFXType.DownReady);
                break;
        }
    }

    public void PlayClipAtPosition(Vector3 position, SFXType type, float pitch = 1f)
    {
        if (!sfxOn) return;

        AudioClip clip = null;
        float newVolume = 1f;
        float desiredFloatSeconds = 0f;
        switch (type)
        {
            case SFXType.Catch:
                clip = catchSound;
                newVolume = 0.4f;
                desiredFloatSeconds = 0.25f;
                break;
            case SFXType.HuddleBreak:
                clip = huddleBreakSound;
                newVolume = 0.5f;
                desiredFloatSeconds = 1.25f;
                break;
            case SFXType.Snap:
                clip = snapSound;
                newVolume = 0.25f;
                desiredFloatSeconds = 1.25f;
                break;
            case SFXType.Green:
                clip = greenSound;
                newVolume = 0.2f;
                desiredFloatSeconds = 1.25f;
                break;
            case SFXType.Blue:
                clip = blueSound;
                newVolume = 0.2f;
                desiredFloatSeconds = 1.25f;
                break;
            case SFXType.HardCount:
                clip = hardCountSound;
                newVolume = 0.25f;
                desiredFloatSeconds = 1.25f;
                break;
            case SFXType.DownReady:
                clip = downReadySound;
                newVolume = 0.25f;
                desiredFloatSeconds = 0f;
                break;
        }

        AudioSource source = backupAudioSource;

        source.transform.position = position;
        source.pitch = pitch;
        source.volume = newVolume;
        source.clip = clip;
        source.time = desiredFloatSeconds;
        source.Play();
    }

    public void StartPreSnapLoop()
    {
        if (!sfxOn) return;

        StopPreSnapLoop();
        preSnapBypass = false;
        preSnapCoroutine = StartCoroutine(PreSnapLoopRoutine());
    }

    public void StopPreSnapLoop()
    {
        preSnapBypass = true;
        if (preSnapCoroutine != null)
        {
            StopCoroutine(preSnapCoroutine);
            preSnapCoroutine = null;
        }
    }

    private IEnumerator PreSnapLoopRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(1.5f);

            if (!preSnapBypass)
            {
                PlayPreSnapSound();
            }
        }
    }

    public void PlayClipAndParent(Transform parentTransform, SFXType type, float newVolume = 0.5f, float pitch = 1f)
    {
        if (!sfxOn) return;

        AudioClip clip = null;
        switch (type)
        {
            case SFXType.Throw:
                clip = throwSound;
                break;
        }

        AudioSource source = GetAvailableWorldSource();
        worldSources.Remove(source);

        source.transform.position = parentTransform.position;
        source.transform.SetParent(parentTransform);
        source.pitch = pitch;
        source.volume = newVolume;
        source.clip = clip;
        source.Play();
    }

    private AudioSource GetAvailableWorldSource()
    {
        // 1. Prefer a source that is not playing
        for (int i = 0; i < worldSources.Count; i++)
        {
            if (!worldSources[i].isPlaying)
                return worldSources[i];
        }

        // 2. If all are busy, steal the one with the least time remaining
        AudioSource bestSource = worldSources[0];
        float shortestRemainingTime = float.MaxValue;

        for (int i = 0; i < worldSources.Count; i++)
        {
            AudioSource src = worldSources[i];

            float remaining = 0f;
            if (src.clip != null && src.pitch > 0.01f)
            {
                remaining = (src.clip.length - src.time) / src.pitch;
            }

            if (remaining < shortestRemainingTime)
            {
                shortestRemainingTime = remaining;
                bestSource = src;
            }
        }

        bestSource.Stop();
        return bestSource;
    }

    private AudioSource CreateWorldSource(string sourceName)
    {
        GameObject go = new GameObject(sourceName);
        go.transform.SetParent(transform);

        AudioSource src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 1f;
        src.minDistance = minDistance;
        src.maxDistance = maxDistance;
        src.rolloffMode = AudioRolloffMode.Logarithmic;

        return src;
    }
}

public enum SFXType
{
    Button,
    Swoosh,
    Throw,
    Catch,
    HuddleBreak,
    Snap,
    Green,
    Blue,
    HardCount,
    DownReady
}