using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct UnitBusJob : IJobParallelFor
{
    private readonly int dgHeight, dgWidth, ogWidht, ogHeight;
    private readonly float ocRadius;
    private readonly float deltaTime;
    private readonly bool arrived;
    private NativeArray<UnitAgentData> unitReg;
    [ReadOnly] private NativeArray<DirectionCell> directionGrid;
    [ReadOnly] private NativeArray<ObstacleData> obstacleReg;
    [ReadOnly] private NativeParallelMultiHashMap<int, int> cellToObstacle;

    public UnitBusJob(int dgWidth, int dgHeight, int ogWidht, int ogHeight, float ocRadius, float deltaTime, bool arrived, NativeArray<UnitAgentData> unitReg, NativeArray<DirectionCell> directionGrid, NativeArray<ObstacleData> obstacleReg, NativeParallelMultiHashMap<int, int> cellToObstacle)
    {
        this.dgWidth = dgWidth;
        this.dgHeight = dgHeight;
        this.ogWidht = ogWidht;
        this.ogHeight = ogHeight;
        this.ocRadius = ocRadius;
        this.deltaTime = deltaTime;
        this.arrived = arrived;
        this.unitReg = unitReg;
        this.directionGrid = directionGrid;
        this.obstacleReg = obstacleReg;
        this.cellToObstacle = cellToObstacle;
    }

    public void Execute(int index)
    {
        UnitAgentData agentData = unitReg[index];
        int steps = (int)math.ceil(agentData.radius / ocRadius);
        int2 ogPos = new(agentData.ogIndex / ogHeight, agentData.ogIndex % ogHeight);

        // FlowFieldVelocity();
        if (!arrived)
        {
            float cost = directionGrid[agentData.dgIndex].cost;
            float curMaxSpeed = math.isinf(cost) ? agentData.speed : agentData.speed / cost;
            float2 dir = directionGrid[agentData.dgIndex].direction;
            if (!UsefulUtils.Approximately(dir, new float2(-1, -1)) && !UsefulUtils.Approximately(dir, float2.zero))
            {
                agentData.velocity += 8f * curMaxSpeed * deltaTime * dir;
                agentData.velocity = curMaxSpeed * math.normalize(agentData.velocity);
            }
        }

        // // BoidsVelocityCorrection();
        // Random rand = new((uint)index);
        // float2 offsetSum = new(0, 0);
        // int count = 0;
        // for (int dx = -1; dx <= 1; dx++)
        // {
        //     for (int dy = -1; dy <= 1; dy++)
        //     {
        //         int2 newPos = new(agentData.unitOgPos.x + dx, agentData.unitOgPos.y + dy);
        //         if (newPos.x < 0 || newPos.x >= ogWidht || newPos.y < 0 || newPos.y >= ogHeight) continue;
        //         int newIndex = newPos.x * ogHeight + newPos.y;

        //         foreach (var other in unitDataList[newIndex])
        //         {
        //             if (other == agentData) continue;

        //             float2 diff = other.position - agentData.position;
        //             if (math.all(diff == float2.zero))
        //                 diff = new float2(rand.NextFloat(-1f, 1f), rand.NextFloat(-1f, 1f));
        //             if (math.lengthsq(diff) < math.pow(agentData.unitRadius + other.unitRadius, 2))
        //             {
        //                 offsetSum += (agentData.unitRadius + other.unitRadius) * math.normalize(diff) - diff;
        //                 count++;
        //             }
        //         }
        //     }
        // }
        // if (count > 0)
        // {
        //     bool offsetSumMask = math.lengthsq(agentData.velocity) > math.lengthsq(offsetSum);
        //     agentData.velocity -= offsetSumMask ? math.length(agentData.velocity) * math.normalize(offsetSum) : offsetSum;
        //     agentData.velocity = UsefulUtils.ClampMagnitude(agentData.velocity, curMaxSpeed);
        // }

        // KenimaticVelocityCorrection()
        for (int dx = -steps; dx <= steps; dx++)
        {
            for (int dy = -steps; dy <= steps; dy++)
            {
                int2 newPos = new(ogPos.x + dx, ogPos.y + dy);
                if (newPos.x < 0 || newPos.x >= ogWidht || newPos.y < 0 || newPos.y >= ogHeight) continue;
                int newIndex = newPos.x * ogHeight + newPos.y;

                if (cellToObstacle.TryGetFirstValue(newIndex, out int id, out NativeParallelMultiHashMapIterator<int> it))
                {
                    do
                    {
                        ObstacleData data = obstacleReg[id];
                        agentData.velocity = (int)data.type;
                        switch (data.type)
                        {
                            case ObstacleType.Circle:
                                // if (UsefulUtils.HasCollideWithCircleObstacle(data.circle, agentData.position, agentData.radius, out float2 negImpactDir))
                                //     agentData.velocity = UsefulUtils.ProjectOnLine(agentData.velocity, negImpactDir);
                                break;
                            case ObstacleType.Rectangle:
                                // if (UsefulUtils.HasCollideWithRectObstacle(data.rect, agentData.position, agentData.radius, out negImpactDir))
                                //     agentData.velocity = UsefulUtils.ProjectOnLine(agentData.velocity, negImpactDir);
                                break;
                        }
                    } while (cellToObstacle.TryGetNextValue(out id, ref it));
                }
            }
        }

        // Damping()
        if (math.lengthsq(agentData.velocity) > 1e-12f)
        {
            agentData.velocity *= math.exp(-8f * deltaTime);
            agentData.position += deltaTime * agentData.velocity;
        }
        else
        {
            agentData.velocity = float2.zero;
        }

        // KenimaticPositionCorrection()
        for (int dx = -steps; dx <= steps; dx++)
        {
            for (int dy = -steps; dy <= steps; dy++)
            {
                int2 newPos = new(ogPos.x + dx, ogPos.y + dy);
                if (newPos.x < 0 || newPos.x >= ogWidht || newPos.y < 0 || newPos.y >= ogHeight) continue;
                int newIndex = newPos.x * ogHeight + newPos.y;

                if (cellToObstacle.TryGetFirstValue(newIndex, out int id, out NativeParallelMultiHashMapIterator<int> it))
                {
                    do
                    {
                        ObstacleData data = obstacleReg[id];
                        switch (data.type)
                        {
                            case ObstacleType.Circle:
                                agentData.position = UsefulUtils.IfIntersectWithCircleObstacle(data.circle, agentData.position, agentData.radius);
                                break;
                            case ObstacleType.Rectangle:
                                agentData.position = UsefulUtils.IfIntersectWithRectObstacle(data.rect, agentData.position, agentData.radius);
                                break;
                        }
                    } while (cellToObstacle.TryGetNextValue(out id, ref it));
                }
            }
        }

        unitReg[index] = agentData;
    }
}
