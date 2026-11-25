using System;
using UnityEngine;

[Serializable]
public class VoiceCue
{
    public string Text;
    public string VoiceId;
    public int Priority;      // higher number wins
    public AudioClip Clip;    // optional pre-rendered audio
}