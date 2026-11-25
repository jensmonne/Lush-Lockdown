using UnityEngine;

public class PlayerSounds : MonoBehaviour
{
    [SerializeField] private AudioClip deathSFX;
    [SerializeField] private AudioClip hurtSFX;
    [SerializeField] private AudioClip chokingSFX;
    public void DeathSFX()
    {
        if (deathSFX == null) return;

        AudioSource tempSource = gameObject.AddComponent<AudioSource>();
        tempSource.clip = deathSFX;
        tempSource.Play();
        Destroy(tempSource, deathSFX.length);
    }

    public void HurtSFX()
    {
        if (hurtSFX == null) return;

        AudioSource tempSource = gameObject.AddComponent<AudioSource>();
        tempSource.clip = hurtSFX;
        tempSource.Play();
        Destroy(tempSource, hurtSFX.length);
    }

    public void ChokingSFX()
    {
        if (chokingSFX == null) return;

        AudioSource tempSource = gameObject.AddComponent<AudioSource>();
        tempSource.clip = chokingSFX;
        tempSource.Play();
        Destroy(tempSource, chokingSFX.length);
    }

}