using System.Collections.Generic;
using UnityEngine;

public enum TileShape { Cap, Straight, Corner, T, Cross }

/// <summary>
/// Alles over de vorm en draaiing van een tegel en over richtingen op het grid.
/// Geen scene-afhankelijkheden, wordt gedeeld door de generator en de manager.
/// </summary>
public static class CircuitTileModel
{
    public static readonly PortDirection[] AllDirections =
    {
        PortDirection.Noord, PortDirection.Oost, PortDirection.Zuid, PortDirection.West
    };

    // Volgorde tegen de klok in (zoals een positieve Z-rotatie in Unity)
    private static readonly PortDirection[] CcwOrder =
    {
        PortDirection.Noord, PortDirection.West, PortDirection.Zuid, PortDirection.Oost
    };

    public static List<PortDirection> BasePorts(TileShape s)
    {
        switch (s)
        {
            case TileShape.Cap: return new List<PortDirection> { PortDirection.Noord };
            case TileShape.Straight: return new List<PortDirection> { PortDirection.Noord, PortDirection.Zuid };
            case TileShape.Corner: return new List<PortDirection> { PortDirection.Noord, PortDirection.Oost };
            case TileShape.T: return new List<PortDirection> { PortDirection.Noord, PortDirection.Oost, PortDirection.West };
            default: return new List<PortDirection> { PortDirection.Noord, PortDirection.Oost, PortDirection.Zuid, PortDirection.West };
        }
    }

    public static PortDirection RotatePort(PortDirection d, int steps)
    {
        int idx = System.Array.IndexOf(CcwOrder, d);
        if (idx < 0) return d;
        return CcwOrder[(idx + ((steps % 4) + 4) % 4) % 4];
    }

    public static List<PortDirection> RotatedPorts(TileShape s, int step)
    {
        List<PortDirection> ports = BasePorts(s);
        for (int i = 0; i < ports.Count; i++) ports[i] = RotatePort(ports[i], step);
        return ports;
    }

    // Leest de draaistand (0..3) uit de Z-rotatie van de transform
    public static int GetStep(Transform t, bool invert)
    {
        float z = t.localEulerAngles.z;
        int step = Mathf.RoundToInt(z / 90f) % 4;
        if (step < 0) step += 4;
        if (invert) step = (4 - step) % 4;
        return step;
    }

    public static void SetStep(Transform t, int step, bool invert)
    {
        step = ((step % 4) + 4) % 4;
        if (invert) step = (4 - step) % 4;
        t.localRotation = Quaternion.Euler(0f, 0f, step * 90f);
    }

    public static Vector2Int Step(Vector2Int p, PortDirection dir)
    {
        switch (dir)
        {
            case PortDirection.Noord: return new Vector2Int(p.x, p.y - 1);
            case PortDirection.Oost: return new Vector2Int(p.x + 1, p.y);
            case PortDirection.Zuid: return new Vector2Int(p.x, p.y + 1);
            case PortDirection.West: return new Vector2Int(p.x - 1, p.y);
            default: return p;
        }
    }

    public static PortDirection Opposite(PortDirection dir)
    {
        switch (dir)
        {
            case PortDirection.Noord: return PortDirection.Zuid;
            case PortDirection.Oost: return PortDirection.West;
            case PortDirection.Zuid: return PortDirection.Noord;
            case PortDirection.West: return PortDirection.Oost;
            default: return dir;
        }
    }

    public static PortDirection DirectionBetween(Vector2Int a, Vector2Int b)
    {
        if (b.x > a.x) return PortDirection.Oost;
        if (b.x < a.x) return PortDirection.West;
        if (b.y > a.y) return PortDirection.Zuid;
        return PortDirection.Noord;
    }
}