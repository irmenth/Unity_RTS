using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct UpdateUnitPositionJob : IJobParallelFor
{
    private readonly int2 dgSize, ogSize;
    private readonly float ocRadius;
    private readonly float deltaTime;
    private readonly bool arrived;
    private NativeArray<UnitAgentData> unitReg;
    [ReadOnly] private NativeArray<UnitAgentData> unitRegRO;
    [ReadOnly] private NativeArray<DirectionCell> directionGrid;
    [ReadOnly] private NativeArray<ObstacleData> obstacleReg;
    [ReadOnly] private NativeParallelMultiHashMap<int, int> cellToObstacle;
    [ReadOnly] private NativeParallelMultiHashMap<int, int> cellToUnit;

    public UpdateUnitPositionJob(
        int2 dgSize,
        int2 ogSize,
        float ocRadius,
        float deltaTime,
        bool arrived,
        NativeArray<UnitAgentData> unitReg,
        NativeArray<UnitAgentData> unitRegRO,
        NativeArray<DirectionCell> directionGrid,
        NativeArray<ObstacleData> obstacleReg,
        NativeParallelMultiHashMap<int, int> cellToObstacle,
        NativeParallelMultiHashMap<int, int> cellToUnit
        )
    {
        this.dgSize = dgSize;
        this.ogSize = ogSize;
        this.ocRadius = ocRadius;
        this.deltaTime = deltaTime;
        this.arrived = arrived;
        this.unitReg = unitReg;
        this.unitRegRO = unitRegRO;
        this.directionGrid = directionGrid;
        this.obstacleReg = obstacleReg;
        this.cellToObstacle = cellToObstacle;
        this.cellToUnit = cellToUnit;
    }

    public void Execute(int index)
    {
        UnitAgentData agentData = unitReg[index];
        int steps = (int)math.ceil(agentData.radius / ocRadius);
        // unit 向内检测的步长，超过此步长的内部区域将跳过检测，数值应 >= 2
        int innerSteps = 2;
        int2 ogPos = new(agentData.ogIndex / ogSize.y, agentData.ogIndex % ogSize.y);

        // FlowFieldVelocity();
        float cost = directionGrid[agentData.dgIndex].cost;
        float curMaxSpeed = math.isinf(cost) ? agentData.speed : agentData.speed / cost;
        if (!arrived)
        {
            float2 dir = directionGrid[agentData.dgIndex].direction;
            if (!UsefulUtils.Approximately(dir, new float2(-1, -1)) && !UsefulUtils.Approximately(dir, float2.zero))
            {
                agentData.velocity += 8f * curMaxSpeed * deltaTime * dir;
                agentData.velocity = curMaxSpeed * math.normalize(agentData.velocity);
            }
        }

        // BoidsVelocityCorrection();
        Random rand = new((uint)index + 1);
        float2 offsetSum = new(0, 0);
        int count = 0;
        for (int dx = -steps; dx <= steps; dx++)
        {
            for (int dy = -steps; dy <= steps; dy++)
            {
                if (steps >= innerSteps && dx >= -steps + innerSteps && dx <= steps - innerSteps && dy >= -steps + innerSteps && dy <= steps - innerSteps) continue;

                int2 newPos = new(ogPos.x + dx, ogPos.y + dy);
                if (newPos.x < 0 || newPos.x >= ogSize.x || newPos.y < 0 || newPos.y >= ogSize.y) continue;
                int newIndex = newPos.x * ogSize.y + newPos.y;

                if (cellToUnit.TryGetFirstValue(newIndex, out int id, out NativeParallelMultiHashMapIterator<int> it))
                {
                    do
                    {
                        if (id == agentData.id) continue;
                        UnitAgentData data = unitRegRO[id];

                        if (math.lengthsq(data.position - agentData.position) < math.pow(agentData.radius + data.radius, 2))
                        {
                            bool coincideMask = math.lengthsq(data.position - agentData.position) < 1e-12f;
                            float2 dir = coincideMask ? rand.NextFloat2Direction() : math.normalize(data.position - agentData.position);
                            float mag = (math.pow(agentData.radius + data.radius, 2) - math.lengthsq(data.position - agentData.position)) / math.pow(agentData.radius + data.radius, 2);
                            offsetSum += mag * dir;
                            count++;
                        }
                    } while (cellToUnit.TryGetNextValue(out id, ref it));
                }
            }
        }
        if (count > 0)
        {
            bool offsetSumMask = math.lengthsq(agentData.velocity) > math.lengthsq(offsetSum);
            agentData.velocity -= offsetSumMask ? math.length(agentData.velocity) * math.normalize(offsetSum) : offsetSum;
            agentData.velocity = UsefulUtils.ClampMagnitude(agentData.velocity, curMaxSpeed);
        }

        // KenimaticVelocityCorrection()
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
                        ObstacleData data = obstacleReg[id];
                        switch (data.type)
                        {
                            case ObstacleType.Circle:
                                if (UsefulUtils.HasCollideWithCircleObstacle(data.circle, agentData.position, agentData.radius, out float2 negImpactDir))
                                    agentData.velocity = UsefulUtils.ProjectOnLine(agentData.velocity, negImpactDir);
                                break;
                            case ObstacleType.Rectangle:
                                if (UsefulUtils.HasCollideWithRectObstacle(data.rect, agentData.position, agentData.radius, out negImpactDir))
                                    agentData.velocity = UsefulUtils.ProjectOnLine(agentData.velocity, negImpactDir);
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
                if (steps >= innerSteps && dx >= -steps + innerSteps && dx <= steps - innerSteps && dy >= -steps + innerSteps && dy <= steps - innerSteps) continue;

                int2 newPos = new(ogPos.x + dx, ogPos.y + dy);
                if (newPos.x < 0 || newPos.x >= ogSize.x || newPos.y < 0 || newPos.y >= ogSize.y) continue;
                int newIndex = newPos.x * ogSize.y + newPos.y;

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
