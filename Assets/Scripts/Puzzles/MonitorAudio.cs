using UnityEngine;

public class MonitorAudio : MonoBehaviour
{
    [SerializeField] private AudioClip correctSFX;
    [SerializeField] private AudioClip wrongSFX;
    [SerializeField] private AudioClip pressSFX;

    [SerializeField] private AudioSource audioSource;

    public void CorrectSound()
    {
        audioSource.PlayOneShot(correctSFX);
    }

    public void WrongSound()
    {
        audioSource.PlayOneShot(wrongSFX);
    }

    public void PressSound()
    {
        audioSource.PlayOneShot(pressSFX);
    }
}
