using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables; // XRI 3.x. For XRI 2.x: remove this line.

/// <summary>
/// Builds the 3D grid: picks start and end, creates a solvable route, fills the rest, and
/// scrambles everything. Knows nothing about damage, score, or UI; CircuitManager handles that..
/// </summary>
public class CircuitGridGenerator : MonoBehaviour
{
    [Header("3D Grid Spawner Settings")]
    [SerializeField] private Transform gridParent;
    [SerializeField] private GameObject startPointPrefab;
    [SerializeField] private GameObject endPointPrefab;
    [Tooltip("The grabbable tile (sphere/ring with TwistRing). The pipe shape is determined automatically by this script.")]
    [SerializeField] private GameObject ringPrefab;
    [SerializeField] private GameObject gridSlotPrefab;
    [SerializeField] private int columns = 7;
    [SerializeField] private int rows = 5;
    [SerializeField] private float spacing = 1.5f;
    [Tooltip("Scale of the tiles. Keep this smaller than 'spacing', otherwise they will overlap.")]
    [SerializeField] private float tileScale = 0.8f;

    [Header("Layout")]
    [Tooltip("Start leftcorner (0,0), end in the leftcollum (collum = rows-2).")]
    [SerializeField] private bool useExampleLayout = true;
    [Tooltip("Minimum length of the solution path (in cells, incl. start and end).")]
    [SerializeField] private int minPathLength = 6;
    [Tooltip("Minimum distance (in tiles) between start and end. Never adjacent when >= 2.")]
    [SerializeField] private int minStartEndDistance = 3;
    [Tooltip("Minimum number of rotations required to solve the puzzle (1-3).")]
    [SerializeField] private int minRequiredMoves = 3;

    [Header("Tile Shapes (Fill Weights)")]
    [SerializeField] private int weightCap = 1;
    [SerializeField] private int weightStraight = 3;
    [SerializeField] private int weightCorner = 3;
    [SerializeField] private int weightT = 2;
    [SerializeField] private int weightCross = 1;
    [Tooltip("Enable if the rotation direction feels reversed.")]
    [SerializeField] private bool invertRotation = false;

    [Header("Plants")]
    [SerializeField] private GameObject plantPrefab;
    [SerializeField] private int plantCount = 2;

    [Header("Start & End Ports (Multiple Connection Points)")]
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
    /// <summary>Builds a new grid and returns the data. Returns null if something is missing..</summary>
    public CircuitGridData Generate()
    {
        if (gridParent == null)
        {
            Debug.LogError("ERROR: Grid Parent is not assigned in the Inspector of CircuitGridGenerator!");
            return null;
        }

        if (ringPrefab == null)
        {
            Debug.LogError("ERROR: Ring Prefab is not assigned in the Inspector of CircuitGridGenerator!");
            return null;
        }

        if (columns * rows < 3)
        {
            Debug.LogError("Grid is too small! Make sure there are at least 3 tiles.");
            return null;
        }

        if (tileScale > spacing * 0.9f)
        {
            Debug.LogWarning($"Tile Scale ({tileScale}) is nearly as large as Spacing ({spacing}): tiles will overlap. Increase Spacing or decrease Tile Scale.");
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
            //Don't leave it half-built/unfinished: otherwise a bare sphere is left hanging next to the start block.
            ClearGrid();
            return null;
        }
        ScrambleRoute(data);

        //Show initial state (which tiles are currently powered).
        HashSet<TwistRing> powered = new HashSet<TwistRing>();
        data.Trace(powered, null);
        data.ApplyPower(powered);

        return data;
    }

    // ---------- Generation steps (the sequence in which the generator builds the 3D grid)----------

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
                    // Tile on the solution path: straight or corner/bend, rotated to fit.
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

    // Scramble path tiles, but not too easily: the puzzle must not already be solvable within (minRequiredMoves - 1) rotations.
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

    // Ensures the ring has the VR rotation movement and that legacy grab components do not cause it to fly away.
    private TwistRing PrepareRing(GameObject obj)
    {
        //Remove old/legacy interactables (such as XR Grab Interactable): they detach the ring from the grid.
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

        // Gravity/physics would make the ring fall out of the grid.
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

    // ---------- path finding ----------

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

    // ---------- small help ----------

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