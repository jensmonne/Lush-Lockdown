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
    [Header("Winnende Instelling")]
    [SerializeField] private float targetCorrectAngle = 90f; // De hoek waarop de ring goed staat
    [Header("Grid Snap Point")]
    [SerializeField] private Vector3 snapPointLocalPosition;
    [SerializeField] private float zOffset = -0.05f;
    private bool hasSnapPoint = false;
    [Header("Power Highlight")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color poweredColor = new Color(0.2f, 1f, 0.85f);
    [SerializeField] private float emissionIntensity = 2f;

    private Renderer[] cachedRenderers;
    private MaterialPropertyBlock propBlock;
    private bool isPowered;

    private void Awake()
    {
        cachedRenderers = GetComponentsInChildren<Renderer>();
        propBlock = new MaterialPropertyBlock();
        SetPowered(false);
    }

    public void SetPowered(bool powered)
    {
        isPowered = powered;
        if (cachedRenderers == null || cachedRenderers.Length == 0) return;

        Color targetColor = powered ? poweredColor : normalColor;

        foreach (Renderer rend in cachedRenderers)
        {
            if (rend == null) continue;
            rend.GetPropertyBlock(propBlock);
            propBlock.SetColor("_Color", targetColor);
            propBlock.SetColor("_BaseColor", targetColor);
            propBlock.SetColor("_EmissionColor", powered ? poweredColor * emissionIntensity : Color.black);
            rend.SetPropertyBlock(propBlock);
        }
    }

    public void SetSnapPoint(Vector3 localGridPosition)
    {
        snapPointLocalPosition = new Vector3(localGridPosition.x, localGridPosition.y, zOffset);
        hasSnapPoint = true;
        transform.localPosition = snapPointLocalPosition;
    }
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

    public void SnapAndCheck()
    {
        // 1. Lees de huidige rotatie uit (neem de Z-as van de transform)
        CurrentAngle = transform.localEulerAngles.z;

        // 2. Bereken de dichtstbijzijnde 90 graden stap
        TargetSnapAngel = Mathf.Round(CurrentAngle / 90f) * 90f;
        CurrentAngle = TargetSnapAngel;

        // 3. Pas de definitieve rotatie én het vaste snap point direct toe
        transform.localRotation = Quaternion.Euler(0f, 0f, CurrentAngle);

        if (hasSnapPoint)
        {
            transform.localPosition = snapPointLocalPosition;
        }

        // 4. Controleer of de hoek overeenkomt met de winnende hoek
        float normalizedAngle = (CurrentAngle % 360f + 360f) % 360f;
        float normalizedTarget = (targetCorrectAngle % 360f + 360f) % 360f;
        IsCorrectAligned = Mathf.Approximately(normalizedAngle, normalizedTarget);

        // 5. Geef een seintje aan de CircuitManager
        OnRingRotated?.Invoke();

        Debug.Log($"Ring gespind naar {CurrentAngle}° op snap point {transform.localPosition} | Correct uitgelijnd: {IsCorrectAligned}");
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
