using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Turns the Quick Outline on while any controller hovers over this interactable.
/// Stays on until the last controller stops hovering, so two hands don't make it flicker.
/// </summary>
[RequireComponent(typeof(XRBaseInteractable), typeof(Outline))]
public class HoverOutline : MonoBehaviour
{
    [Tooltip("Hide the outline while the object is being held.")]
    [SerializeField] private bool hideWhileSelected = true;

    private XRBaseInteractable interactable;
    private Outline outline;

    private void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
        outline = GetComponent<Outline>();
        outline.enabled = false;
    }

    private void Update()
    {
        // isHovered is true while at least one interactor is hovering, no matter how many enter or leave.
        bool show = interactable.isHovered && !(hideWhileSelected && interactable.isSelected);
        if (outline.enabled != show) outline.enabled = show;
    }
}
