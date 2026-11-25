using System.Threading.Tasks;
using UnityEngine;

public interface TTSEngine
{
    Task<AudioClip> SynthesizeAsync(string text, string voiceId);
}
