using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct UpdateArrivedJob : IJob
{
    private NativeArray<bool> arrived;
    [ReadOnly] private NativeArray<float2> positions;
    private readonly int regIndex;
    private readonly float2 destination;
    private readonly float destRadius;

    public UpdateArrivedJob(
        NativeArray<bool> arrived,
        NativeArray<float2> positions,
        int regIndex,
        float2 destination,
        float destRadius
        )
    {
        this.arrived = arrived;
        this.positions = positions;
        this.regIndex = regIndex;
        this.destination = destination;
        this.destRadius = destRadius;
    }

    public void Execute()
    {
        int arrivedCount = 0;
        for (int i = 0; i < regIndex + 1; i++)
        {
            if (math.lengthsq(positions[i] - destination) < destRadius * destRadius) arrivedCount++;
        }

        arrived[0] = arrivedCount / (regIndex + 1f) >= 0.8f;
    }
}
