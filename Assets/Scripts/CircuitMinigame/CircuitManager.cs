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
    [Header("Grid Visuals")]
    [SerializeField] private GameObject gridSlotPrefab; // Een visueel rondje/vakje dat de snap-plek op het grid aangeeft
    [Header("UI Feedback")]
    [SerializeField] private WorldScreen puzzleScreen;
    private void Start()
    {
        GenerateGrid();
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
    [ContextMenu("Generate Grid Now")]
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
            Debug.LogError("FOUT: Eén of meer prefabs (Start, Eind, Ring) ontbreken of zijn geen echte prefabs in de Project-map!");
            return;
        }

        // 1. Maak het grid eerst leeg (veilig voor zowel Play-mode als Edit-mode)
        for (int i = gridParent.childCount - 1; i >= 0; i--)
        {
            Transform child = gridParent.GetChild(i);
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
        allRings.Clear();

        int totalCells = columns * rows;
        Debug.Log($"Totaal aantal cellen om te spawnen: {totalCells} (Kolommen: {columns}, Rijen: {rows})");

        if (totalCells < 2)
        {
            Debug.LogError("Grid is te klein! Zorg voor minimaal 2 vakjes (bijv. 2x1 of 3x3).");
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

            // A. Spawn altijd eerst een visueel Snap Point / Grid Cel op deze plek
            if (gridSlotPrefab != null)
            {
                GameObject slotObj = Instantiate(gridSlotPrefab, gridParent);
                slotObj.name = $"GridSlot_{x}_{y}";
                slotObj.transform.localPosition = spawnPosition;
                slotObj.transform.localRotation = Quaternion.identity;
                slotObj.transform.localScale = Vector3.one;
            }

            // B. Spawn daarna de Ring, Start of Eindpunt op exact dezelfde positie
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
                spawnedObj.name = $"Ring_3D_{i}"; // GOED

                RotatableRing newRing = spawnedObj.GetComponent<RotatableRing>();
                if (newRing != null)
                {
                    allRings.Add(newRing);
                }
            }

            if (spawnedObj != null)
            {
                // Zet ze ietsjes vóór de grid slots (bijv. -0.05 op Z) zodat ze er mooi bovenop liggen
                spawnedObj.transform.localPosition = new Vector3(spawnPosition.x, spawnPosition.y, -0.05f);
                spawnedObj.transform.localRotation = Quaternion.identity;
                spawnedObj.transform.localScale = Vector3.one;
            }
        }

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
