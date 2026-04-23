using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Jobs;

[BurstCompile]
public struct UpdateUnitTransformJob : IJobParallelForTransform
{
    private NativeArray<float2> positions;
    private NativeArray<float2> lastPositions;
    private NativeArray<quaternion> rotations;
    [ReadOnly] private NativeArray<float> curMaxSpeeds;
    private readonly float deltaTime;

    public UpdateUnitTransformJob(
        NativeArray<float2> positions,
        NativeArray<float2> lastPositions,
        NativeArray<quaternion> rotations,
        NativeArray<float> curMaxSpeeds,
        float deltaTime
    )
    {
        this.positions = positions;
        this.lastPositions = lastPositions;
        this.rotations = rotations;
        this.curMaxSpeeds = curMaxSpeeds;
        this.deltaTime = deltaTime;
    }

    public void Execute(int index, TransformAccess transform)
    {
        float2 posToLast = positions[index] - lastPositions[index];
        lastPositions[index] = math.select(lastPositions[index], positions[index], math.lengthsq(posToLast) > 0.1 * 0.8 * curMaxSpeeds[index]);
        quaternion desiredRot = quaternion.LookRotationSafe(new(posToLast.x, 0, posToLast.y), new(0, 1, 0));
        rotations[index] = math.slerp(rotations[index], desiredRot, 4f * deltaTime);

        transform.SetPositionAndRotation(new(positions[index].x, transform.position.y, positions[index].y), rotations[index]);
    }
}
