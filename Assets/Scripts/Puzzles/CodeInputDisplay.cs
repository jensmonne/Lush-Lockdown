using UnityEngine;
using TMPro;

public class CodeInputDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI inputText;

    [SerializeField] private CodeButtonManager codeButtonManager;

    void Update()
    {
        inputText.text = codeButtonManager.currentInput;
    }
}
