using UnityEngine;

public class WorldScreen : MonoBehaviour
{
    [SerializeField] private GameObject paneelCanvas;
    [SerializeField] private bool isAlwaysVisable;

    public void KeepScreenActive()
    {
        if (isAlwaysVisable = true)
        {
            paneelCanvas.SetActive(true);
        }
    }
}

