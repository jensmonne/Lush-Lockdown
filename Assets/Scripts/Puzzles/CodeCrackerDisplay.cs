using UnityEngine;
using TMPro;

public class CodeCrackerDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI codeText;
    public CodeCrackerScript codeCrackerScript;
   
    private void Update()
    {
        UpdateCode();
    }

    private void UpdateCode()
    {
        if (codeCrackerScript != null)
        {
            codeText.text = codeCrackerScript.Code.ToString("D4");
        }
    }
}
