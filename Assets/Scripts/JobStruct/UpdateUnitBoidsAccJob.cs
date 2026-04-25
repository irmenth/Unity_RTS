using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct UpdateUnitBoidsAccJob : IJobParallelFor
{
    [ReadOnly] private NativeArray<float> radii;
    [ReadOnly] private NativeArray<float2> positions;
    [ReadOnly] private NativeArray<int> ogIndices;
    [ReadOnly] private NativeArray<float> curMaxSpeeds;
    [ReadOnly] private NativeArray<float2> dirAccs;
    private readonly int2 ogSize;
    private readonly float ocRadius;
    [ReadOnly] private NativeParallelMultiHashMap<int, int> cellToUnit;
    private NativeArray<float2> boidsAccs;
    private NativeArray<float> dirAccRatios;

    public UpdateUnitBoidsAccJob(
        NativeArray<float> radii,
        NativeArray<float2> positions,
        NativeArray<int> ogIndices,
        NativeArray<float> curMaxSpeeds,
        NativeArray<float2> dirAccs,
        int2 ogSize,
        float ocRadius,
        NativeParallelMultiHashMap<int, int> cellToUnit,
        NativeArray<float2> boidsAccs,
        NativeArray<float> dirAccRatios
    )
    {
        this.radii = radii;
        this.positions = positions;
        this.ogIndices = ogIndices;
        this.curMaxSpeeds = curMaxSpeeds;
        this.dirAccs = dirAccs;
        this.ogSize = ogSize;
        this.ocRadius = ocRadius;
        this.cellToUnit = cellToUnit;
        this.boidsAccs = boidsAccs;
        this.dirAccRatios = dirAccRatios;
    }

    public void Execute(int index)
    {
        Random rand = new((uint)index + 1);
        int steps = (int)math.ceil(radii[index] / ocRadius);
        // unit 向内检测的步长，超过此步长的内部区域将跳过检测，数值应 >= 2
        int innerSteps = 2;
        int2 ogPos = new(ogIndices[index] / ogSize.y, ogIndices[index] % ogSize.y);

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

                if (cellToUnit.TryGetFirstValue(newIndex, out int otherIndex, out NativeParallelMultiHashMapIterator<int> it))
                {
                    do
                    {
                        if (otherIndex == index) continue;

                        float2 diff = positions[otherIndex] - positions[index];
                        float totalRadius = radii[index] + radii[otherIndex];
                        float maxDist = totalRadius + 0.2f * math.min(radii[index], radii[otherIndex]);
                        float overLapDist = totalRadius;
                        if (math.lengthsq(diff) < maxDist * maxDist)
                        {
                            float dist = math.length(diff);
                            float2 sepDir = dist < 1e-3f ? rand.NextFloat2Direction() : diff / dist;
                            float linearFactor = 1 - math.saturate((dist - totalRadius) / (maxDist - totalRadius));
                            float overLap = 8 * curMaxSpeeds[index] * (1 - math.saturate(dist / overLapDist));
                            float radiusFactor = math.clamp(radii[otherIndex] / radii[index], 0.1f, 20f);
                            float mag = (4 * curMaxSpeeds[index] * linearFactor + overLap) * radiusFactor;
                            sepAccSum += mag * sepDir;
                            count++;

                            if (math.lengthsq(dirAccs[index]) > 1e-9f && math.dot(dirAccs[index], sepDir) > 0)
                            {
                                alignFactor += linearFactor;
                                alignCount++;
                            }
                        }
                    } while (cellToUnit.TryGetNextValue(out otherIndex, ref it));
                }
            }
        }

        boidsAccs[index] = math.select(float2.zero, -sepAccSum, count > 0);
        dirAccRatios[index] = math.select(1, 1 - math.saturate(alignFactor / alignCount), alignCount > 0);
    }
}
