using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class FlowField
{
    public NativeArray<DirectionCell> directionGrid;
    public NativeArray<ObstacleCell> obstacleGrid;
    public NativeParallelMultiHashMap<int, int> cellToUnit;
    public NativeParallelMultiHashMap<int, int> cellToObstacle;
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
        directionGrid = new(dgWidth * dgHeight, Allocator.Persistent);

        this.ogWidth = ogWidth;
        this.ogHeight = ogHeight;
        this.ocRadius = ocRadius;
        ocDiameter = ocRadius * 2f;
        obstacleGrid = new(ogWidth * ogHeight, Allocator.Persistent);

        cellToUnit = new(4 * ogWidth * ogHeight, Allocator.Persistent);
        cellToObstacle = new(ogWidth * ogHeight, Allocator.Persistent);
    }

    public void Dispose()
    {
        directionGrid.Dispose();
        obstacleGrid.Dispose();
        cellToUnit.Dispose();
        cellToObstacle.Dispose();
    }

    /// <summary>
    /// </summary>
    /// <param name="worldPos"></param>
    /// <returns>
    /// -1 * Vector2Int.one if out of range
    /// </returns>
    public int WorldToDGIndex(float2 worldPos)
    {
        int2 gridPos = new((int)math.floor(worldPos.x / dcDiameter), (int)math.floor(worldPos.y / dcDiameter));
        if (gridPos.x < 0 || gridPos.x >= dgWidth || gridPos.y < 0 || gridPos.y >= dgHeight) return -1;
        return gridPos.x * dgHeight + gridPos.y;
    }

    /// <summary>
    /// </summary>
    /// <param name="worldPos"></param>
    /// <returns>
    /// -1 * Vector2Int.one if out of range
    /// </returns>
    public int WorldToOGIndex(float2 worldPos)
    {
        int2 gridPos = new((int)math.floor(worldPos.x / ocDiameter), (int)math.floor(worldPos.y / ocDiameter));
        if (gridPos.x < 0 || gridPos.x >= ogWidth || gridPos.y < 0 || gridPos.y >= ogHeight) return -1;
        return gridPos.x * ogHeight + gridPos.y;
    }

    private void ChangeCost(int index, float cost)
    {
        DirectionCell cell = directionGrid[index];
        cell.cost = cost;
        directionGrid[index] = cell;
    }

    private void ChangeDirection(int index, float2 direction)
    {
        DirectionCell cell = directionGrid[index];
        cell.direction = direction;
        directionGrid[index] = cell;
    }

    public void GenerateGridBurst()
    {
        DirectionGridGenerationJob dirGridGenJob = new(dgHeight, dcRadius, directionGrid);
        dirGridGenJob.Schedule(dgWidth * dgHeight, 64).Complete();

        ObstacleGridGenerationJob obsGridGenJob = new(ogHeight, ocRadius, obstacleGrid);
        obsGridGenJob.Schedule(ogWidth * ogHeight, 64).Complete();
    }

    private readonly Collider[] dgBoxHitBuffter = new Collider[10];

    public void GenerateCostField(LayerMask costLayerMask, int impassibleLayer, int roughLayer)
    {
        float subCellDiameter = dcDiameter / 3f;
        for (int x = 0; x < dgWidth; x++)
        {
            for (int y = 0; y < dgHeight; y++)
            {
                int index = x * dgHeight + y;
                Vector3 detectPos = new(directionGrid[index].worldPos.x, -10, directionGrid[index].worldPos.y);
                int hitCount = Physics.OverlapBoxNonAlloc(detectPos, new Vector3(dcRadius, 20, dcRadius), dgBoxHitBuffter, Quaternion.identity, costLayerMask);

                bool hasRecordRough = false, hasRecordImpassible = false;
                for (int i = 0; i < hitCount; i++)
                {
                    if (float.IsInfinity(directionGrid[index].cost)) continue;

                    if (!hasRecordImpassible && dgBoxHitBuffter[i].gameObject.layer == impassibleLayer)
                    {
                        int subCellHitCount = 0;
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dy = -1; dy <= 1; dy++)
                            {
                                if (subCellHitCount >= 4) break;

                                Vector3 curSubCellPos = detectPos + new Vector3(dx * subCellDiameter, 0, dy * subCellDiameter);
                                if (Physics.Raycast(curSubCellPos, Vector3.up, out RaycastHit hit, 100f, 1 << impassibleLayer))
                                    subCellHitCount++;
                            }
                        }

                        if (subCellHitCount >= 4)
                        {
                            ChangeCost(index, float.PositiveInfinity);
                            hasRecordImpassible = true;
                            hasRecordRough = true;
                        }
                    }

                    if (!hasRecordRough && dgBoxHitBuffter[i].gameObject.layer == roughLayer)
                    {
                        int subCellHitCount = 0;
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dy = -1; dy <= 1; dy++)
                            {
                                if (subCellHitCount >= 4) break;

                                Vector3 curSubCellPos = detectPos + new Vector3(dx * subCellDiameter, 0, dy * subCellDiameter);
                                if (Physics.Raycast(curSubCellPos, Vector3.up, out RaycastHit hit, 100f, 1 << roughLayer))
                                    subCellHitCount++;
                            }
                        }

                        if (subCellHitCount >= 4)
                        {
                            ChangeCost(index, 2f);
                            hasRecordRough = true;
                        }
                    }
                }
            }
        }
    }

    public void GenerateHeatMapBurst(int destinationGridIndex)
    {
        int size = dgWidth * dgHeight;

        NativeQueue<int> openList = new(Allocator.TempJob);
        NativeArray<byte> inOpenList = new(size, Allocator.TempJob);
        NativeArray<byte> closeList = new(size, Allocator.TempJob);

        HeatMapJob job = new(dgWidth, dgHeight, destinationGridIndex, openList, inOpenList, closeList, directionGrid);
        job.Schedule().Complete();

        openList.Dispose();
        inOpenList.Dispose();
        closeList.Dispose();
    }

    public void GenerateFlowFieldBurst()
    {
        int size = dgWidth * dgHeight;

        NativeArray<float2> flowDir = new(size, Allocator.TempJob);

        FlowFieldJob job = new(dgWidth, dgHeight, directionGrid, flowDir);
        job.Schedule(size, 64).Complete();

        for (int i = 0; i < size; i++)
        {
            ChangeDirection(i, flowDir[i]);
        }

        flowDir.Dispose();
    }

    private readonly Collider[] ogBoxHitBuffer = new Collider[10];
    private readonly Dictionary<Collider, int> colliderBuffer = new();

    public void GenerateObstacleMap(int impassibleLayer)
    {
        for (int x = 0; x < ogWidth; x++)
        {
            for (int y = 0; y < ogHeight; y++)
            {
                int index = x * ogHeight + y;
                Vector3 detectPos = new(obstacleGrid[index].worldPos.x, -10, obstacleGrid[index].worldPos.y);
                int hitCount = Physics.OverlapBoxNonAlloc(detectPos, new Vector3(ocRadius, 20, ocRadius), ogBoxHitBuffer, Quaternion.identity, 1 << impassibleLayer);

                for (int i = 0; i < hitCount; i++)
                {
                    Collider collider = ogBoxHitBuffer[i];

                    if (!colliderBuffer.ContainsKey(collider))
                        colliderBuffer[collider] = collider.GetComponent<ObstacleAgent>().id;

                    int id = colliderBuffer[collider];

                    cellToObstacle.Add(index, id);
                }
            }
        }
    }
}
