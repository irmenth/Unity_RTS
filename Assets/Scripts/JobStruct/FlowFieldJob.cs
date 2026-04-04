using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct FlowFieldJob : IJobParallelFor
{
    private readonly int2 size;
    [ReadOnly] private NativeArray<DirectionCell> directionGrid;
    private NativeArray<float2> flowDir;

    public FlowFieldJob(int2 size, NativeArray<DirectionCell> directionGrid, NativeArray<float2> flowDir)
    {
        this.size = size;
        this.directionGrid = directionGrid;
        this.flowDir = flowDir;
    }

    public void Execute(int index)
    {
        flowDir[index] = float2.zero;

        if (math.isinf(directionGrid[index].heat))
        {
            flowDir[index] = new float2(-1, -1);
            return;
        }

        int x = index / size.y, y = index % size.y;

        float minHeat = directionGrid[index].heat;
        float2 baseDir = float2.zero;
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                int nx = x + dx, ny = y + dy;
                if (nx < 0 || nx >= size.x || ny < 0 || ny >= size.y) continue;

                int newIndex = nx * size.y + ny;
                float newHeat = directionGrid[newIndex].heat;
                if (newHeat < minHeat)
                {
                    minHeat = newHeat;
                    baseDir = new float2(dx, dy);
                }
            }
        }
        if (math.abs(minHeat - directionGrid[index].heat) < 1e-6f) return;

        flowDir[index] = math.normalize(baseDir);
    }
}
