using UnityEngine;

public class WorldScreen : MonoBehaviour
{
    [SerializeField] private Canvas paneelCanvas;
    [SerializeField] private bool isAlwaysVisable;
    private void Start()
    {
        // Controleer of de Canvas referentie is ingesteld, zo niet, pak hem automatisch
        if (paneelCanvas == null)
        {
            paneelCanvas = GetComponent<Canvas>();
        }

        // Zet de zichtbaarheid bij de start op basis van het attribuut
        SetScreenVisibility(isAlwaysVisable);
    }
    public void KeepScreenActive()
    {
        if (paneelCanvas != null && !paneelCanvas.enabled)
        {
            paneelCanvas.enabled = true;
            Debug.Log("WorldPuzzleScreen: Scherm is geforceerd actief gezet.");
        }
    }
    public void SetScreenVisibility(bool visible)
    {
        if (paneelCanvas != null)
        {
            paneelCanvas.enabled = visible;
        }
    }
}

