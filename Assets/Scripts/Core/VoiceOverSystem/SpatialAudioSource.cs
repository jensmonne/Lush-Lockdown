using UnityEngine;

public class SpatialAudioSource : MonoBehaviour
{
    [SerializeField] private AudioSource source;

    public bool IsPlaying => source != null && source.isPlaying;

    public void PlayAt(AudioClip clip)
    {
        source.spatialBlend = 1f;     // 1 = fully 3D, so the voice comes from this object
        source.clip = clip;
        source.Play();
    }

    public void Stop() => source.Stop();
}