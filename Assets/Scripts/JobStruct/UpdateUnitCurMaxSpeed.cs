using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct UpdateUnitCurMaxSpeed : IJobParallelFor
{
    [ReadOnly] private NativeArray<float> costMap;
    [ReadOnly] private NativeArray<int> dgIndices;
    [ReadOnly] private NativeArray<float> speeds;
    private NativeArray<float> curMaxSpeeds;

    public UpdateUnitCurMaxSpeed(
        NativeArray<float> costMap,
        NativeArray<int> dgIndices,
        NativeArray<float> speeds,
        NativeArray<float> curMaxSpeeds
    )
    {
        this.costMap = costMap;
        this.dgIndices = dgIndices;
        this.speeds = speeds;
        this.curMaxSpeeds = curMaxSpeeds;
    }

    public void Execute(int index)
    {
        curMaxSpeeds[index] = math.select(speeds[index] / costMap[dgIndices[index]], speeds[index], math.isinf(costMap[dgIndices[index]]));
    }
}
