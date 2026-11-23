using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public enum PortDirection { Noord, Oost, Zuid, West }

public class RotatableRing : MonoBehaviour
{
    [SerializeField] private float CurrentAngle;
    [SerializeField] private float TargetSnapAngel;
    [SerializeField] private List<PortDirection> RingPorts = new List<PortDirection>();
    [SerializeField] private bool isBeingHeld;

    public bool IsCorrectAligned { get; set; } = false;
    public event Action OnRingRotated;

    [Header("VR Grab Rotatie")]
    [SerializeField] private float grabStartStepAngle = 90f; // Draait direct bij het pakken (zet op 0 als je alleen wilt meedraaien)
    [SerializeField] private float dragRotationSpeed = 120f;

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

    private XRGrabInteractable grabInteractable;
    private IXRSelectInteractor activeInteractor;
    private Vector3 lastInteractorEuler;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        cachedRenderers = GetComponentsInChildren<Renderer>();
        propBlock = new MaterialPropertyBlock();
        SetPowered(false);
    }

    private void OnEnable()
    {
        if (grabInteractable == null)
        {
            grabInteractable = GetComponent<XRGrabInteractable>();
        }

        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(HandleSelectEntered);
            grabInteractable.selectExited.AddListener(HandleSelectExited);
        }
    }

    private void OnDisable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(HandleSelectEntered);
            grabInteractable.selectExited.RemoveListener(HandleSelectExited);
        }
    }

    private void Update()
    {
        if (!isBeingHeld || activeInteractor == null) return;

        Vector3 currentInteractorEuler = activeInteractor.transform.eulerAngles;
        float deltaZ = Mathf.DeltaAngle(lastInteractorEuler.z, currentInteractorEuler.z);
        lastInteractorEuler = currentInteractorEuler;

        if (Mathf.Abs(deltaZ) > 0.01f)
        {
            RotateRing(deltaZ * (dragRotationSpeed / 90f));
        }
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

    private void HandleSelectEntered(SelectEnterEventArgs args)
    {
        activeInteractor = args.interactorObject;
        if (activeInteractor != null)
        {
            lastInteractorEuler = activeInteractor.transform.eulerAngles;
        }
        OnGrabStart();
    }

    private void HandleSelectExited(SelectExitEventArgs args)
    {
        activeInteractor = null;
        OnGrabEnd();
    }

    public void OnGrabStart()
    {
        isBeingHeld = true;
        Debug.Log("Ring wordt vastgepakt en begint met draaien.");

        if (Mathf.Abs(grabStartStepAngle) > 0.01f)
        {
            RotateRing(grabStartStepAngle);
        }
    }

    public void OnGrabEnd()
    {
        isBeingHeld = false;
        Debug.Log("Ring losgelaten, nu snappen naar dichtstbijzijnde 90 graden.");

        SnapAndCheck();
    }

    public void RotateRing(float deltaAngel)
    {
        CurrentAngle += deltaAngel;
        transform.localRotation = Quaternion.Euler(0f, 0f, CurrentAngle);

        if (hasSnapPoint)
        {
            transform.localPosition = snapPointLocalPosition;
        }
    }

    public void SnapAndCheck()
    {
        StopAllCoroutines();
        StartCoroutine(SnapNextFrameRoutine());
    }

    private IEnumerator SnapNextFrameRoutine()
    {
        yield return null;

        CurrentAngle = transform.localEulerAngles.z;
        TargetSnapAngel = Mathf.Round(CurrentAngle / 90f) * 90f;
        CurrentAngle = (TargetSnapAngel % 360f + 360f) % 360f;

        transform.localRotation = Quaternion.Euler(0f, 0f, CurrentAngle);

        if (hasSnapPoint)
        {
            transform.localPosition = snapPointLocalPosition;
        }

        float normalizedTarget = (targetCorrectAngle % 360f + 360f) % 360f;
        IsCorrectAligned = Mathf.Approximately(CurrentAngle, normalizedTarget);

        OnRingRotated?.Invoke();

        Debug.Log($"Ring gespind naar {CurrentAngle}° op snap point {transform.localPosition} | Correct uitgelijnd: {IsCorrectAligned}");
    }
    public void SnapToNearest90()
    {
        SnapAndCheck();
    }
    public void SetRotationStep(int step)
    {
        step = ((step % 4) + 4) % 4;
        transform.localRotation = Quaternion.Euler(0f, 0f, step * 90f);
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