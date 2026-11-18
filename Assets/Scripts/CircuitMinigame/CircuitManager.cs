using System.Collections.Generic;
using UnityEngine;
public class CircuitManager : MonoBehaviour
{
    [SerializeField] private string puzzleId;
    [SerializeField] private bool IsCompleted;
    [SerializeField] private List<RotatableRing> allRings = new List<RotatableRing>();
    [SerializeField] private int currentAttempts;
    [Header("Visuals & Feedback")]
    [SerializeField] private CircuitVisualController visualController;
    [SerializeField] private PlayerShockHandler shockHandler;
    [Header("3D Grid Spawner Instellingen")]
    [SerializeField] private Transform gridParent; // Een leeg GameObject in de scene waar de objecten onder komen
    [SerializeField] private GameObject startPointPrefab;
    [SerializeField] private GameObject endPointPrefab;
    [SerializeField] private GameObject ringPrefab; // Je 3D ring prefab met Collider en XR Grab Interactable
    [SerializeField] private int columns = 3;
    [SerializeField] private int rows = 3;
    [SerializeField] private float spacing = 1.5f; // De afstand tussen de 3D objecten in meters

    [Header("UI Feedback")]
    [SerializeField] private WorldScreen puzzleScreen;
    private void Start()
    {
       Initializepuzzel();
    }
    public void Initializepuzzel() 
    {
        if (puzzleScreen != null)
        {
            puzzleScreen.KeepScreenActive();
        }
        IsCompleted = false;
        if(allRings == null|| allRings.Count == 0 ) 
        {
            allRings = new List<RotatableRing>(FindObjectsOfType<RotatableRing>());
        }
        foreach (RotatableRing ring in allRings) 
        {
            ring.OnRingRotated += HandleRingRotated;
        }
    }
    public void GenerateGrid()
    {
        Debug.Log("GenerateGrid gestart!");

        if (gridParent == null)
        {
            Debug.LogError("FOUT: Grid Parent is niet gekoppeld in de Inspector!");
            return;
        }

        if (ringPrefab == null || startPointPrefab == null || endPointPrefab == null)
        {
            Debug.LogError("FOUT: Eén of meer prefabs (Start, Eind, Ring) ontbreken in de Inspector!");
            return;
        }

        // 1. Maak het grid eerst leeg
        foreach (Transform child in gridParent)
        {
            Destroy(child.gameObject);
        }
        allRings.Clear();

        int totalCells = columns * rows;
        Debug.Log($"Totaal aantal cellen om te spawnen: {totalCells} (Kolommen: {columns}, Rijen: {rows})");

        if (totalCells < 2)
        {
            Debug.LogError("Grid is te klein! Zorg voor minimaal 2 vakjes.");
            return;
        }

        // 2. Kies een willekeurige plek voor het Startpunt en Eindpunt
        int startIndex = Random.Range(0, totalCells);
        int endIndex = startIndex;
        while (endIndex == startIndex)
        {
            endIndex = Random.Range(0, totalCells);
        }

        Debug.Log($"Startpunt index: {startIndex}, Eindpunt index: {endIndex}");

        // 3. Bouw het 3D grid op
        for (int i = 0; i < totalCells; i++)
        {
            int x = i % columns;
            int y = i / columns;

            Vector3 spawnPosition = new Vector3(x * spacing, -y * spacing, 0f);
            GameObject spawnedObj = null;

            if (i == startIndex)
            {
                spawnedObj = Instantiate(startPointPrefab, gridParent);
                spawnedObj.name = "StartPoint_3D";
            }
            else if (i == endIndex)
            {
                spawnedObj = Instantiate(endPointPrefab, gridParent);
                spawnedObj.name = "EndPoint_3D";
            }
            else
            {
                spawnedObj = Instantiate(ringPrefab, gridParent);
                spawnedObj.name = $"Ring_3D_{i}";

                RotatableRing newRing = spawnedObj.GetComponent<RotatableRing>();
                if (newRing != null)
                {
                    allRings.Add(newRing);
                }
                else
                {
                    Debug.LogWarning($"Waarschuwing: Gespawnde ring ({spawnedObj.name}) heeft geen RotatableRing component!");
                }
            }

            if (spawnedObj != null)
            {
                spawnedObj.transform.localPosition = spawnPosition;
                spawnedObj.transform.localRotation = Quaternion.identity;
                spawnedObj.transform.localScale = Vector3.one; // Zorg dat de schaal niet 0 is
            }
        }

        Debug.Log($"Grid generatie voltooid! Aantal geldige ringen in lijst: {allRings.Count}");
    }
    private void HandleRingRotated()
    {
        if (IsCompleted) return;

        // Controleer direct of het circuit nu compleet is
        bool success = CheckCircuit();

        if (success)
        {
            triggerSuccessState();
        } 
    }

    public bool CheckCircuit()
    {
        foreach (RotatableRing ring in allRings)
        {
            if (!ring.IsCorrectAligned)
            {
                return false; // Als er één ring verkeerd staat, is het circuit niet compleet
            }
        }

        return true;
    }
    public void triggerSuccessState() 
    {
       IsCompleted = true;
       Debug.Log("Puzzel afgerond");
        if (visualController != null) 
        {
            visualController.SetPowerActive(true);
            visualController.PlaySparkeffect();
        }
    }
    public void triggerPenalty() 
    {
        Debug.Log("Somethings wrong");
        if (shockHandler != null) 
        {
            shockHandler.ApplySchockFeedback();

        }
        if (visualController != null) 
        {
            visualController.PlayElectricShockEffect();
        }

    }
}
