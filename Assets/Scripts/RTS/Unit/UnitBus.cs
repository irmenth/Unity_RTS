using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class UnitBus : MonoBehaviour
{
    [SerializeField] private GridController gc;

    private void UpdateUnitGridIndexBurst()
    {
        UpdateUnitGridIndexJob job = new(
            gc.flowField.dgSize,
            gc.flowField.ogSize,
            gc.flowField.dcDiameter,
            gc.flowField.ocDiameter,
            unitReg
            );
        job.Schedule(UnitRegister.instance.indexer + 1, 64).Complete();
    }

    private void UpdateCellToUnitBurst()
    {
        gc.flowField.cellToUnit.Clear();
        UpdateCellToUnitJob job = new(
            gc.flowField.ogSize,
            gc.flowField.ocRadius,
            gc.flowField.cellToUnit.AsParallelWriter(),
            unitReg
            );
        job.Schedule(UnitRegister.instance.indexer + 1, 64).Complete();
    }

    private void UpdateUnitPositionBurst()
    {
        NativeArray<UnitAgentData> unitRegRO = new(unitReg.Length, Allocator.TempJob);
        unitRegRO.CopyFrom(unitReg);

        UpdateUnitPositionJob job = new(
            gc.flowField.dgSize,
            gc.flowField.ogSize,
            gc.flowField.ocRadius,
            Time.deltaTime,
            destination,
            destRadius,
            unitReg,
            unitRegRO,
            gc.flowField.directionGrid,
            ObstacleRegister.instance.obstacleRegistry,
            gc.flowField.cellToObstacle,
            gc.flowField.cellToUnit
            );
        job.Schedule(UnitRegister.instance.indexer + 1, 64).Complete();

        unitRegRO.Dispose();
    }

    private float2 destination = new(float.PositiveInfinity, float.PositiveInfinity);
    private float destRadius;

    private void SetDestination(MoveToEvent evt)
    {
        if (UnitRegister.instance.indexer + 1 <= 0) return;

        destination = evt.destination;

        float averageRadius = 0;
        for (int i = 0; i <= UnitRegister.instance.indexer; i++)
        {
            averageRadius += unitReg[i].radius;
        }
        averageRadius /= UnitRegister.instance.indexer + 1;
        int row = Mathf.CeilToInt(Mathf.Sqrt(UnitRegister.instance.indexer + 1));
        destRadius = 0.8f * Mathf.Sqrt(2) * averageRadius * row;
    }

    private NativeArray<UnitAgentData> unitReg;

    private void Awake()
    {
        unitReg = UnitRegister.instance.unitRegistry;

        EventBus.Subscribe<MoveToEvent>(SetDestination);
    }

    private void Update()
    {
        if (UnitRegister.instance.indexer + 1 <= 0) return;

        UpdateCellToUnitBurst();
        UpdateUnitGridIndexBurst();
        UpdateUnitPositionBurst();
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<MoveToEvent>(SetDestination);
    }
}
