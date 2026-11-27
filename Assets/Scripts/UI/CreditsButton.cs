using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

public class CreditsButton : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private GameObject creditsButton;
    [SerializeField] private GameObject startButton;
    [SerializeField] private GameObject ReturnButton;

    [Header("Text")]
    [SerializeField] private GameObject DevText;
    [SerializeField] private GameObject ArtText;

    private void Awake()
    {
        PressReturn();
    }

    public void PressCredits()
    {
            creditsButton.SetActive(false);
            startButton.SetActive(false);

            ReturnButton.SetActive(true);
            DevText.SetActive(true);
            ArtText.SetActive(true);
    }
    public void PressReturn()
    {
        ReturnButton.SetActive(false);
        DevText.SetActive(false);
        ArtText.SetActive(false);

        creditsButton.SetActive(true);
        startButton.SetActive(true);

    }
}
