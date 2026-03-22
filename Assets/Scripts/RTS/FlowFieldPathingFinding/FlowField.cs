using System.Collections.Generic;
using UnityEngine;

public class FlowField
{
    public Cell[,] Grid { get; private set; }
    public readonly int gridWidth;
    public readonly int gridHeight;
    public readonly float cellRadius;
    public readonly float cellDiameter;

    public FlowField(int gridWidth, int gridHeight, float cellRadius)
    {
        this.gridWidth = gridWidth;
        this.gridHeight = gridHeight;
        this.cellRadius = cellRadius;
        this.cellDiameter = cellRadius * 2f;
        Grid = new Cell[gridWidth, gridHeight];
    }

    public void GenerateGrid()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                var worldPos = new Vector3(cellDiameter * x + cellRadius, 0, cellDiameter * y + cellRadius);
                var gridPos = new Vector2Int(x, y);
                Grid[x, y] = new Cell(worldPos, gridPos);
            }
        }
    }

    private readonly Collider[] lapBoxHitBuffter = new Collider[20];

    public void GenerateCostField(LayerMask costLayerMask, int impassibleLayer, int roughLayer)
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                int hitCount = Physics.OverlapBoxNonAlloc(Grid[x, y].WorldPos, Vector3.one * cellRadius, lapBoxHitBuffter, Quaternion.identity, costLayerMask);

                bool hasRecordRough = false, hasRecordImpassible = false;
                for (int i = 0; i < hitCount; i++)
                {
                    if (float.IsInfinity(Grid[x, y].cost)) break;

                    if (!hasRecordImpassible && lapBoxHitBuffter[i].gameObject.layer == impassibleLayer)
                    {
                        Grid[x, y].cost += float.PositiveInfinity;
                        hasRecordImpassible = true;
                    }
                    else if (!hasRecordRough && lapBoxHitBuffter[i].gameObject.layer == roughLayer)
                    {
                        Grid[x, y].cost += 1f;
                        hasRecordRough = true;
                    }
                }
            }
        }
    }

    /// <summary>
    /// </summary>
    /// <param name="worldPos"></param>
    /// <returns>
    /// (-1, -1) if out of range
    /// </returns>
    public Vector2Int WorldToGridPos(Vector3 worldPos)
    {
        var gridPos = new Vector2Int(Mathf.FloorToInt(worldPos.x / cellDiameter), Mathf.FloorToInt(worldPos.z / cellDiameter));
        if (gridPos.x < 0 || gridPos.x >= gridWidth || gridPos.y < 0 || gridPos.y >= gridHeight) return new Vector2Int(-1, -1);
        return gridPos;
    }

    private readonly Queue<int> openList = new();

    public void GenerateHeatMap(Vector2Int destinationGridPos)
    {
        openList.Clear();
        var closedList = new bool[gridWidth * gridHeight];

        if (float.IsInfinity(Grid[destinationGridPos.x, destinationGridPos.y].cost)) return;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Grid[x, y].heat = float.PositiveInfinity;
            }
        }
        Grid[destinationGridPos.x, destinationGridPos.y].heat = 0;
        openList.Enqueue(destinationGridPos.x * gridHeight + destinationGridPos.y);

        while (openList.Count > 0)
        {
            var curIndex = openList.Dequeue();
            var curGridPos = new Vector2Int(curIndex / gridHeight, curIndex % gridHeight);
            closedList[curIndex] = true;

            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if (i == 0 && j == 0) continue;
                    Vector2Int newGridPos = new(Grid[curGridPos.x, curGridPos.y].GridPos.x + i, Grid[curGridPos.x, curGridPos.y].GridPos.y + j);
                    var newIndex = newGridPos.x * gridHeight + newGridPos.y;
                    if (newGridPos.x < 0 || newGridPos.x >= gridWidth || newGridPos.y < 0 || newGridPos.y >= gridHeight) continue;
                    if (closedList[newGridPos.x * gridHeight + newGridPos.y]) continue;

                    if (float.IsInfinity(Grid[newGridPos.x, newGridPos.y].cost))
                    {
                        closedList[newIndex] = true;
                        continue;
                    }

                    var moveCost = Grid[newGridPos.x, newGridPos.y].cost;
                    if (i * j != 0)
                        moveCost *= 1.4f;

                    var newCost = Grid[curGridPos.x, curGridPos.y].heat + moveCost;
                    if (newCost < Grid[newGridPos.x, newGridPos.y].heat)
                    {
                        Grid[newGridPos.x, newGridPos.y].heat = newCost;
                        if (!openList.Contains(newIndex))
                            openList.Enqueue(newIndex);
                    }

                }
            }
        }
    }

    public void GenerateFlowField()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Grid[x, y].direction = Vector3.zero;
            }
        }

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (float.IsInfinity(Grid[x, y].cost))
                {
                    Grid[x, y].direction = -1 * Vector3.one;
                    continue;
                }

                var minHeat = Grid[x, y].heat;
                var dir = Vector2Int.zero;
                for (int i = -1; i <= 1; i++)
                {
                    for (int j = -1; j <= 1; j++)
                    {
                        if (i == 0 && j == 0) continue;
                        var newGridPos = new Vector2Int(x + i, y + j);
                        if (newGridPos.x < 0 || newGridPos.x >= gridWidth || newGridPos.y < 0 || newGridPos.y >= gridHeight) continue;

                        var newHeat = Grid[newGridPos.x, newGridPos.y].heat;
                        if (newHeat < minHeat)
                        {
                            minHeat = newHeat;
                            dir.Set(i, j);
                        }
                    }
                }
                if (Mathf.Approximately(minHeat, Grid[x, y].heat)) continue;

                Grid[x, y].direction = new Vector3(dir.x, 0, dir.y).normalized;
            }
        }
    }
}
