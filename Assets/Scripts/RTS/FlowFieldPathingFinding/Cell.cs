using UnityEngine;

public struct Cell
{
    public Vector3 WorldPos { get; private set; }
    public Vector2Int GridPos { get; private set; }
    public float cost;
    public float heat;
    public Vector3 direction;

    public Cell(Vector3 worldPos, Vector2Int gridPos)
    {
        WorldPos = worldPos;
        GridPos = gridPos;
        cost = 1;
        heat = float.PositiveInfinity;
        direction = Vector3.zero;
    }
}
