using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct ObstacleGridGenerationJob : IJobParallelFor
{
    private readonly int height;
    private readonly float radius, diameter;
    private NativeArray<ObstacleCell> obstacleGrid;

    public ObstacleGridGenerationJob(int height, float radius, NativeArray<ObstacleCell> obstacleGrid)
    {
        this.height = height;
        this.radius = radius;
        diameter = radius * 2;
        this.obstacleGrid = obstacleGrid;
    }

    public void Execute(int index)
    {
        int x = index / height;
        int y = index % height;

        obstacleGrid[index] = new ObstacleCell(index, new float2(x, y) * diameter + new float2(radius));
    }
}
