using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

public class PlayerShockHandler : MonoBehaviour
{
    [Header("UML Attributes - VR Controllers")]
    [Tooltip("Haptic Impulse Player on XR Origin > Camera Offset > Left Controller")]
    [SerializeField] private HapticImpulsePlayer leftController;
    [Tooltip("Haptic Impulse Player on XR Origin > Camera Offset > Right Controller")]
    [SerializeField] private HapticImpulsePlayer rightController;

    [Header("UML Attributes - Shock & Damage")]
    [SerializeField] private float shockIntensity = 0.8f;
    [SerializeField] private float shockDuration = 0.5f;
    [SerializeField] private int playerHealth = 100;
    [SerializeField] private float shockCooldown = 1.0f;

    private float lastShockTime = -999f;

    private void TriggerHaptic(HapticImpulsePlayer controller)
    {
        if (controller != null)
        {
            controller.SendHapticImpulse(shockIntensity, shockDuration);
        }
    }

    public void ApplySchockFeedback()
    {
      

        TriggerHaptic(leftController);
        TriggerHaptic(rightController);
    }

    public void TakeDamageAndShock(int damage)
    {
        if (Time.time - lastShockTime < shockCooldown) return;
        lastShockTime = Time.time;

        playerHealth = Mathf.Max(0, playerHealth - damage);

        ApplySchockFeedback();

        if (playerHealth <= 0)
        {
            Debug.Log("PlayerShockHandler: Player defeated by plants/shocks!"); ;
        }
    }
}