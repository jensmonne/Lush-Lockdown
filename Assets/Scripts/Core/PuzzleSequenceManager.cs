using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Runs the puzzles of the room in order. Only the current puzzle is unlocked; solving it unlocks the next one.
/// When the last puzzle is solved, onComplete is called (open the exit, stop the timer, ...).
/// </summary>
public class PuzzleSequenceManager : MonoBehaviour
{
    [Tooltip("The puzzles in the order they must be solved.")]
    [SerializeField] private List<Puzzle> sequence = new();

    [Tooltip("Called when every puzzle in the sequence is solved.")]
    public UnityEvent onComplete;

    [Tooltip("Called every time a puzzle in the sequence is solved, with the puzzle that was solved.")]
    public UnityEvent<Puzzle> onPuzzleSolved;

    private int currentIndex;

    public Puzzle CurrentPuzzle => currentIndex < sequence.Count ? sequence[currentIndex] : null;
    public int SolvedCount => currentIndex;
    public int TotalCount => sequence.Count;
    public bool IsComplete => sequence.Count > 0 && currentIndex >= sequence.Count;

    private void Start()
    {
        sequence.RemoveAll(p => p == null);

        foreach (var puzzle in sequence)
            puzzle.Solved += OnPuzzleSolved;

        if (sequence.Count == 0)
        {
            Debug.LogWarning("PuzzleSequenceManager has no puzzles in its sequence.", this);
            return;
        }

        sequence[0].Unlock();
    }

    private void OnDestroy()
    {
        foreach (var puzzle in sequence)
        {
            if (puzzle != null) puzzle.Solved -= OnPuzzleSolved;
        }
    }

    private void OnPuzzleSolved(Puzzle puzzle)
    {
        if (puzzle != CurrentPuzzle) return;

        currentIndex++;
        onPuzzleSolved.Invoke(puzzle);

        if (CurrentPuzzle != null)
        {
            CurrentPuzzle.Unlock();
            return;
        }

        Debug.Log("All puzzles solved!", this);
        onComplete.Invoke();
    }

    [ContextMenu("Skip Current Puzzle")]
    public void SkipCurrentPuzzle()
    {
        if (!Application.isPlaying || CurrentPuzzle == null) return;

        Debug.Log($"Skipping puzzle '{CurrentPuzzle.PuzzleName}'.", this);
        CurrentPuzzle.Complete();
    }

    private void OnValidate()
    {
        var seen = new HashSet<Puzzle>();
        foreach (var puzzle in sequence)
        {
            if (puzzle != null && !seen.Add(puzzle))
                Debug.LogWarning($"Puzzle '{puzzle.PuzzleName}' is in the sequence more than once.", this);
        }
    }
}
