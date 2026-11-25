using System.Threading.Tasks;
using UnityEngine;

public class LocalTTSEngine : MonoBehaviour, TTSEngine
{
    [SerializeField] private string modelPath;

    public Task<AudioClip> SynthesizeAsync(string text, string voiceId)
    {
        return Task.FromResult<AudioClip>(null);   // placeholder
    }
}
