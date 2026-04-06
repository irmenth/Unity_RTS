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
    public readonly int2 dgSize, ogSize;
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
    public FlowField(int2 dgSize, float dcRadius, int2 ogSize, float ocRadius)
    {
        this.dgSize = dgSize;
        this.dcRadius = dcRadius;
        dcDiameter = dcRadius * 2f;
        directionGrid = new(dgSize.x * dgSize.y, Allocator.Persistent);

        this.ogSize = ogSize;
        this.ocRadius = ocRadius;
        ocDiameter = ocRadius * 2f;
        obstacleGrid = new(ogSize.x * ogSize.y, Allocator.Persistent);

        cellToUnit = new((int)8e3f, Allocator.Persistent);
        cellToObstacle = new(4 * ogSize.x * ogSize.y, Allocator.Persistent);
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
        if (gridPos.x < 0 || gridPos.x >= dgSize.x || gridPos.y < 0 || gridPos.y >= dgSize.y) return -1;
        return gridPos.x * dgSize.y + gridPos.y;
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
        if (gridPos.x < 0 || gridPos.x >= ogSize.x || gridPos.y < 0 || gridPos.y >= ogSize.y) return -1;
        return gridPos.x * ogSize.y + gridPos.y;
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
        DirectionGridGenerationJob dirGridGenJob = new(dgSize.y, dcRadius, directionGrid);
        dirGridGenJob.Schedule(dgSize.x * dgSize.y, 64).Complete();

        ObstacleGridGenerationJob obsGridGenJob = new(ogSize.y, ocRadius, obstacleGrid);
        obsGridGenJob.Schedule(ogSize.x * ogSize.y, 64).Complete();
    }

    private readonly Collider[] dgBoxHitBuffter = new Collider[10];

    public void GenerateCostField(LayerMask costLayerMask, int impassibleLayer, int roughLayer)
    {
        float subCellDiameter = dcDiameter / 3f;
        for (int x = 0; x < dgSize.x; x++)
        {
            for (int y = 0; y < dgSize.y; y++)
            {
                int index = x * dgSize.y + y;
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

    public float2 GenerateHeatMapBurst(int destinationGridIndex)
    {
        int size = dgSize.x * dgSize.y;

        NativeQueue<int> openList = new(Allocator.TempJob);
        NativeArray<byte> inOpenList = new(size, Allocator.TempJob);
        NativeArray<byte> closeList = new(size, Allocator.TempJob);
        NativeArray<int> destination = new(1, Allocator.TempJob);

        destination[0] = destinationGridIndex;
        HeatMapJob job = new(dgSize, destination, openList, inOpenList, closeList, directionGrid);
        job.Schedule().Complete();
        int destIndex = destination[0];

        openList.Dispose();
        inOpenList.Dispose();
        closeList.Dispose();
        destination.Dispose();

        return destIndex == destinationGridIndex ? new(-1, -1) : directionGrid[destIndex].worldPos;
    }

    public void GenerateFlowFieldBurst()
    {
        int size = dgSize.x * dgSize.y;

        NativeArray<float2> flowDir = new(size, Allocator.TempJob);

        FlowFieldJob job = new(dgSize, directionGrid, flowDir);
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
        for (int x = 0; x < ogSize.x; x++)
        {
            for (int y = 0; y < ogSize.y; y++)
            {
                int index = x * ogSize.y + y;
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
