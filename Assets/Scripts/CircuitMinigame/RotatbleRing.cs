using System;
using System.Collections.Generic;
using UnityEngine;

public enum PortDirection { Noord,Oost, Zuid, West }
public class RotatableRing : MonoBehaviour
{
    [SerializeField] private float CurrentAngle;
    [SerializeField] private float TargetSnapAngel;
    [SerializeField] private List<PortDirection> RingPorts = new List<PortDirection>(); 
    [SerializeField] private bool isBeingHeld;
    public bool IsCorrectAligned { get; set; } = false;
    public event Action OnRingRotated;
    public void OnGrabStart() 
    {
        isBeingHeld = true;
        Debug.Log("Ring wordt vastgepakt.");
    }
    public void OnGrabEnd() 
    {
        isBeingHeld = false;
        Debug.Log("Ring losgelaten, nu snappen naar dichtstbijzijnde 90 graden.");

        SnapToNearest90();
    }
    public void RotateRing(float deltaAngel) 
    {

    }
    public void SnapToNearest90() 
    {

        TargetSnapAngel = Mathf.Round(CurrentAngle / 90f) * 90f;
        CurrentAngle = TargetSnapAngel;

        // Pas de definitieve rotatie toe op het object
        transform.localRotation = Quaternion.Euler(0f, 0f, CurrentAngle);

        // Geef een seintje aan de CircuitManager dat de ring definitief is neergezet
        OnRingRotated?.Invoke();
    }
    public List<PortDirection> GetPortDirections()
    {
        // We maken een nieuwe lijst aan voor de gedraaide poorten
        List<PortDirection> rotatedPorts = new List<PortDirection>();

        // Bereken hoeveel stappen van 90 graden de ring is gedraaid (0, 1, 2 of 3)
        int rotationSteps = Mathf.RoundToInt((CurrentAngle % 360f + 360f) % 360f / 90f);

        foreach (PortDirection originalPort in RingPorts)
        {
            // Tel de rotatiestappen op bij de oorspronkelijke poort en zorg dat hij binnen 0-3 blijft (Noord, Oost, Zuid, West)
            int newPortIndex = ((int)originalPort + rotationSteps) % 4;
            rotatedPorts.Add((PortDirection)newPortIndex);
        }

        return rotatedPorts;

    }
}
