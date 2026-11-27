using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Orchestrates the puzzle: starts generation, checks after each rotation whether the puzzle is solved,
/// and handles success, damage, and feedback. The grid itself is built by CircuitGridGenerator.
/// </summary>
public class CircuitManager : MiniGame
{
    [SerializeField] private string puzzleId;
    [SerializeField] private bool IsCompleted;
    [SerializeField] private List<TwistRing> allRings = new List<TwistRing>();
    [SerializeField] private int currentAttempts;

    [Header("Grid")]
    [Tooltip("The script that builds the grid. Automatically found on this object if left empty.")]
    [SerializeField] private CircuitGridGenerator gridGenerator;

    [Header("Visuals & Feedback")]
    [SerializeField] private CircuitVisualController visualController;
    [SerializeField] private PlayerShockHandler shockHandler;
    [Header("Plant Hazard")]
    [SerializeField] private int plantDamage = 25;

    [Header("Puzzle Sequence")]
    [Tooltip("Optional. Links this minigame to the PuzzleSequenceManager. Found on this GameObject if left empty.")]
    [SerializeField] private Puzzle puzzle;

    private CircuitGridData grid;

    private bool AcceptsInput => !IsCompleted && (puzzle == null || (puzzle.IsUnlocked && !puzzle.IsSolved));

    private void Reset()
    {
        gridGenerator = GetComponent<CircuitGridGenerator>();
    }

    private void Awake()
    {
        if (puzzle == null) puzzle = GetComponent<Puzzle>();
        if (puzzle != null)
        {
            puzzle.onUnlocked.AddListener(UpdateRingsInteractable);
            puzzle.Solved += OnPuzzleSolved;
        }
    }

    private void OnDestroy()
    {
        if (puzzle != null)
        {
            puzzle.onUnlocked.RemoveListener(UpdateRingsInteractable);
            puzzle.Solved -= OnPuzzleSolved;
        }
    }

    private void Start()
    {
        Initializepuzzel();
        GenerateGrid();
    }

    private void UpdateRingsInteractable()
    {
        bool interactable = AcceptsInput;
        foreach (TwistRing ring in allRings)
        {
            if (ring != null) ring.enabled = interactable;
        }
    }

    private void OnPuzzleSolved(Puzzle solvedPuzzle)
    {
        if (!IsCompleted) triggerSuccessState();
        UpdateRingsInteractable();
    }

    public void Initializepuzzel()
    {
       
        IsCompleted = false;
        currentAttempts = 0;
    }

    [ContextMenu("Generate Grid Now")]
    public void GenerateGrid()
    {
        if (gridGenerator == null) gridGenerator = GetComponent<CircuitGridGenerator>();
        if (gridGenerator == null)
        {
            Debug.LogError("ERROR: No CircuitGridGenerator assigned. Add the component to this object or drag it into 'Grid Generator'.");
            return;
        }

        IsCompleted = false;
        allRings.Clear();

        grid = gridGenerator.Generate();
        if (grid == null) return;

        allRings.AddRange(grid.allRings);
        foreach (TwistRing ring in grid.allRings)
        {
            if (ring == null) continue;
            ring.OnRingRotated -= HandleRingRotated;
            ring.OnRingRotated += HandleRingRotated;
        }

        CheckCircuit(true);
        UpdateRingsInteractable();
    }

    private void HandleRingRotated()
    {
        if (IsCompleted) return;

        currentAttempts++;

        if (CheckCircuit())
        {
            triggerSuccessState();
        }
    }

    public bool CheckCircuit(bool silent = false)
    {
        if (grid == null) return false;

        HashSet<TwistRing> poweredRings = new HashSet<TwistRing>();
        bool reachedEnd = grid.Trace(poweredRings, silent ? (System.Action)null : OnPlantTouched);
        grid.ApplyPower(poweredRings);
        return reachedEnd;
    }

    private void OnPlantTouched()
    {
        if (shockHandler != null)
        {
            shockHandler.TakeDamageAndShock(plantDamage);
        }
    }

    public void triggerSuccessState()
    {
        IsCompleted = true;
        if (visualController != null)
        {
            visualController.SetPowerActive(true);
            visualController.PlaySparkeffect();
        }

        if (puzzle != null) puzzle.Complete();
    }

    public void triggerPenalty()
    {
        Debug.Log("Somethings wrong");
        if (shockHandler != null)
        {
            shockHandler.ApplySchockFeedback();
        }
        if (visualController != null)
        {
            visualController.PlayElectricShockEffect();
        }
    }
}