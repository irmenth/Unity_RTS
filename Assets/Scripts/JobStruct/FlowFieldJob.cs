using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct FlowFieldJob : IJobParallelFor
{
    private readonly int2 size;
    [ReadOnly] private NativeArray<float> heatMap;
    private NativeArray<float2> dirMap;

    public FlowFieldJob(int2 size, NativeArray<float> heatMap, NativeArray<float2> dirMap)
    {
        this.size = size;
        this.heatMap = heatMap;
        this.dirMap = dirMap;
    }

    public void Execute(int index)
    {
        dirMap[index] = float2.zero;

        if (math.isinf(heatMap[index]))
        {
            dirMap[index] = new(float.PositiveInfinity, float.PositiveInfinity);
            return;
        }

        int x = index / size.y, y = index % size.y;

        float minHeat = heatMap[index];
        float2 baseDir = float2.zero;
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                int nx = x + dx, ny = y + dy;
                if (nx < 0 || nx >= size.x || ny < 0 || ny >= size.y) continue;

                int newIndex = nx * size.y + ny;
                float newHeat = heatMap[newIndex];
                if (newHeat < minHeat)
                {
                    minHeat = newHeat;
                    baseDir = new(dx, dy);
                }
            }
        }
        if (math.abs(minHeat - heatMap[index]) < 1e-3f) return;

        dirMap[index] = math.normalize(baseDir);
    }
}
