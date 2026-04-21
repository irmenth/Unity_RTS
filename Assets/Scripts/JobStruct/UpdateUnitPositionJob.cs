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
        Random rand = new((uint)index + 1);

        int steps = (int)math.ceil(agentData.radius / ocRadius);
        // unit 向内检测的步长，超过此步长的内部区域将跳过检测，数值应 >= 2
        int innerSteps = 2;

        int2 ogPos = new(agentData.ogIndex / ogSize.y, agentData.ogIndex % ogSize.y);
        int2 dgPos = new(agentData.dgIndex / dgSize.y, agentData.dgIndex % dgSize.y);
        float2 baseDir = directionGrid[agentData.dgIndex].direction;
        bool baseInf = math.isinf(baseDir.x) && math.isinf(baseDir.y);

        float cost = directionGrid[agentData.dgIndex].cost;
        agentData.curMaxSpeed = math.select(agentData.speed / cost, agentData.speed, math.isinf(cost));

        // FlowFieldAcceleration()
        if (!arrived)
        {
            if (baseInf)
            {
                int step = 1;
                while (step < math.max(dgSize.x, dgSize.y))
                {
                    bool canBreak = false;
                    for (int dx = -step; dx <= step; dx++)
                    {
                        for (int dy = -step; dy <= step; dy++)
                        {
                            if (dx != -step && dx != step && dy != -step && dy != step) continue;

                            int2 newPos = new(dgPos.x + dx, dgPos.y + dy);
                            if (newPos.x < 0 || newPos.x >= dgSize.x || newPos.y < 0 || newPos.y >= dgSize.y) continue;
                            int newIndex = newPos.x * dgSize.y + newPos.y;

                            float2 newDir = directionGrid[newIndex].direction;
                            if (math.isfinite(newDir.x) && math.isfinite(newDir.y))
                            {
                                baseDir = newDir;
                                canBreak = true;
                            }
                        }
                        if (canBreak) break;
                    }
                    if (canBreak) break;
                    step++;
                }
            }
        }

        // BoidsAcceleration()
        float2 sepAccSum = float2.zero;
        int count = 0;
        float alignFactor = 0;
        int alignCount = 0;
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
                        UnitAgentData otherData = unitRegRO[id];

                        float2 diff = otherData.position - agentData.position;
                        float totalRadius = agentData.radius + otherData.radius;
                        float maxDist = totalRadius + 0.2f * math.min(agentData.radius, otherData.radius);
                        float overLapDist = totalRadius;
                        if (math.lengthsq(diff) < maxDist * maxDist)
                        {
                            float dist = math.length(diff);
                            float2 sepDir = dist < 1e-3f ? rand.NextFloat2Direction() : diff / dist;
                            float linearFactor = 1 - math.saturate((dist - totalRadius) / (maxDist - totalRadius));
                            float overLap = 64 * agentData.curMaxSpeed * (1 - math.saturate(dist / overLapDist));
                            float radiusFactor = math.clamp(otherData.radius / agentData.radius, 0.1f, 20f);
                            float mag = (32 * agentData.curMaxSpeed * linearFactor + overLap) * radiusFactor;
                            sepAccSum += mag * sepDir;
                            count++;

                            if (math.dot(baseDir, sepDir) > 0)
                            {
                                alignFactor += linearFactor;
                                alignCount++;
                            }
                        }
                    } while (cellToUnit.TryGetNextValue(out id, ref it));
                }
            }
        }

        // ApplyAcceleration()
        alignFactor = 1 - math.saturate(math.select(0, alignFactor / alignCount, alignCount > 0));
        float2 totalAcc = float2.zero;
        if (!arrived)
        {
            totalAcc += alignFactor * 4 * agentData.curMaxSpeed * baseDir;
        }
        totalAcc += -sepAccSum;
        totalAcc = UsefulUtils.ClampMagnitude(totalAcc, 2 * agentData.curMaxSpeed);
        agentData.velocity += deltaTime * totalAcc;
        agentData.velocity = UsefulUtils.ClampMagnitude(agentData.velocity, agentData.curMaxSpeed);

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

        // Damping() && ApplyVelocity()
        if (!UsefulUtils.Approximately(agentData.velocity, float2.zero))
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
