using System.Globalization;
using UnityEngine;
using UnityEngine.Events;

public class CodeButtonManager : MonoBehaviour
{
    [Header("Code Settings")]
    [SerializeField] private CodeCrackerScript codeCrackerScript;
    public string currentInput = "";

    [Header("Events")]

    public UnityEvent onCodeCorrect;
    public UnityEvent onCodeWrong;

    public void PressNumber(string digit)
    {
        if (currentInput.Length >= 4) return;

        currentInput += digit;
        Debug.Log("Current Input: " + currentInput);
    }

    public void EnterInput()
    {
        if (currentInput == codeCrackerScript.Code.ToString("D4")) 
        {
            Debug.Log("Access Granted!");
            onCodeCorrect.Invoke();
        }
        else
        {
            Debug.Log("Wrong Code! Resetting...");
            onCodeWrong.Invoke();
            ResetInput();
        }
    }

    public void ResetInput()
    {
        currentInput = "";
    }

    public void BackspaceInput()
    {
        if (currentInput.Length >= 1)
        {
            currentInput = currentInput.Substring(0, currentInput.Length - 1);
            Debug.Log("Current Input: " + currentInput);
        }
    }
}