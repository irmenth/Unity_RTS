using System.Collections.Generic;
using UnityEngine;

public struct Cell
{
    public Vector3 WorldPos { get; private set; }
    public Vector2Int GridPos { get; private set; }
    public float cost;
    /// <summary>
    /// Positive Infinity means impassible
    /// </summary>
    public float heat;
    /// <summary>
    /// -1 * Vector2.one means impassible
    /// </summary>
    public Vector2 direction;
    public readonly List<Obstacles> obstacleList;

    public Cell(Vector3 worldPos, Vector2Int gridPos)
    {
        WorldPos = worldPos;
        GridPos = gridPos;
        cost = 1;
        heat = float.PositiveInfinity;
        direction = Vector2.zero;
        obstacleList = new List<Obstacles>();
    }
}
