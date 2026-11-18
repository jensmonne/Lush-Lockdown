using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Remembers where this object started and can put it back there.
/// Add to any interactable that should respawn when it leaves the play area.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Respawnable : MonoBehaviour
{
    private Rigidbody rb;
    private XRGrabInteractable grab;
    private Vector3 startPosition;
    private Quaternion startRotation;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grab = GetComponent<XRGrabInteractable>();
        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    public void Respawn()
    {
        if (grab != null && grab.isSelected && grab.interactionManager != null)
            grab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)grab);

        if (!rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        transform.SetPositionAndRotation(startPosition, startRotation);
        rb.position = startPosition;
        rb.rotation = startRotation;
        Physics.SyncTransforms();
    }
}
