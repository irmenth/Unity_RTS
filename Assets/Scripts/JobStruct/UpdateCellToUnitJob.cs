using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct UpdateCellToUnitJob : IJobParallelFor
{
    private readonly int2 ogSize;
    private readonly float ocDiameter;
    private NativeParallelMultiHashMap<int, int>.ParallelWriter cellToUnit;
    private NativeArray<float2> positions;
    private NativeArray<float> radii;

    public UpdateCellToUnitJob(
        int2 ogSize,
        float ocRadius,
        NativeParallelMultiHashMap<int, int>.ParallelWriter cellToUnit,
        NativeArray<float2> positions,
        NativeArray<float> radii
        )
    {
        this.ogSize = ogSize;
        ocDiameter = ocRadius * 2;
        this.cellToUnit = cellToUnit;
        this.positions = positions;
        this.radii = radii;
    }

    private readonly int2 WorldToOGPos(float2 worldPos)
    {
        int2 gridPos = new((int)math.floor(worldPos.x / ocDiameter), (int)math.floor(worldPos.y / ocDiameter));
        gridPos = new(math.clamp(gridPos.x, 0, ogSize.x - 1), math.clamp(gridPos.y, 0, ogSize.y - 1));
        return gridPos;
    }

    public void Execute(int index)
    {
        float2 min = new(positions[index].x - radii[index], positions[index].y - radii[index]);
        float2 max = new(positions[index].x + radii[index], positions[index].y + radii[index]);
        int2 minOg = WorldToOGPos(min);
        int2 maxOg = WorldToOGPos(max);
        for (int x = minOg.x; x <= maxOg.x; x++)
        {
            for (int y = minOg.y; y <= maxOg.y; y++)
            {
                int i = x * ogSize.y + y;
                cellToUnit.Add(i, index);
            }
        }
    }
}
