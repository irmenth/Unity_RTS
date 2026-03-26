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
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                if (i == 0 && j == 0) continue;

                int nx = x + i, ny = y + j;
                if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;

                float newHeat = heatMap[nx * height + ny];
                if (newHeat < minHeat)
                {
                    minHeat = newHeat;
                    baseDir = new float2(i, j);
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
