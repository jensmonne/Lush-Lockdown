using UnityEngine;

public class OOBGuard : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // The collider may sit on a child, so look up the rigidbody's object.
        var target = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;

        if (target.TryGetComponent(out Respawnable respawnable)) respawnable.Respawn();
    }
}