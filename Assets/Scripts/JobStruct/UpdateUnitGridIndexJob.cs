using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct UpdateUnitGridIndexJob : IJobParallelFor
{
    private readonly int2 dgSize, ogSize;
    private readonly float dcDiameter, ocDiameter;
    [ReadOnly] private NativeArray<float2> positions;
    private NativeArray<int> dgIndices;
    private NativeArray<int> ogIndices;

    public UpdateUnitGridIndexJob(
        int2 dgSize,
        int2 ogSize,
        float dcDiameter,
        float ocDiameter,
        NativeArray<float2> positions,
        NativeArray<int> dgIndices,
        NativeArray<int> ogIndices
    )
    {
        this.dgSize = dgSize;
        this.ogSize = ogSize;
        this.dcDiameter = dcDiameter;
        this.ocDiameter = ocDiameter;
        this.positions = positions;
        this.dgIndices = dgIndices;
        this.ogIndices = ogIndices;
    }

    private readonly int WorldToDGIndex(float2 worldPos)
    {
        int2 gridPos = new((int)math.floor(worldPos.x / dcDiameter), (int)math.floor(worldPos.y / dcDiameter));
        if (gridPos.x < 0 || gridPos.x >= dgSize.x || gridPos.y < 0 || gridPos.y >= dgSize.y) return -1;
        return gridPos.x * dgSize.y + gridPos.y;
    }

    private readonly int WorldToOGIndex(float2 worldPos)
    {
        int2 gridPos = new((int)math.floor(worldPos.x / ocDiameter), (int)math.floor(worldPos.y / ocDiameter));
        if (gridPos.x < 0 || gridPos.x >= ogSize.x || gridPos.y < 0 || gridPos.y >= ogSize.y) return -1;
        return gridPos.x * ogSize.y + gridPos.y;
    }

    public void Execute(int index)
    {
        dgIndices[index] = WorldToDGIndex(positions[index]);
        ogIndices[index] = WorldToOGIndex(positions[index]);
    }
}
