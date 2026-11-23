using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables; // XRI 3.x. Bij XRI 2.x: deze regel weghalen.

/// <summary>
/// Bouwt het 3D-grid: kiest start en eind, maakt een oplosbare route, vult de rest op en
/// draait alles door elkaar. Weet niets van schade, score of UI; dat doet de CircuitManager.
/// </summary>
public class CircuitGridGenerator : MonoBehaviour
{
    [Header("3D Grid Spawner Instellingen")]
    [SerializeField] private Transform gridParent;
    [SerializeField] private GameObject startPointPrefab;
    [SerializeField] private GameObject endPointPrefab;
    [Tooltip("De grijpbare tegel (bol/ring met TwistRing). De vorm van de buis bepaalt dit script zelf.")]
    [SerializeField] private GameObject ringPrefab;
    [SerializeField] private GameObject gridSlotPrefab;
    [SerializeField] private int columns = 7;
    [SerializeField] private int rows = 5;
    [SerializeField] private float spacing = 1.5f;
    [Tooltip("Schaal van de tegels. Houd dit kleiner dan 'spacing', anders overlappen ze.")]
    [SerializeField] private float tileScale = 0.8f;

    [Header("Layout")]
    [Tooltip("Start linksboven (0,0), eind in de linkerkolom (rij = rows-2).")]
    [SerializeField] private bool useExampleLayout = true;
    [Tooltip("Minimale lengte van de oplossingsroute (in cellen, incl. start en eind).")]
    [SerializeField] private int minPathLength = 6;
    [Tooltip("Minimale afstand (in vakjes) tussen start en eind. Nooit naast elkaar bij >= 2.")]
    [SerializeField] private int minStartEndDistance = 3;
    [Tooltip("Minimaal aantal keer draaien dat nodig is om de puzzel op te lossen (1-3).")]
    [SerializeField] private int minRequiredMoves = 3;

    [Header("Tegel-vormen (kans voor de opvulling)")]
    [SerializeField] private int weightCap = 1;
    [SerializeField] private int weightStraight = 3;
    [SerializeField] private int weightCorner = 3;
    [SerializeField] private int weightT = 2;
    [SerializeField] private int weightCross = 1;
    [Tooltip("Zet aan als de draairichting omgekeerd aanvoelt.")]
    [SerializeField] private bool invertRotation = false;

    [Header("Planten")]
    [SerializeField] private GameObject plantPrefab;
    [SerializeField] private int plantCount = 2;

    [Header("Start & Eind Poorten (Meerdere Aansluitpunten)")]
    [SerializeField]
    private List<PortDirection> startPorts = new List<PortDirection>
    {
        PortDirection.Noord, PortDirection.Oost, PortDirection.Zuid, PortDirection.West
    };

    [SerializeField]
    private List<PortDirection> endPorts = new List<PortDirection>
    {
        PortDirection.Noord, PortDirection.Oost, PortDirection.Zuid, PortDirection.West
    };

    /// <summary>Bouwt een nieuw grid en geeft de data terug. Geeft null als er iets ontbreekt.</summary>
    public CircuitGridData Generate()
    {
        if (gridParent == null)
        {
            Debug.LogError("FOUT: Grid Parent is niet gekoppeld in de Inspector van de CircuitGridGenerator!");
            return null;
        }

        if (ringPrefab == null)
        {
            Debug.LogError("FOUT: Ring Prefab is niet gekoppeld in de Inspector van de CircuitGridGenerator!");
            return null;
        }

        if (columns * rows < 3)
        {
            Debug.LogError("Grid is te klein! Zorg voor minimaal 3 vakjes.");
            return null;
        }

        if (tileScale > spacing * 0.9f)
        {
            Debug.LogWarning($"Tile Scale ({tileScale}) is bijna zo groot als Spacing ({spacing}): de tegels overlappen. Maak Spacing groter of Tile Scale kleiner.");
        }

        ClearGrid();

        CircuitGridData data = new CircuitGridData
        {
            columns = columns,
            rows = rows,
            rings = new TwistRing[columns, rows],
            plants = new bool[columns, rows],
            startPorts = new List<PortDirection>(startPorts),
            endPorts = new List<PortDirection>(endPorts),
            invertRotation = invertRotation
        };

        ChooseStartAndEnd(data);
        data.path = FindRandomPath(data);

        Dictionary<Vector2Int, int> pathIndex = new Dictionary<Vector2Int, int>();
        for (int i = 0; i < data.path.Count; i++) pathIndex[data.path[i]] = i;

        HashSet<Vector2Int> plantCells = ChoosePlantCells(pathIndex);

        if (!BuildCells(data, pathIndex, plantCells))
        {
            // Niet half laten staan: dan blijft er een kale bol naast het startblok hangen
            ClearGrid();
            return null;
        }
        ScrambleRoute(data);

        // Beginstand tonen (welke tegels staan nu onder stroom)
        HashSet<TwistRing> powered = new HashSet<TwistRing>();
        data.Trace(powered, null);
        data.ApplyPower(powered);

        return data;
    }

    // ---------- Stappen van het genereren ----------

    private void ClearGrid()
    {
        for (int i = gridParent.childCount - 1; i >= 0; i--)
        {
            SafeDestroy(gridParent.GetChild(i).gameObject);
        }
    }

    private void ChooseStartAndEnd(CircuitGridData data)
    {
        Vector2Int exampleStart = new Vector2Int(0, 0);
        Vector2Int exampleEnd = new Vector2Int(0, rows - 2);
        bool exampleOk = useExampleLayout && rows >= 3 &&
                         ManhattanDistance(exampleStart, exampleEnd) >= Mathf.Max(2, minStartEndDistance);

        if (exampleOk)
        {
            data.startPos = exampleStart;
            data.endPos = exampleEnd;
            return;
        }

        int minDist = Mathf.Max(2, minStartEndDistance);
        data.startPos = new Vector2Int(Random.Range(0, columns), Random.Range(0, rows));

        List<Vector2Int> candidates = new List<Vector2Int>();
        Vector2Int farthest = data.startPos;
        int farthestDist = -1;

        for (int i = 0; i < columns * rows; i++)
        {
            Vector2Int c = new Vector2Int(i % columns, i / columns);
            int d = ManhattanDistance(c, data.startPos);
            if (d >= minDist) candidates.Add(c);
            if (d > farthestDist)
            {
                farthestDist = d;
                farthest = c;
            }
        }

        data.endPos = candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : farthest;
    }

    private HashSet<Vector2Int> ChoosePlantCells(Dictionary<Vector2Int, int> pathIndex)
    {
        HashSet<Vector2Int> plantCells = new HashSet<Vector2Int>();
        if (plantPrefab == null) return plantCells;

        List<Vector2Int> freeCells = new List<Vector2Int>();
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                Vector2Int c = new Vector2Int(x, y);
                if (!pathIndex.ContainsKey(c)) freeCells.Add(c);
            }
        }

        Shuffle(freeCells);
        for (int i = 0; i < Mathf.Min(plantCount, freeCells.Count); i++)
        {
            plantCells.Add(freeCells[i]);
        }
        return plantCells;
    }

    private bool BuildCells(CircuitGridData data, Dictionary<Vector2Int, int> pathIndex, HashSet<Vector2Int> plantCells)
    {
        List<Vector2Int> path = data.path;

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                Vector3 slotPos = new Vector3(x * spacing, -y * spacing, 0f);
                Vector3 objPos = new Vector3(slotPos.x, slotPos.y, -0.05f);

                if (gridSlotPrefab != null)
                {
                    GameObject slotObj = Instantiate(gridSlotPrefab, gridParent);
                    slotObj.name = $"GridSlot_{x}_{y}";
                    slotObj.transform.localPosition = slotPos;
                    slotObj.transform.localRotation = Quaternion.identity;
                    slotObj.transform.localScale = Vector3.one;
                }

                if (cell == data.startPos)
                {
                    PlaceStatic(CreatePointObject(startPointPrefab, "StartPoint_3D", new Color(0.2f, 1f, 0.3f), new Vector3(0.65f, 0.65f, 0.65f)), objPos);
                }
                else if (cell == data.endPos)
                {
                    PlaceStatic(CreatePointObject(endPointPrefab, "EndPoint_3D", new Color(1f, 0.65f, 0.1f), new Vector3(0.65f, 0.65f, 0.65f)), objPos);
                }
                else if (plantCells.Contains(cell))
                {
                    GameObject plant = Instantiate(plantPrefab, gridParent);
                    plant.name = $"Plant_3D_{x}_{y}";
                    data.plants[x, y] = true;
                    PlaceStatic(plant, objPos);
                }
                else if (pathIndex.TryGetValue(cell, out int idx))
                {
                    // Tegel op de oplossingsroute: recht of bocht, passend gedraaid
                    PortDirection toPrev = CircuitTileModel.DirectionBetween(cell, path[idx - 1]);
                    PortDirection toNext = CircuitTileModel.DirectionBetween(cell, path[idx + 1]);
                    TileShape shape = CircuitTileModel.Opposite(toPrev) == toNext ? TileShape.Straight : TileShape.Corner;

                    TwistRing ring = SpawnRing(data, shape, objPos, $"Ring_3D_{x}_{y}");
                    if (ring == null) return false;

                    int solvedStep = 0;
                    for (int s = 0; s < 4; s++)
                    {
                        List<PortDirection> p = CircuitTileModel.RotatedPorts(shape, s);
                        if (p.Contains(toPrev) && p.Contains(toNext)) { solvedStep = s; break; }
                    }
                    data.SetStep(ring, solvedStep);
                    data.rings[x, y] = ring;
                }
                else
                {
                    TwistRing ring = SpawnRing(data, RandomFillShape(), objPos, $"Ring_3D_{x}_{y}");
                    if (ring == null) return false;
                    data.SetStep(ring, Random.Range(0, 4));
                    data.rings[x, y] = ring;
                }
            }
        }

        return true;
    }

    // Route-tegels door elkaar draaien, maar niet te makkelijk: de puzzel mag niet binnen
    // (minRequiredMoves - 1) draaien al op te lossen zijn.
    private void ScrambleRoute(CircuitGridData data)
    {
        int maxEasyMoves = Mathf.Clamp(minRequiredMoves, 1, 3) - 1;

        for (int attempt = 0; attempt < 60; attempt++)
        {
            for (int i = 1; i < data.path.Count - 1; i++)
            {
                TwistRing r = data.rings[data.path[i].x, data.path[i].y];
                if (r != null) data.SetStep(r, Random.Range(0, 4));
            }
            if (!data.SolvableWithin(maxEasyMoves)) break;
        }
    }

    // ---------- Spawnen ----------

    private TwistRing SpawnRing(CircuitGridData data, TileShape shape, Vector3 localPos, string objName)
    {
        GameObject obj = Instantiate(ringPrefab, gridParent);
        obj.name = objName;
        obj.transform.localPosition = localPos;
        obj.transform.localScale = Vector3.one * tileScale;

        TwistRing ring = PrepareRing(obj);
        if (ring == null) return null;

        ring.SetSnapPoint(localPos);
        ring.SetPowered(false);

        data.shapes[ring] = shape;
        data.allRings.Add(ring);
        return ring;
    }

    // Zorgt dat de ring de VR-draaibeweging heeft en dat oude grijp-componenten hem niet laten wegvliegen
    private TwistRing PrepareRing(GameObject obj)
    {
        // Oude interactables (zoals XR Grab Interactable) verwijderen: die pakken de ring los van het grid
        foreach (XRBaseInteractable old in obj.GetComponents<XRBaseInteractable>())
        {
            if (old is TwistRing) continue;
            SafeDestroy(old);
        }

        TwistRing ring = obj.GetComponent<TwistRing>();
        if (ring == null)
        {
            Debug.LogWarning($"De Ring Prefab '{ringPrefab.name}' had geen TwistRing: ik heb hem toegevoegd. Voeg TwistRing zelf toe aan de prefab om dit bericht te verwijderen.");
            ring = obj.AddComponent<TwistRing>();
        }

        // Zwaartekracht/physics zou de ring uit het grid laten vallen
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        return ring;
    }

    private void PlaceStatic(GameObject obj, Vector3 localPos)
    {
        if (obj == null) return;
        obj.transform.localPosition = localPos;
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one * tileScale * 0.8f;
    }

    private GameObject CreatePointObject(GameObject prefab, string objectName, Color fallbackColor, Vector3 fallbackScale)
    {
        GameObject obj;
        if (prefab != null)
        {
            obj = Instantiate(prefab, gridParent);
        }
        else
        {
            obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.transform.SetParent(gridParent, false);
            obj.transform.localScale = fallbackScale;

            Renderer rend = obj.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = fallbackColor;
            }
        }

        obj.name = objectName;
        return obj;
    }

    private TileShape RandomFillShape()
    {
        int total = weightCap + weightStraight + weightCorner + weightT + weightCross;
        if (total <= 0) return TileShape.Straight;

        int roll = Random.Range(0, total);
        if ((roll -= weightCap) < 0) return TileShape.Cap;
        if ((roll -= weightStraight) < 0) return TileShape.Straight;
        if ((roll -= weightCorner) < 0) return TileShape.Corner;
        if ((roll -= weightT) < 0) return TileShape.T;
        return TileShape.Cross;
    }

    // ---------- Route zoeken ----------

    private int ManhattanDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private List<Vector2Int> FindRandomPath(CircuitGridData data)
    {
        List<Vector2Int> best = null;

        for (int attempt = 0; attempt < 60; attempt++)
        {
            bool[,] visited = new bool[columns, rows];
            List<Vector2Int> path = new List<Vector2Int> { data.startPos };
            visited[data.startPos.x, data.startPos.y] = true;
            int budget = 3000;

            if (Dfs(data, data.startPos, visited, path, ref budget))
            {
                if (best == null || path.Count > best.Count) best = new List<Vector2Int>(path);
                if (path.Count >= minPathLength) return path;
            }
        }

        return best ?? BfsPath(data);
    }

    private bool Dfs(CircuitGridData data, Vector2Int cur, bool[,] visited, List<Vector2Int> path, ref int budget)
    {
        if (cur == data.endPos) return true;
        if (budget-- <= 0) return false;

        List<PortDirection> dirs = new List<PortDirection>(CircuitTileModel.AllDirections);
        Shuffle(dirs);

        foreach (PortDirection dir in dirs)
        {
            Vector2Int next = CircuitTileModel.Step(cur, dir);
            if (!data.InBounds(next) || visited[next.x, next.y]) continue;

            visited[next.x, next.y] = true;
            path.Add(next);
            if (Dfs(data, next, visited, path, ref budget)) return true;
            path.RemoveAt(path.Count - 1);
            visited[next.x, next.y] = false;
        }
        return false;
    }

    private List<Vector2Int> BfsPath(CircuitGridData data)
    {
        Dictionary<Vector2Int, Vector2Int> prev = new Dictionary<Vector2Int, Vector2Int>();
        Queue<Vector2Int> q = new Queue<Vector2Int>();
        q.Enqueue(data.startPos);
        prev[data.startPos] = data.startPos;

        while (q.Count > 0)
        {
            Vector2Int cur = q.Dequeue();
            if (cur == data.endPos) break;
            foreach (PortDirection dir in CircuitTileModel.AllDirections)
            {
                Vector2Int next = CircuitTileModel.Step(cur, dir);
                if (!data.InBounds(next) || prev.ContainsKey(next)) continue;
                prev[next] = cur;
                q.Enqueue(next);
            }
        }

        List<Vector2Int> result = new List<Vector2Int>();
        Vector2Int c = data.endPos;
        result.Add(c);
        while (c != data.startPos)
        {
            c = prev[c];
            result.Add(c);
        }
        result.Reverse();
        return result;
    }

    // ---------- Kleine hulpjes ----------

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T tmp = list[i];
            list[i] = list[j];
            list[j] = tmp;
        }
    }

    private void SafeDestroy(Object obj)
    {
        if (Application.isPlaying) Destroy(obj);
        else DestroyImmediate(obj);
    }
}