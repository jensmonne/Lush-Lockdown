using UnityEngine;


public class CodeCrackerScript : MonoBehaviour
{
    [SerializeField] private GameObject codeCanvas;
    [SerializeField] private VaultDoor vaultDoor;
    public int Code;

    private void Awake()
    {
        GenerateRandomCode();
    }

    public int GenerateRandomCode()
    {
        Code = UnityEngine.Random.Range(0, 10000);
        Debug.Log(Code.ToString("D4"));
        return Code;

    }

    public void CodeCracked()
    {
        vaultDoor.OpenVault();
        
    }
}
