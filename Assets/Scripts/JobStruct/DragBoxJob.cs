using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct DragBoxJob : IJobParallelFor
{
    private NativeArray<bool> enableMap;
    [ReadOnly] private NativeArray<float2> positions;
    private readonly float2 ld, ru;

    public DragBoxJob(
        NativeArray<bool> enableMap,
        NativeArray<float2> positions,
        float2 ld,
        float2 ru
    )
    {
        this.enableMap = enableMap;
        this.positions = positions;
        this.ld = ld;
        this.ru = ru;
    }

    public void Execute(int index)
    {
        enableMap[index] = false;
        if (positions[index].x >= ld.x && positions[index].x <= ru.x && positions[index].y >= ld.y && positions[index].y <= ru.y)
        {
            enableMap[index] = true;
        }
    }
}
