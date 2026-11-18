using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class PlayerShockHandler : MonoBehaviour
{
    [Header("UML Attributen - VR Controllers")]
    [SerializeField] private ActionBasedController leftController;
    [SerializeField] private ActionBasedController rightController;
    [Header("UML Attributen - Schok Instellingen")]
    [SerializeField] private float shockIntensity = 0.8f;
    [SerializeField] private float shockDuration = 0.5f;
    private void TriggerHaptic(ActionBasedController controller)
    {
        if (controller != null && controller.enableInputTracking)
        {
            // Stuurt een trilling met een bepaalde amplitude (sterkte) en duur naar de controller
            controller.SendHapticImpulse(shockIntensity, shockDuration);
        }
    }
    public void ApplySchockFeedback()
    {
        Debug.Log("PlayerShockHandler: BZZT! Elektrische schok wordt toegepast op VR-controllers.");

        // Stuur haptic feedback naar beide controllers (indien gekoppeld)
        TriggerHaptic(leftController);
        TriggerHaptic(rightController);
    }

}
