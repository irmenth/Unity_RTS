using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class UnitBus : MonoBehaviour
{
    [SerializeField] private GridController gc;

    private void UpdateUnitGridIndex()
    {
        for (int i = 0; i <= UnitRegister.instance.indexer; i++)
        {
            UnitAgentData agentData = unitReg[i];
            agentData.dgIndex = gc.flowField.WorldToDGIndex(agentData.position);
            agentData.ogIndex = gc.flowField.WorldToOGIndex(agentData.position);
            unitReg[i] = agentData;
        }
    }

    private void UpdateCellToUnit()
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
    private bool arrived = true;

    private void SetDestination(MoveToEvent evt)
    {
        destination = evt.destination;
        arrived = false;
    }

    private void DetectIsArrived()
    {
        if (arrived) return;

        for (int i = 0; i <= UnitRegister.instance.indexer; i++)
        {
            if (math.lengthsq(unitReg[i].position - destination) < 1f)
            {
                arrived = true;
                return;
            }
        }
    }

    private NativeArray<UnitAgentData> unitReg;

    private void Awake()
    {
        unitReg = UnitRegister.instance.unitRegistry;

        EventBus.Subscribe<MoveToEvent>(SetDestination);
    }

    private void Update()
    {
        DetectIsArrived();
        UpdateCellToUnit();
        UpdateUnitGridIndex();
        UpdateUnitPositionBurst();
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<MoveToEvent>(SetDestination);
    }
}
