using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// De uitkomst van het genereren: welke tegel waar staat, hoe hij gedraaid is en
/// hoe de stroom loopt. De generator vult dit, de manager gebruikt het om te controleren.
/// </summary>
public class CircuitGridData
{
    public int columns;
    public int rows;

    public TwistRing[,] rings;
    public bool[,] plants;

    public Vector2Int startPos;
    public Vector2Int endPos;
    public List<PortDirection> startPorts;
    public List<PortDirection> endPorts;
    public bool invertRotation;

    public List<Vector2Int> path = new List<Vector2Int>();
    public readonly List<TwistRing> allRings = new List<TwistRing>();
    public readonly Dictionary<TwistRing, TileShape> shapes = new Dictionary<TwistRing, TileShape>();

    public bool InBounds(Vector2Int p) => p.x >= 0 && p.x < columns && p.y >= 0 && p.y < rows;

    public int GetStep(TwistRing ring) => CircuitTileModel.GetStep(ring.transform, invertRotation);

    public void SetStep(TwistRing ring, int step) => CircuitTileModel.SetStep(ring.transform, step, invertRotation);

    public List<PortDirection> GetRingPorts(TwistRing ring)
    {
        if (ring == null || !shapes.TryGetValue(ring, out TileShape shape)) return new List<PortDirection>();
        return CircuitTileModel.RotatedPorts(shape, GetStep(ring));
    }

    public List<PortDirection> GetPortsAtCell(Vector2Int cell)
    {
        if (cell == startPos) return startPorts;
        if (cell == endPos) return endPorts;

        if (rings != null && InBounds(cell) && rings[cell.x, cell.y] != null)
        {
            return GetRingPorts(rings[cell.x, cell.y]);
        }

        return new List<PortDirection>();
    }

    /// <summary>
    /// Volgt de stroom vanaf de start. Geeft true als het eindpunt bereikt wordt.
    /// poweredRings (optioneel) krijgt alle tegels die onder stroom staan.
    /// onPlantTouched (optioneel) wordt aangeroepen voor elke plant waar de stroom tegenaan loopt.
    /// </summary>
    public bool Trace(HashSet<TwistRing> poweredRings, System.Action onPlantTouched)
    {
        if (rings == null || columns <= 0 || rows <= 0) return false;

        bool[,] visited = new bool[columns, rows];
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        visited[startPos.x, startPos.y] = true;
        queue.Enqueue(startPos);

        bool reachedEnd = false;

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            foreach (PortDirection dir in GetPortsAtCell(current))
            {
                Vector2Int next = CircuitTileModel.Step(current, dir);
                if (!InBounds(next)) continue;

                PortDirection requiredNeighborPort = CircuitTileModel.Opposite(dir);

                if (next == endPos)
                {
                    if (endPorts.Contains(requiredNeighborPort)) reachedEnd = true;
                    continue;
                }

                if (visited[next.x, next.y]) continue;

                if (plants != null && plants[next.x, next.y])
                {
                    visited[next.x, next.y] = true;
                    onPlantTouched?.Invoke();
                    continue;
                }

                TwistRing nextRing = rings[next.x, next.y];
                if (nextRing == null) continue;

                if (!GetRingPorts(nextRing).Contains(requiredNeighborPort)) continue;

                visited[next.x, next.y] = true;
                poweredRings?.Add(nextRing);
                queue.Enqueue(next);
            }
        }

        return reachedEnd;
    }

    public void ApplyPower(HashSet<TwistRing> poweredRings)
    {
        foreach (TwistRing ring in allRings)
        {
            if (ring == null) continue;
            ring.SetPowered(poweredRings.Contains(ring));
        }
    }

    /// <summary>Is de puzzel op te lossen door hoogstens 'moves' tegels 90 graden te draaien?</summary>
    public bool SolvableWithin(int moves, int fromIndex = 0)
    {
        if (Trace(null, null)) return true;
        if (moves <= 0) return false;

        for (int i = fromIndex; i < allRings.Count; i++)
        {
            TwistRing ring = allRings[i];
            if (ring == null) continue;

            int original = GetStep(ring);
            for (int s = 1; s < 4; s++)
            {
                SetStep(ring, original + s);
                if (SolvableWithin(moves - 1, i + 1))
                {
                    SetStep(ring, original);
                    return true;
                }
            }
            SetStep(ring, original);
        }
        return false;
    }
}