using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class FlowField
{
    public DirCell[,] DirGrid { get; private set; }
    public ObstacleCell[,] ObstacleGrid { get; private set; }
    public readonly int dgWidth, dgHeight, ogWidth, ogHeight;
    public readonly float dcRadius, dcDiameter, ocRadius, ocDiameter;

    /// <summary>
    /// </summary>
    /// <param name="dgWidth">
    /// Width of the DirGrid
    /// </param>
    /// <param name="dgHeight">
    /// Height of the DirGrid
    /// </param>
    /// <param name="dcRadius">
    /// Radius of the DirCell
    /// </param>
    /// <param name="ogWidth">
    /// Width of the ObstacleGrid
    /// </param>
    /// <param name="ogHeight">
    /// Height of the ObstacleGrid
    /// </param>
    /// <param name="ocRadius">
    /// Radius of the ObstacleCell
    /// </param>
    public FlowField(int dgWidth, int dgHeight, float dcRadius, int ogWidth, int ogHeight, float ocRadius)
    {
        this.dgWidth = dgWidth;
        this.dgHeight = dgHeight;
        this.dcRadius = dcRadius;
        dcDiameter = dcRadius * 2f;
        DirGrid = new DirCell[dgWidth, dgHeight];

        this.ogWidth = ogWidth;
        this.ogHeight = ogHeight;
        this.ocRadius = ocRadius;
        ocDiameter = ocRadius * 2f;
        ObstacleGrid = new ObstacleCell[ogWidth, ogHeight];
    }

    /// <summary>
    /// </summary>
    /// <param name="worldPos"></param>
    /// <returns>
    /// -1 * Vector2Int.one if out of range
    /// </returns>
    public Vector2Int WorldToDirGridPos(Vector3 worldPos)
    {
        var gridPos = new Vector2Int(Mathf.FloorToInt(worldPos.x / dcDiameter), Mathf.FloorToInt(worldPos.z / dcDiameter));
        if (gridPos.x < 0 || gridPos.x >= dgWidth || gridPos.y < 0 || gridPos.y >= dgHeight) return new Vector2Int(-1, -1);
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
        return WorldToDirGridPos(UsefulUtils.V2ToV3(worldPos));
    }

    /// <summary>
    /// </summary>
    /// <param name="worldPos"></param>
    /// <returns>
    /// -1 * Vector2Int.one if out of range
    /// </returns>
    public Vector2Int WorldToObstacleGridPos(Vector3 worldPos)
    {
        var gridPos = new Vector2Int(Mathf.FloorToInt(worldPos.x / ocDiameter), Mathf.FloorToInt(worldPos.z / ocDiameter));
        if (gridPos.x < 0 || gridPos.x >= ogWidth || gridPos.y < 0 || gridPos.y >= ogHeight) return new Vector2Int(-1, -1);
        return gridPos;
    }
    /// <summary>
    /// </summary>
    /// <param name="worldPos"></param>
    /// <returns>
    /// -1 * Vector2Int.one if out of range
    /// </returns>
    public Vector2Int WorldToObstacleGridPos(Vector2 worldPos)
    {
        return WorldToObstacleGridPos(UsefulUtils.V2ToV3(worldPos));
    }

    public void GenerateGrid()
    {
        for (int x = 0; x < dgWidth; x++)
        {
            for (int y = 0; y < dgHeight; y++)
            {
                var worldPos = new Vector3(dcDiameter * x + dcRadius, 0, dcDiameter * y + dcRadius);
                var gridPos = new Vector2Int(x, y);
                DirGrid[x, y] = new DirCell(worldPos, gridPos);
            }
        }

        for (int x = 0; x < ogWidth; x++)
        {
            for (int y = 0; y < ogHeight; y++)
            {
                var worldPos = new Vector3(ocDiameter * x + ocRadius, 0, ocDiameter * y + ocRadius);
                var gridPos = new Vector2Int(x, y);
                ObstacleGrid[x, y] = new ObstacleCell(worldPos, gridPos);
            }
        }
    }

    private readonly Collider[] cfBoxHitBuffter = new Collider[10];

    public void GenerateCostField(LayerMask costLayerMask, int impassibleLayer, int roughLayer)
    {
        var subCellDiameter = dcDiameter / 3f;
        for (int x = 0; x < dgWidth; x++)
        {
            for (int y = 0; y < dgHeight; y++)
            {
                int hitCount = Physics.OverlapBoxNonAlloc(DirGrid[x, y].GetWorldPos(), Vector3.one * dcRadius, cfBoxHitBuffter, Quaternion.identity, costLayerMask);

                bool hasRecordRough = false, hasRecordImpassible = false;
                for (int i = 0; i < hitCount; i++)
                {
                    if (float.IsInfinity(DirGrid[x, y].cost)) continue;

                    if (!hasRecordImpassible && cfBoxHitBuffter[i].gameObject.layer == impassibleLayer)
                    {
                        var subCellHitCount = 0;
                        for (int m = -1; m <= 1; m++)
                        {
                            for (int n = -1; n <= 1; n++)
                            {
                                if (subCellHitCount >= 4) break;

                                var curSubCellPos = DirGrid[x, y].GetWorldPos() + new Vector3(m * subCellDiameter, -10, n * subCellDiameter);
                                if (Physics.Raycast(curSubCellPos, Vector3.up, out var hit, 100f, 1 << impassibleLayer))
                                    subCellHitCount++;
                            }
                        }

                        if (subCellHitCount >= 4)
                        {
                            DirGrid[x, y].cost += float.PositiveInfinity;
                            hasRecordImpassible = true;
                        }
                    }
                    else if (!hasRecordRough && cfBoxHitBuffter[i].gameObject.layer == roughLayer)
                    {
                        var subCellHitCount = 0;
                        for (int m = -1; m <= 1; m++)
                        {
                            for (int n = -1; n <= 1; n++)
                            {
                                if (subCellHitCount >= 4) break;

                                var curSubCellPos = DirGrid[x, y].GetWorldPos() + new Vector3(m * subCellDiameter, -10, n * subCellDiameter);
                                if (Physics.Raycast(curSubCellPos, Vector3.up, out var hit, 100f, 1 << roughLayer))
                                    subCellHitCount++;
                            }
                        }

                        if (subCellHitCount >= 4)
                        {
                            DirGrid[x, y].cost += 1f;
                            hasRecordRough = true;
                        }
                    }
                }
            }
        }
    }

    private readonly Queue<int> openList = new();

    public void GenerateHeatMap(Vector2Int destinationGridPos)
    {
        openList.Clear();
        var closedList = new bool[dgWidth * dgHeight];

        if (float.IsInfinity(DirGrid[destinationGridPos.x, destinationGridPos.y].cost)) return;

        for (int x = 0; x < dgWidth; x++)
        {
            for (int y = 0; y < dgHeight; y++)
            {
                DirGrid[x, y].heat = float.PositiveInfinity;
            }
        }
        DirGrid[destinationGridPos.x, destinationGridPos.y].heat = 0;
        openList.Enqueue(destinationGridPos.x * dgHeight + destinationGridPos.y);

        while (openList.Count > 0)
        {
            var curIndex = openList.Dequeue();
            var curGridPos = new Vector2Int(curIndex / dgHeight, curIndex % dgHeight);
            closedList[curIndex] = true;

            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if (i == 0 && j == 0) continue;
                    Vector2Int newGridPos = new(DirGrid[curGridPos.x, curGridPos.y].GetGridPos().x + i, DirGrid[curGridPos.x, curGridPos.y].GetGridPos().y + j);
                    var newIndex = newGridPos.x * dgHeight + newGridPos.y;
                    if (newGridPos.x < 0 || newGridPos.x >= dgWidth || newGridPos.y < 0 || newGridPos.y >= dgHeight) continue;
                    if (closedList[newGridPos.x * dgHeight + newGridPos.y]) continue;

                    if (float.IsInfinity(DirGrid[newGridPos.x, newGridPos.y].cost))
                    {
                        closedList[newIndex] = true;
                        continue;
                    }

                    var moveCost = DirGrid[newGridPos.x, newGridPos.y].cost;
                    if (i * j != 0)
                        moveCost *= 1.4f;

                    var newCost = DirGrid[curGridPos.x, curGridPos.y].heat + moveCost;
                    if (newCost < DirGrid[newGridPos.x, newGridPos.y].heat)
                    {
                        DirGrid[newGridPos.x, newGridPos.y].heat = newCost;
                        if (!openList.Contains(newIndex))
                            openList.Enqueue(newIndex);
                    }

                }
            }
        }
    }

    public void GenerateFlowFieldBurst()
    {
        int size = dgWidth * dgHeight;

        var heatMap = new NativeArray<float>(size, Allocator.TempJob);
        var flowDir = new NativeArray<float2>(size, Allocator.TempJob);

        for (int i = 0; i < dgWidth; i++)
        {
            for (int j = 0; j < dgHeight; j++)
            {
                heatMap[i * dgHeight + j] = DirGrid[i, j].heat;
            }
        }

        var job = new FlowFieldJob(dgWidth, dgHeight, heatMap, flowDir);
        job.Schedule(size, 64).Complete();

        for (int i = 0; i < dgWidth; i++)
        {
            for (int j = 0; j < dgHeight; j++)
            {
                DirGrid[i, j].direction = flowDir[i * dgHeight + j];
            }
        }

        heatMap.Dispose();
        flowDir.Dispose();
    }

    private readonly Collider[] omBoxHitBuffer = new Collider[10];
    private readonly Dictionary<Collider, Obstacles> colliderBuffer = new();

    public void GenerateObstacleMap(int impassibleLayer)
    {
        for (int x = 0; x < ogWidth; x++)
        {
            for (int y = 0; y < ogHeight; y++)
            {
                int hitCount = Physics.OverlapBoxNonAlloc(ObstacleGrid[x, y].GetWorldPos(), Vector3.one * ocRadius, omBoxHitBuffer, Quaternion.identity, 1 << impassibleLayer);

                for (int i = 0; i < hitCount; i++)
                {
                    var collider = omBoxHitBuffer[i];

                    if (!colliderBuffer.ContainsKey(collider))
                        colliderBuffer[collider] = collider.GetComponent<ObstacleCollider>().obstacle;

                    var obstacleCollider = colliderBuffer[collider];

                    if (!ObstacleGrid[x, y].obstacleList.Contains(obstacleCollider))
                        ObstacleGrid[x, y].obstacleList.Add(obstacleCollider);
                }
            }
        }
    }
}
