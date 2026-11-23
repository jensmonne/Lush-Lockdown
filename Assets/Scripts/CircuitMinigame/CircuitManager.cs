using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// De puzzel-regie: start het genereren, controleert na elke draai of de puzzel klaar is
/// en regelt succes, schade en feedback. Het grid zelf wordt gebouwd door CircuitGridGenerator.
/// </summary>
public class CircuitManager : MonoBehaviour
{
    [SerializeField] private string puzzleId;
    [SerializeField] private bool IsCompleted;
    [SerializeField] private List<RotatableRing> allRings = new List<RotatableRing>();
    [SerializeField] private int currentAttempts;

    [Header("Grid")]
    [Tooltip("Het script dat het grid bouwt. Wordt automatisch gezocht op dit object als het leeg is.")]
    [SerializeField] private CircuitGridGenerator gridGenerator;

    [Header("Visuals & Feedback")]
    [SerializeField] private CircuitVisualController visualController;
    [SerializeField] private PlayerShockHandler shockHandler;

    [Header("Planten Gevaar")]
    [SerializeField] private int plantDamage = 25;

    [Header("UI Feedback")]
    [SerializeField] private WorldScreen puzzleScreen;

    private CircuitGridData grid;

    private void Reset()
    {
        gridGenerator = GetComponent<CircuitGridGenerator>();
    }

    private void Start()
    {
        Initializepuzzel();
        GenerateGrid();
    }

    public void Initializepuzzel()
    {
        if (puzzleScreen != null)
        {
            puzzleScreen.KeepScreenActive();
        }
        IsCompleted = false;
        currentAttempts = 0;
    }

    [ContextMenu("Generate Grid Now")]
    public void GenerateGrid()
    {
        if (gridGenerator == null) gridGenerator = GetComponent<CircuitGridGenerator>();
        if (gridGenerator == null)
        {
            Debug.LogError("FOUT: Geen CircuitGridGenerator gekoppeld. Voeg het component toe aan dit object of sleep het in 'Grid Generator'.");
            return;
        }

        IsCompleted = false;
        allRings.Clear();

        grid = gridGenerator.Generate();
        if (grid == null) return;

        allRings.AddRange(grid.allRings);
        foreach (RotatableRing ring in grid.allRings)
        {
            if (ring == null) continue;
            ring.OnRingRotated -= HandleRingRotated;
            ring.OnRingRotated += HandleRingRotated;
        }

        CheckCircuit(true);
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

    // silent = true: geen schade/shock (gebruikt bij het genereren van het grid)
    public bool CheckCircuit(bool silent = false)
    {
        if (grid == null) return false;

        HashSet<RotatableRing> poweredRings = new HashSet<RotatableRing>();
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
        Debug.Log("Puzzel afgerond");
        if (visualController != null)
        {
            visualController.SetPowerActive(true);
            visualController.PlaySparkeffect();
        }
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