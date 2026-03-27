using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct FlowFieldJob : IJobParallelFor
{
    public int width, height;
    [ReadOnly] public NativeArray<float> heatMap;
    public NativeArray<float2> flowDir;

    public FlowFieldJob(int width, int height, NativeArray<float> heatMap, NativeArray<float2> flowDir)
    {
        this.width = width;
        this.height = height;
        this.heatMap = heatMap;
        this.flowDir = flowDir;
    }

    public void Execute(int index)
    {
        flowDir[index] = float2.zero;

        if (math.isinf(heatMap[index]))
        {
            flowDir[index] = new float2(-1, -1);
            return;
        }

        int x = index / height, y = index % height;

        float minHeat = heatMap[index];
        float2 baseDir = float2.zero;
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                int nx = x + dx, ny = y + dy;
                if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;

                float newHeat = heatMap[nx * height + ny];
                if (newHeat < minHeat)
                {
                    minHeat = newHeat;
                    baseDir = new float2(dx, dy);
                }
            }
        }
        if (math.abs(minHeat - heatMap[index]) < 1e-6f) return;

        flowDir[index] = baseDir;
        if (baseDir.x == -1 && baseDir.y == -1)
            flowDir[index] = new float2(-0.71f, -0.71f);
        else if (baseDir.x == 1 && baseDir.y == -1)
            flowDir[index] = new float2(0.71f, -0.71f);
        else if (baseDir.x == 1 && baseDir.y == 1)
            flowDir[index] = new float2(0.71f, 0.71f);
        else if (baseDir.x == -1 && baseDir.y == 1)
            flowDir[index] = new float2(-0.71f, 0.71f);
    }
}
