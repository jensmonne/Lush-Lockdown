using UnityEngine;


public class CodeCrackerScript : MonoBehaviour
{
    [SerializeField] private GameObject codeCanvas;
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

    public void CodeCracked(bool CheckForCode)
    {

    }
}
