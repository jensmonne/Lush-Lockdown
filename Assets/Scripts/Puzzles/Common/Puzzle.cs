using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Marks a GameObject as one puzzle in the room. Add it next to the puzzle's own script and call
/// Complete() when the puzzle is solved (e.g. from CodeButtonManager.OnCodeCorrect).
/// The PuzzleSequenceManager decides when it is unlocked.
/// </summary>
public class Puzzle : MonoBehaviour
{
    [SerializeField] private string puzzleName;

    [Tooltip("Called when the sequence reaches this puzzle. Use it to enable the puzzle's objects or interaction.")]
    public UnityEvent onUnlocked;

    [Tooltip("Called when this puzzle is solved. Put rewards here: open a door, play a sound, turn on a light.")]
    public UnityEvent onSolved;

    public string PuzzleName => string.IsNullOrEmpty(puzzleName) ? name : puzzleName;
    public bool IsUnlocked { get; private set; }
    public bool IsSolved { get; private set; }

    public event Action<Puzzle> Solved;

    public void Unlock()
    {
        if (IsUnlocked) return;

        IsUnlocked = true;
        onUnlocked.Invoke();
    }

    public void Complete()
    {
        if (IsSolved) return;

        if (!IsUnlocked)
        {
            Debug.LogWarning($"Puzzle '{PuzzleName}' was solved while still locked. Ignoring it.", this);
            return;
        }

        IsSolved = true;
        Debug.Log($"Puzzle '{PuzzleName}' solved.", this);
        onSolved.Invoke();
        Solved?.Invoke(this);
    }
}
