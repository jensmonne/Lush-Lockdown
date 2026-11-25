using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NarrationManager : MonoBehaviour
{
    [SerializeField] private ClueTable clueTable;
    [SerializeField] private MonoBehaviour ttsBehaviour;   // drag CloudTTSEngine or LocalTTSEngine here
    private TTSEngine tts;
    [SerializeField] private SpatialAudioSource audioSource;
    private enum State { Idle, Synthesizing, Playing }
    private State state = State.Idle;
    private VoiceCue current;
    private int token;
    [SerializeField] private int maxQueueSize = 5;
    private readonly List<VoiceCue> queue = new List<VoiceCue>();   // needs using System.Collections.Generic;
    [SerializeField] private GameEventBus bus;
    private void Awake()
    {
        tts = ttsBehaviour as TTSEngine;
        if (tts == null)
            Debug.LogError("ttsBehaviour does not implement TTSEngine", this);
    }
    private void Update()
    {
        if (state == State.Playing && !audioSource.IsPlaying)
        {
            current = null;
            state = State.Idle;
        }

        if (state == State.Idle && queue.Count > 0)
            StartNext();
    }

    public void OnEvent(string evt) 
    {
        VoiceCue cue = clueTable.Lookup(evt);
        if (cue == null) return;      // most events have no narration
        Play(cue);
    }
    public void Play(VoiceCue cue)
    {
        if (cue == null) return;
        if (cue == current || queue.Contains(cue)) return;     // no duplicates

        // a more important line cuts in
        if (state != State.Idle && cue.Priority > current.Priority)
            StopCurrent();

        // insert before the first line with a lower priority, so equal priorities keep their order
        int index = queue.Count;
        for (int i = 0; i < queue.Count; i++)
        {
            if (queue[i].Priority < cue.Priority) { index = i; break; }
        }
        queue.Insert(index, cue);

        // too many waiting: drop the least important one (last in the list)
        if (queue.Count > maxQueueSize)
            queue.RemoveAt(queue.Count - 1);

    }
    public void StopCurrent()
    {
        token++;                 // a TTS result that arrives late will now be ignored
        audioSource.Stop();
        current = null;
        state = State.Idle;
    }
    private async void StartNext()
    {
        current = queue[0];
        queue.RemoveAt(0);
        state = State.Synthesizing;
        int myToken = ++token;

        AudioClip clip = current.Clip;                    // pre-rendered? then no TTS call
        if (clip == null && tts != null)
        {
            try
            {
                clip = await tts.SynthesizeAsync(current.Text, current.VoiceId);
                if (clip != null) current.Clip = clip;    // cache it for next time
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("TTS failed: " + e.Message, this);
            }
        }

        if (myToken != token) return;                     // stopped while we were waiting

        if (clip == null)                                 // TTS failed, so skip this line
        {
            current = null;
            state = State.Idle;
            return;
        }

        audioSource.PlayAt(clip);
        state = State.Playing;
    }
    private void OnEnable() => bus.Subscribe(OnEvent);
    private void OnDisable() => bus.Unsubscribe(OnEvent);

}
