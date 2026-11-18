using UnityEngine;


public class CodeCrackerScript : MonoBehaviour
{
    [SerializeField] private GameObject codeCanvas;
    public int Code;

    private void Awake()
    {
        codeCanvas.SetActive(false);
        GenerateRandomCode();
    }

    public int GenerateRandomCode()
    {
        Code = UnityEngine.Random.Range(0, 9999);
        return Code;
    }

    public void CodeCracked()
    {
        codeCanvas.SetActive(true);
    }
}
