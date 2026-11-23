using UnityEngine;

public class VaultAudio : MonoBehaviour
{
    [SerializeField] private AudioClip vaultSFX;
    [SerializeField] private AudioSource audioSource;

    public void OpeningVault()
    {
        audioSource.PlayOneShot(vaultSFX);
    }

}


