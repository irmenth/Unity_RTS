using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;


[BurstCompile]
public struct UpdateUnitVelocitiesJob : IJobParallelFor
{
    [ReadOnly] private NativeArray<float> radii;
    [ReadOnly] private NativeArray<int> ogIndices;
    [ReadOnly] private NativeArray<float2> dirAccs;
    [ReadOnly] private NativeArray<float2> boidsAccs;
    [ReadOnly] private NativeArray<float> dirAccRatios;
    [ReadOnly] private NativeArray<float> curMaxSpeeds;
    [ReadOnly] private NativeArray<float2> positions;
    [ReadOnly] private NativeParallelMultiHashMap<int, int> cellToObstacle;
    [ReadOnly] private NativeArray<ObstacleData> obstacleReg;
    private NativeArray<float2> velocities;
    private readonly float ocRadius;
    private readonly int2 ogSize;
    private readonly float deltaTime;

    public UpdateUnitVelocitiesJob(
        NativeArray<float> radii,
        NativeArray<int> ogIndices,
        NativeArray<float2> dirAccs,
        NativeArray<float2> boidsAccs,
        NativeArray<float> dirAccRatios,
        NativeArray<float> curMaxSpeeds,
        NativeArray<float2> positions,
        NativeParallelMultiHashMap<int, int> cellToObstacle,
        NativeArray<ObstacleData> obstacleReg,
        NativeArray<float2> velocities,
        float ocRadius,
        int2 ogSize,
        float deltaTime
    )
    {
        this.radii = radii;
        this.ogIndices = ogIndices;
        this.dirAccs = dirAccs;
        this.boidsAccs = boidsAccs;
        this.dirAccRatios = dirAccRatios;
        this.curMaxSpeeds = curMaxSpeeds;
        this.positions = positions;
        this.cellToObstacle = cellToObstacle;
        this.obstacleReg = obstacleReg;
        this.velocities = velocities;
        this.ocRadius = ocRadius;
        this.ogSize = ogSize;
        this.deltaTime = deltaTime;
    }

    public void Execute(int index)
    {
        velocities[index] += deltaTime * UsefulUtils.ClampMagnitude(boidsAccs[index] + dirAccRatios[index] * dirAccs[index], 2 * curMaxSpeeds[index]);
        velocities[index] = math.lengthsq(velocities[index]) < 1e-9f ? float2.zero : velocities[index] * math.exp(-8f * deltaTime);
        velocities[index] = UsefulUtils.ClampMagnitude(velocities[index], curMaxSpeeds[index]);

        int steps = (int)math.ceil(radii[index] / ocRadius);
        // unit 向内检测的步长，超过此步长的内部区域将跳过检测，数值应 >= 2
        int innerSteps = 2;
        int2 ogPos = new(ogIndices[index] / ogSize.y, ogIndices[index] % ogSize.y);

        for (int dx = -steps; dx <= steps; dx++)
        {
            for (int dy = -steps; dy <= steps; dy++)
            {
                if (steps >= innerSteps && dx >= -steps + innerSteps && dx <= steps - innerSteps && dy >= -steps + innerSteps && dy <= steps - innerSteps) continue;

                int2 newPos = new(ogPos.x + dx, ogPos.y + dy);
                if (newPos.x < 0 || newPos.x >= ogSize.x || newPos.y < 0 || newPos.y >= ogSize.y) continue;
                int newIndex = newPos.x * ogSize.y + newPos.y;

                if (cellToObstacle.TryGetFirstValue(newIndex, out int id, out NativeParallelMultiHashMapIterator<int> it))
                {
                    do
                    {
                        // ObstacleData data = obstacleReg[id];
                        // switch (data.type)
                        // {
                        //     case ObstacleType.Circle:
                        //         if (UsefulUtils.HasCollideWithCircleObstacle(data.circle, positions[index], radii[index], out float2 negImpactDir))
                        //             velocities[index] = UsefulUtils.ProjectOnLine(velocities[index], negImpactDir);
                        //         break;
                        //     case ObstacleType.Rectangle:
                        //         if (UsefulUtils.HasCollideWithRectObstacle(data.rect, positions[index], radii[index], out negImpactDir))
                        //             velocities[index] = UsefulUtils.ProjectOnLine(velocities[index], negImpactDir);
                        //         break;
                        // }
                    } while (cellToObstacle.TryGetNextValue(out id, ref it));
                }
            }
        }
    }
}
