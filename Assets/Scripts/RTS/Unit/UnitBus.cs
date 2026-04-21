using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class UnitBus : MonoBehaviour
{
    [SerializeField] private GridController gc;

    private void UpdateArrivedBurst()
    {
        if (math.abs(destRadius) < 1e-6f || arrived) return;

        NativeArray<bool> tarrived = new(1, Allocator.TempJob);

        UpdateArrivedJob job = new(
            tarrived,
            unitReg,
            UnitRegister.instance.indexer,
            destination,
            destRadius
            );
        job.Schedule().Complete();
        arrived = tarrived[0];

        tarrived.Dispose();
    }

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
            arrived,
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

    private float2 destination;
    private float destRadius;
    private bool arrived = true;
    private readonly static float sqrt2 = math.sqrt(2);

    private void SetDestination(MoveToEvent evt)
    {
        if (UnitRegister.instance.indexer + 1 <= 0) return;

        destination = evt.destination;
        arrived = false;

        float averageRadius = 0;
        for (int i = 0; i < UnitRegister.instance.indexer + 1; i++)
        {
            averageRadius += unitReg[i].radius;
        }
        averageRadius /= UnitRegister.instance.indexer + 1;
        destRadius = 0.8f * averageRadius * sqrt2 * math.ceil(math.sqrt(UnitRegister.instance.indexer + 1));
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

        UpdateArrivedBurst();
        UpdateCellToUnitBurst();
        UpdateUnitGridIndexBurst();
        UpdateUnitPositionBurst();
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<MoveToEvent>(SetDestination);
    }
}
