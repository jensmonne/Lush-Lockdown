using UnityEngine;

public class VaultDoor : MonoBehaviour
{
    [SerializeField] private GameObject vault;
    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private float rotationAngle = -90f;
    [SerializeField] private VaultAudio vaultAudio;

    private Quaternion targetRotation;
    private bool isMoving;

    private void Awake()
    {
        if (vault != null)
        {
            targetRotation = vault.transform.rotation;
        }
    }

    private void Update()
    {
        if (!isMoving || vault == null)
        {
            return;
        }

        vault.transform.rotation = Quaternion.RotateTowards(
            vault.transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );

        if (Quaternion.Angle(vault.transform.rotation, targetRotation) < 0.01f)
        {
            vault.transform.rotation = targetRotation;
            isMoving = false;
        }
    }

    public void OpenVault()
    {
        vaultAudio.OpeningVault();
        SetTargetAngle(rotationAngle);
    }

    public void CloseVault()
    {
        SetTargetAngle(0f);
    }

    private void SetTargetAngle(float yAngle)
    {
        if (vault == null)
        {
            return;
        }

        Vector3 currentEuler = vault.transform.eulerAngles;
        targetRotation = Quaternion.Euler(currentEuler.x, yAngle, currentEuler.z);
        isMoving = true;
    }
}