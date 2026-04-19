using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct UpdateArrivedJob : IJob
{
    private NativeArray<bool> arrived;
    [ReadOnly] NativeArray<UnitAgentData> unitReg;
    private readonly int regIndex;
    private readonly float2 destination;
    private readonly float destRadius;

    public UpdateArrivedJob(
        NativeArray<bool> arrived,
        NativeArray<UnitAgentData> unitReg,
        int regIndex,
        float2 destination,
        float destRadius
        )
    {
        this.arrived = arrived;
        this.unitReg = unitReg;
        this.regIndex = regIndex;
        this.destination = destination;
        this.destRadius = destRadius;
    }

    public void Execute()
    {
        int arrivedCount = 0;
        for (int i = 0; i < regIndex + 1; i++)
        {
            float2 pos = unitReg[i].position;
            if (math.lengthsq(pos - destination) < destRadius * destRadius) arrivedCount++;
        }

        arrived[0] = arrivedCount / (regIndex + 1f) >= 0.8f;
    }
}
