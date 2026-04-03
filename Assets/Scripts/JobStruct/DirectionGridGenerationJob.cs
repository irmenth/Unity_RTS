using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct DirectionGridGenerationJob : IJobParallelFor
{
    private readonly int height;
    private readonly float radius, diameter;
    private NativeArray<DirectionCell> directionGrid;

    public DirectionGridGenerationJob(int height, float radius, NativeArray<DirectionCell> directionGrid)
    {
        this.height = height;
        this.radius = radius;
        diameter = radius * 2;
        this.directionGrid = directionGrid;
    }

    public void Execute(int index)
    {
        int x = index / height;
        int y = index % height;

        directionGrid[index] = new DirectionCell(index, new float2(x, y) * diameter + new float2(radius));
    }
}
