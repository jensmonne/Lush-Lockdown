using System.Threading.Tasks;
using UnityEngine;

public class CloudTTSEngine : MonoBehaviour, TTSEngine
{
    [SerializeField] private string apiKey;

    public Task<AudioClip> SynthesizeAsync(string text, string voiceId)
    {
        return Task.FromResult<AudioClip>(null);   // placeholder until we call the real API
    }
}
