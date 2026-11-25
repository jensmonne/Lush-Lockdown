using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Narration/Clue Table")]
public class ClueTable : ScriptableObject
{
    [Serializable] public class EventCue { public string eventId; public VoiceCue cue; }
    [Serializable] public class HintSet { public string puzzleId; public VoiceCue[] hints; }

    [SerializeField] private List<EventCue> eventCues = new List<EventCue>();
    [SerializeField] private List<HintSet> hintSets = new List<HintSet>();

    public VoiceCue Lookup(string eventId)
    {
        foreach (var e in eventCues)
            if (e.eventId == eventId) return e.cue;
        return null;
    }

    public VoiceCue NextHint(string puzzleId, int level)
    {
        foreach (var set in hintSets)
        {
            if (set.puzzleId != puzzleId || set.hints == null || set.hints.Length == 0) continue;
            return set.hints[Mathf.Min(level, set.hints.Length - 1)];
        }
        return null;
    }
}