using System.Collections.Generic;
using UnityEngine;

public interface ICells
{
    public Vector3 GetWorldPos();
    public Vector2Int GetGridPos();
}

public struct DirCell : ICells
{
    private Vector3 worldPos;
    private Vector2Int gridPos;
    public readonly Vector3 GetWorldPos() => worldPos;
    public readonly Vector2Int GetGridPos() => gridPos;
    public float cost;
    /// <summary>
    /// Positive Infinity means impassible
    /// </summary>
    public float heat;
    /// <summary>
    /// -1 * Vector2.one means impassible
    /// </summary>
    public Vector2 direction;

    public DirCell(Vector3 worldPos, Vector2Int gridPos)
    {
        this.worldPos = worldPos;
        this.gridPos = gridPos;
        cost = 1;
        heat = float.PositiveInfinity;
        direction = Vector2.zero;
    }
}

public struct ObstacleCell : ICells
{
    private Vector3 worldPos;
    private Vector2Int gridPos;
    public readonly Vector3 GetWorldPos() => worldPos;
    public readonly Vector2Int GetGridPos() => gridPos;
    public readonly List<Obstacles> obstacleList;
    public readonly List<UnitAgent> unitList;

    public ObstacleCell(Vector3 worldPos, Vector2Int gridPos)
    {
        this.worldPos = worldPos;
        this.gridPos = gridPos;
        obstacleList = new List<Obstacles>();
        unitList = new List<UnitAgent>();
    }
}
