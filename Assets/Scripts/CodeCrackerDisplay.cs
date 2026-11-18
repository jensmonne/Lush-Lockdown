using UnityEngine;
using TMPro;
using System;

public class CodeCrackerDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI codeText;
    private CodeCrackerScript codeCrackerScript;

    private void Start()
    {
        codeText.text = GetComponent<CodeCrackerScript>().Code.ToString();
    }
}
