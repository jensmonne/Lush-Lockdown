using UnityEngine;

/// <summary>
/// Put this on one big trigger box around the playable area. Anything Respawnable that leaves it gets respawned.
/// </summary>
public class OOBGuard : MonoBehaviour
{
    private void OnTriggerExit(Collider other)
    {
        // The collider may sit on a child, so look up the rigidbody's object.
        var target = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;

        if (target.TryGetComponent(out Respawnable respawnable)) respawnable.Respawn();
    }
}