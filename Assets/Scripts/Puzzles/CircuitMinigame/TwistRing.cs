using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// A ring you rotate by grabbing it with a VR controller (grip/trigger)
/// and twisting your wrist. The ring stays in place and follows your hand's rotation.
/// When you release, it snaps to the nearest quarter-turn (90 degrees) and fires OnRingRotated.
/// </summary>
public class TwistRing : XRSimpleInteractable
{
    [Header("Hand Rotation")]
    [Tooltip("1 = the ring rotates 1:1 with your wrist. Higher = less wrist rotation needed.")]
    [SerializeField] private float twistSensitivity = 1f;
    [Tooltip("Enable if the ring rotates in the opposite direction of your hand.")]
    [SerializeField] private bool invertTwist = false;
    [Tooltip("How fast the ring follows your hand. Higher = more direct, lower = smoother. 0 = no smoothing.")]
    [SerializeField] private float followSpeed = 30f;
    [Tooltip("How long snapping to a quarter-turn takes (seconds).")]
    [SerializeField] private float snapDuration = 0.1f;

    [Header("Colour (Power On/Off)")]
    [SerializeField] private Renderer[] tintRenderers;
    [SerializeField] private Color idleColor = new Color(0.2f, 0.85f, 0.8f);
    [SerializeField] private Color poweredColor = new Color(1f, 0.9f, 0.2f);

    [Header("Debug")]
    [Tooltip("Logs to the Console when you grab, rotate, and release the ring.")]
    [SerializeField] private bool logDebug = false;

    /// <summary>Fired/invoked after the ring is released and is on a different quarter-turn (90-degree step)..</summary>
    public event Action OnRingRotated;

    private Transform handAttach;
    private Quaternion startHandRotation;
    private float lastRawTwist;
    private float accumulatedTwist;
    private float startRingAngle;
    private float currentAngle;
    private bool isTwisting;
    private Coroutine snapRoutine;
    private MaterialPropertyBlock propertyBlock;

    protected override void Awake()
    {
        base.Awake();

        if (tintRenderers == null || tintRenderers.Length == 0)
        {
            tintRenderers = GetComponentsInChildren<Renderer>();
        }
        propertyBlock = new MaterialPropertyBlock();
        SetPowered(false);
    }

    // ---------- API for generator/ manager ----------

    public void SetSnapPoint(Vector3 localPosition)
    {
        transform.localPosition = localPosition;
    }

    public void SetPowered(bool powered)
    {
        if (tintRenderers == null) return;
        if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();

        Color c = powered ? poweredColor : idleColor;
        foreach (Renderer r in tintRenderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_BaseColor", c); // URP
            propertyBlock.SetColor("_Color", c);     // Built-in
            r.SetPropertyBlock(propertyBlock);
        }
    }

    // ---------- grabbing and turning  ----------

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);

        handAttach = args.interactorObject.GetAttachTransform(this);
        if (handAttach == null) return;

        if (snapRoutine != null)
        {
            StopCoroutine(snapRoutine);
            snapRoutine = null;
        }

        startHandRotation = handAttach.rotation;
        lastRawTwist = 0f;
        accumulatedTwist = 0f;
        startRingAngle = Mathf.Round(transform.localEulerAngles.z / 90f) * 90f;
        currentAngle = startRingAngle;
        isTwisting = true;

        if (logDebug) Debug.Log($"[TwistRing] {name} grabbed (start {startRingAngle:0}°)", this);
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);

        if (!isTwisting) return;
        isTwisting = false;
        handAttach = null;

        float snapped = Mathf.Round(currentAngle / 90f) * 90f;
        bool changed = Mathf.Abs(Mathf.DeltaAngle(snapped, startRingAngle)) > 1f;

        if (logDebug) Debug.Log($"[TwistRing] {name} released at {currentAngle:0}° -> {snapped:0}° (changed: {changed})", this);

        //If the object is just being disabled or destroyed (e.g., during regeneration), do not animate it anymore.
        if (!isActiveAndEnabled)
        {
            transform.localRotation = Quaternion.Euler(0f, 0f, snapped);
            return;
        }

        if (snapRoutine != null) StopCoroutine(snapRoutine);
        snapRoutine = StartCoroutine(SnapTo(snapped, changed));
    }

    private void Update()
    {
        if (!isTwisting || handAttach == null) return;

        // How much your hand has rotated around the axis perpendicular to the grid.
        Quaternion handDelta = handAttach.rotation * Quaternion.Inverse(startHandRotation);
        float raw = TwistAngleAbout(handDelta, PlaneNormal());

        // Accumulate in small increments/steps so you can also rotate past 180 degrees.
        accumulatedTwist += Mathf.DeltaAngle(lastRawTwist, raw);
        lastRawTwist = raw;

        float delta = accumulatedTwist * twistSensitivity;
        if (invertTwist) delta = -delta;

        float target = startRingAngle + delta;
        float k = followSpeed <= 0f ? 1f : 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
        currentAngle = Mathf.Lerp(currentAngle, target, k);

        transform.localRotation = Quaternion.Euler(0f, 0f, currentAngle);
    }

    private Vector3 PlaneNormal()
    {
        return (transform.parent != null ? transform.parent.forward : Vector3.forward).normalized;
    }

    // The component of a rotation around axis (swing-twist), in degrees (-180..180).
    private static float TwistAngleAbout(Quaternion q, Vector3 axis)
    {
        if (q.w < 0f) q = new Quaternion(-q.x, -q.y, -q.z, -q.w);

        float projected = Vector3.Dot(new Vector3(q.x, q.y, q.z), axis);
        float angle = 2f * Mathf.Atan2(projected, q.w) * Mathf.Rad2Deg;
        return Mathf.DeltaAngle(0f, angle);
    }

    private IEnumerator SnapTo(float targetAngle, bool fireEvent)
    {
        float from = currentAngle;
        float t = 0f;
        float duration = Mathf.Max(0.01f, snapDuration);

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float a = Mathf.Lerp(from, targetAngle, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)));
            transform.localRotation = Quaternion.Euler(0f, 0f, a);
            yield return null;
        }

        transform.localRotation = Quaternion.Euler(0f, 0f, targetAngle);
        currentAngle = targetAngle;
        snapRoutine = null;

        if (fireEvent) OnRingRotated?.Invoke();
    }
}