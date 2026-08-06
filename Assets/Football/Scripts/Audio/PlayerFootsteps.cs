
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerFootsteps : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioSource backupAudioSource;
    [SerializeField] private AudioClip[] footstepClips;
    [SerializeField] private AudioClip tackleSound;
    [SerializeField] private AudioClip tackledSound;
    [SerializeField] private AudioClip gruntSound;
    [SerializeField] private AudioClip gruntedSound;


    [Header("Volume Modulation")]
    [SerializeField] private float minPitch = 0.9f;
    [SerializeField] private float maxPitch = 1.1f;

    // This method name MUST match the Function name in the Animation Event exactly
    public void PlayFootstepSound()
    {
        if (footstepClips == null || footstepClips.Length == 0 || audioSource == null)
            return;

        // Pick a random clip so it doesn't sound mechanical
        int randomIndex = Random.Range(0, footstepClips.Length);
        audioSource.clip = footstepClips[randomIndex];

        // Slight pitch variation makes footsteps sound organic and natural
        audioSource.pitch = Random.Range(minPitch, maxPitch);

        audioSource.Play();
    }

    public void PlayTackleSound()
    {
        audioSource.clip = tackleSound;
        audioSource.pitch = Random.Range(minPitch, maxPitch);

        backupAudioSource.clip = (Random.Range(0, 2) == 1) ? gruntSound : gruntedSound;
        backupAudioSource.pitch = Random.Range(1f, maxPitch);

        audioSource.Play();
        backupAudioSource.Play();
        //backupAudioSource.PlayOneShot(gruntSound);
    }

    public void PlayTackledSound()
    {
        audioSource.clip = tackledSound;
        audioSource.pitch = Random.Range(minPitch, maxPitch);

        backupAudioSource.clip = (Random.Range(0, 2) == 1) ? gruntSound : gruntedSound;
        backupAudioSource.pitch = Random.Range(minPitch, 1f);

        audioSource.Play();
        backupAudioSource.Play();
        //backupAudioSource.PlayOneShot(gruntedSound);
    }
}
