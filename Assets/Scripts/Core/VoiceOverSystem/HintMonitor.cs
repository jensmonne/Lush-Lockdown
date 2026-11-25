using System;
using UnityEngine;

public class HintMonitor : MonoBehaviour
{
    [SerializeField] private int maxAttempts = 3;
    [SerializeField] private float maxSeconds = 60f;
    [SerializeField] private float cooldown = 30f;
    public int hintlevel;

    public event Action<string, int> OnStuck;     // (puzzleId, level)

    private string puzzleId;
    private int failedAttempts;
    private float timer;
    private float cooldownLeft;
    private bool watching;

    public void Watch(string id)
    {
        puzzleId = id;
        watching = true;
        hintlevel = 0;
        ResetCounters();
        cooldownLeft = 0f;
    }

    public void StopWatching() => watching = false;

    public void ReportFailedAttempt() => failedAttempts++;

    private void Update() => CheckProgress();

    public void CheckProgress()
    {
        if (!watching) return;

        timer += Time.deltaTime;
        if (cooldownLeft > 0f) cooldownLeft -= Time.deltaTime;

        if (cooldownLeft <= 0f && IsStuck())
            GiveHint();
    }

    public void RequestHint()
    {
        if (!watching || cooldownLeft > 0f) return;   // no spamming
        GiveHint();
    }

    private bool IsStuck() => failedAttempts >= maxAttempts || timer >= maxSeconds;

    private void GiveHint()
    {
        OnStuck?.Invoke(puzzleId, hintlevel);
        hintlevel++;
        ResetCounters();
        cooldownLeft = cooldown;
    }

    private void ResetCounters()
    {
        failedAttempts = 0;
        timer = 0f;
    }
}