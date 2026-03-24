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
    private readonly Dictionary<Collider, ObstacleCollider> obstacleColliders = new();

    public void GenerateCostField(LayerMask costLayerMask, int impassibleLayer, int roughLayer)
    {
        var subCellDiameter = cellDiameter / 3f;
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                int hitCount = Physics.OverlapBoxNonAlloc(Grid[x, y].WorldPos, Vector3.one * cellRadius, lapBoxHitBuffter, Quaternion.identity, costLayerMask);

                bool hasRecordRough = false, hasRecordImpassible = false;
                for (int i = 0; i < hitCount; i++)
                {
                    if (lapBoxHitBuffter[i].gameObject.layer == impassibleLayer)
                    {
                        if (!obstacleColliders.TryGetValue(lapBoxHitBuffter[i], out var col))
                        {
                            col = lapBoxHitBuffter[i].GetComponent<ObstacleCollider>();
                            obstacleColliders.Add(lapBoxHitBuffter[i], col);
                        }
                        if (!Grid[x, y].obstacleList.Contains(col.obstacle))
                            Grid[x, y].obstacleList.Add(col.obstacle);
                    }

                    if (float.IsInfinity(Grid[x, y].cost)) continue;

                    if (!hasRecordImpassible && lapBoxHitBuffter[i].gameObject.layer == impassibleLayer)
                    {
                        var subCellHitCount = 0;
                        for (int m = -1; m <= 1; m++)
                        {
                            for (int n = -1; n <= 1; n++)
                            {
                                if (subCellHitCount >= 4) break;

                                var curSubCellPos = Grid[x, y].WorldPos + new Vector3(m * subCellDiameter, -10, n * subCellDiameter);
                                if (Physics.Raycast(curSubCellPos, Vector3.up, out var hit, 100f, 1 << impassibleLayer))
                                    subCellHitCount++;
                            }
                        }

                        if (subCellHitCount >= 4)
                        {
                            Grid[x, y].cost += float.PositiveInfinity;
                            hasRecordImpassible = true;
                        }
                    }
                    else if (!hasRecordRough && lapBoxHitBuffter[i].gameObject.layer == roughLayer)
                    {
                        var subCellHitCount = 0;
                        for (int m = -1; m <= 1; m++)
                        {
                            for (int n = -1; n <= 1; n++)
                            {
                                if (subCellHitCount >= 4) break;

                                var curSubCellPos = Grid[x, y].WorldPos + new Vector3(m * subCellDiameter, -10, n * subCellDiameter);
                                if (Physics.Raycast(curSubCellPos, Vector3.up, out var hit, 100f, 1 << roughLayer))
                                    subCellHitCount++;
                            }
                        }

                        if (subCellHitCount >= 4)
                        {
                            Grid[x, y].cost += 1f;
                            hasRecordRough = true;
                        }
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
    /// <summary>
    /// </summary>
    /// <param name="worldPos"></param>
    /// <returns>
    /// (-1, -1) if out of range
    /// </returns>
    public Vector2Int WorldToGridPos(Vector2 worldPos)
    {
        return WorldToGridPos(UsefulUtils.V2ToV3(worldPos));
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
                Grid[x, y].direction = Vector2.zero;
            }
        }

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (float.IsInfinity(Grid[x, y].cost))
                {
                    Grid[x, y].direction = -Vector2.one;
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

                Grid[x, y].direction = dir;
                if (dir.x == -1 && dir.y == -1)
                    Grid[x, y].direction = new Vector2(-0.71f, -0.71f);
                else if (dir.x == 1 && dir.y == 1)
                    Grid[x, y].direction = new Vector2(0.71f, 0.71f);
                else if (dir.x == -1 && dir.y == 1)
                    Grid[x, y].direction = new Vector2(-0.71f, 0.71f);
                else if (dir.x == 1 && dir.y == -1)
                    Grid[x, y].direction = new Vector2(0.71f, -0.71f);
            }
        }
    }
}
