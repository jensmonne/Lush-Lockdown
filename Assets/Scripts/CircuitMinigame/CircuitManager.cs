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

    [Header("Planten Gevaar")]
    [SerializeField] private GameObject plantPrefab;
    [SerializeField] private int plantCount = 2;
    [SerializeField] private int plantDamage = 25;

    [Header("Start & Eind Richting")]
    [SerializeField] private PortDirection startOutputDir = PortDirection.Oost;
    [SerializeField] private PortDirection endInputDir = PortDirection.West;

    [Header("Grid Visuals")]
    [SerializeField] private GameObject gridSlotPrefab; // Een visueel rondje/vakje dat de snap-plek op het grid aangeeft

    [Header("UI Feedback")]
    [SerializeField] private WorldScreen puzzleScreen;

    private RotatableRing[,] ringGrid;
    private bool[,] plantGrid;
    private Vector2Int startPos;
    private Vector2Int endPos;

    private void Start()
    {
        Initializepuzzel();
        GenerateGrid();
    }

    public void Initializepuzzel()
    {
        if (puzzleScreen != null)
        {
            puzzleScreen.KeepScreenActive();
        }
        IsCompleted = false;
        currentAttempts = 0;
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
        IsCompleted = false;

        int totalCells = columns * rows;
        Debug.Log($"Totaal aantal cellen om te spawnen: {totalCells} (Kolommen: {columns}, Rijen: {rows})");

        if (totalCells < 2)
        {
            Debug.LogError("Grid is te klein! Zorg voor minimaal 2 vakjes (bijv. 2x1 of 3x3).");
            return;
        }

        ringGrid = new RotatableRing[columns, rows];
        plantGrid = new bool[columns, rows];

        // 2. Kies een willekeurige plek voor het Startpunt en Eindpunt
        int startIndex = Random.Range(0, totalCells);
        int endIndex = startIndex;
        while (endIndex == startIndex)
        {
            endIndex = Random.Range(0, totalCells);
        }

        Debug.Log($"Startpunt index: {startIndex}, Eindpunt index: {endIndex}");

        // Kies vrije cellen voor planten (niet op startIndex of endIndex)
        HashSet<int> plantIndices = new HashSet<int>();
        int maxPlants = Mathf.Min(plantCount, Mathf.Max(0, totalCells - 2));
        int safetyCounter = 0;

        while (plantIndices.Count < maxPlants && safetyCounter < totalCells * 10)
        {
            int candidate = Random.Range(0, totalCells);
            if (candidate != startIndex && candidate != endIndex)
            {
                plantIndices.Add(candidate);
            }
            safetyCounter++;
        }

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

            // B. Spawn daarna de Ring, Plant, Start of Eindpunt op exact dezelfde positie
            GameObject spawnedObj = null;

            if (i == startIndex)
            {
                spawnedObj = Instantiate(startPointPrefab, gridParent);
                spawnedObj.name = "StartPoint_3D";
                startPos = new Vector2Int(x, y);
            }
            else if (i == endIndex)
            {
                spawnedObj = Instantiate(endPointPrefab, gridParent);
                spawnedObj.name = "EndPoint_3D";
                endPos = new Vector2Int(x, y);
            }
            else if (plantIndices.Contains(i) && plantPrefab != null)
            {
                spawnedObj = Instantiate(plantPrefab, gridParent);
                spawnedObj.name = $"Plant_3D_{i}";
                plantGrid[x, y] = true;
            }
            else
            {
                spawnedObj = Instantiate(ringPrefab, gridParent);
                spawnedObj.name = $"Ring_3D_{i}";

                RotatableRing newRing = spawnedObj.GetComponent<RotatableRing>();
                if (newRing != null)
                {
                    newRing.SetSnapPoint(spawnPosition);
                    newRing.SetPowered(false);
                    newRing.OnRingRotated -= HandleRingRotated;
                    newRing.OnRingRotated += HandleRingRotated;
                    ringGrid[x, y] = newRing;
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

        currentAttempts++;

        if (CheckCircuit())
        {
            triggerSuccessState();
        }
    }

    public bool CheckCircuit()
    {
        if (ringGrid == null || columns <= 0 || rows <= 0) return false;

        bool[,] visited = new bool[columns, rows];
        List<RotatableRing> activePathRings = new List<RotatableRing>();

        bool reachedEnd = CanReachEnd(startPos.x, startPos.y, startOutputDir, visited, activePathRings);

        UpdateRingHighlights(reachedEnd ? activePathRings : null);
        return reachedEnd;
    }

    private void UpdateRingHighlights(List<RotatableRing> poweredRings)
    {
        foreach (RotatableRing ring in allRings)
        {
            if (ring == null) continue;
            bool shouldLightUp = poweredRings != null && poweredRings.Contains(ring);
            ring.SetPowered(shouldLightUp);
        }
    }

    private bool CanReachEnd(int x, int y, PortDirection entryDir, bool[,] visited, List<RotatableRing> pathRings)
    {
        // Vanaf het startpunt stappen we direct de eerste buur binnen
        if (x == startPos.x && y == startPos.y)
        {
            visited[x, y] = true;
            Vector2Int next = Step(x, y, startOutputDir);
            return InBounds(next) && CanReachEnd(next.x, next.y, Opposite(startOutputDir), visited, pathRings);
        }

        if (!InBounds(new Vector2Int(x, y)) || visited[x, y]) return false;
        visited[x, y] = true;

        // Plant blokkeert het pad en geeft damage + schok
        if (plantGrid != null && plantGrid[x, y])
        {
            if (shockHandler != null)
            {
                shockHandler.TakeDamageAndShock(plantDamage);
            }
            return false;
        }

        // Eindpunt bereikt als we het vanaf de verwachte ingangsrichting binnengaan
        if (x == endPos.x && y == endPos.y)
        {
            return entryDir == endInputDir;
        }

        RotatableRing ring = ringGrid[x, y];
        if (ring == null) return false;

        List<PortDirection> ports = ring.GetPortDirections();
        if (!ports.Contains(entryDir)) return false;

        pathRings.Add(ring);

        foreach (PortDirection exitDir in ports)
        {
            if (exitDir == entryDir) continue;
            Vector2Int next = Step(x, y, exitDir);
            if (CanReachEnd(next.x, next.y, Opposite(exitDir), visited, pathRings))
            {
                return true;
            }
        }

        pathRings.Remove(ring);
        return false;
    }

    private bool InBounds(Vector2Int p) => p.x >= 0 && p.x < columns && p.y >= 0 && p.y < rows;

    private Vector2Int Step(int x, int y, PortDirection dir) => dir switch
    {
        PortDirection.Noord => new Vector2Int(x, y - 1),
        PortDirection.Oost => new Vector2Int(x + 1, y),
        PortDirection.Zuid => new Vector2Int(x, y + 1),
        PortDirection.West => new Vector2Int(x - 1, y),
        _ => new Vector2Int(x, y)
    };

    private PortDirection Opposite(PortDirection dir) => dir switch
    {
        PortDirection.Noord => PortDirection.Zuid,
        PortDirection.Oost => PortDirection.West,
        PortDirection.Zuid => PortDirection.Noord,
        PortDirection.West => PortDirection.Oost,
        _ => dir
    };

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