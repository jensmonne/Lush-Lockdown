using System;
using System.Collections.Generic;
using UnityEngine;

public class GameEventBus : MonoBehaviour
{
    private readonly List<Action<string>> handlers = new List<Action<string>>();
    private readonly Queue<string> queue = new Queue<string>();

    public void Subscribe(Action<string> handler)
    {
        if (handler == null || handlers.Contains(handler)) return;   // no nulls, no double listeners
        handlers.Add(handler);
    }

    public void Unsubscribe(Action<string> handler)
    {
        handlers.Remove(handler);
    }

    public void Publish(string eventId)
    {
        if (string.IsNullOrEmpty(eventId)) return;
        queue.Enqueue(eventId);                                      // delivered later, in Update
    }

    private void Update()
    {
        int count = queue.Count;                                     // only what was waiting at the start of the frame
        for (int i = 0; i < count; i++)
        {
            string eventId = queue.Dequeue();

            Action<string>[] snapshot = handlers.ToArray();          // copy, so a handler can unsubscribe while we deliver
            foreach (Action<string> handler in snapshot)
            {
                try { handler(eventId); }
                catch (Exception e) { Debug.LogException(e, this); } // one broken listener must not block the others
            }
        }
    }
}