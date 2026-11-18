using System.Collections.Generic;
using UnityEngine;
public class CircuitManager : MonoBehaviour 
{
    [SerializeField] private string puzzleId;
    [SerializeField] private bool IsCompleted;
    [SerializeField] private List<RotatableRing> allRings = new List<RotatableRing>();
    [SerializeField] private int currentAttempts;
    [Header("Visuals & Feedback")]
    [SerializeField] private CircuitVisualController visualController;
    [SerializeField] private PlayerShockHandler shockHandler;
    private void Start()
    {
       Initializepuzzel();
    }
    public void Initializepuzzel() 
    {
        IsCompleted = false;
        if(allRings == null|| allRings.Count == 0 ) 
        {
            allRings = new List<RotatableRing>(FindObjectsOfType<RotatableRing>());
        }
        foreach (RotatableRing ring in allRings) 
        {
            ring.OnRingRotated += HandleRingRotated;
        }
    }
    private void HandleRingRotated()
    {
        if (IsCompleted) return;

        // Controleer direct of het circuit nu compleet is
        bool success = CheckCircuit();

        if (success)
        {
            triggerSuccessState();
        } 
    }

    public bool CheckCircuit()
    {
        foreach (RotatableRing ring in allRings)
        {
            if (!ring.IsCorrectAligned)
            {
                return false; // Als er één ring verkeerd staat, is het circuit niet compleet
            }
        }

        return true;
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
