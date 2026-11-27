using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class CodeButtonManager : MonoBehaviour
{
    [Header("Code Settings")]
    [SerializeField] private CodeCrackerScript codeCrackerScript;
    public string currentInput = "";

    [Header("Events")]
    public UnityEvent OnCodeCorrect;
    public UnityEvent OnCodeWrong;

    public void PressNumber(string digit)
    {
        if (!codeCrackerScript.AcceptsInput) return;
        if (currentInput.Length >= 4) return;

        currentInput += digit;
        Debug.Log("Current Input: " + currentInput);
    }

    public void EnterInput()
    {
        if (!codeCrackerScript.AcceptsInput) return;

        {
            if (currentInput == codeCrackerScript.Code.ToString("D4"))
            {
                Debug.Log("Access Granted!");
                OnCodeCorrect.Invoke();
                Button[] btns = gameObject.GetComponentsInChildren<Button>();
                foreach (Button btn in btns)
                    btn.enabled = false;
            }
            else
            {
                Debug.Log("Wrong Code! Resetting...");
                OnCodeWrong.Invoke();
                ResetInput();
            }
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