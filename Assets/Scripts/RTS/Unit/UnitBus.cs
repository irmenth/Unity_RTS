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

    private void UpdateUnitPositionBurst()
    {
        UnitBusJob job = new(gc.flowField.dgWidth, gc.flowField.dgHeight, gc.flowField.ogWidth, gc.flowField.ogHeight, gc.flowField.ocRadius, Time.deltaTime, arrived, unitReg, gc.flowField.directionGrid, ObstacleRegister.instance.obstacleRegistry, gc.flowField.cellToObstacle);
        job.Schedule(UnitRegister.instance.indexer + 1, 64).Complete();
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
            if (UsefulUtils.Approximately(unitReg[i].position, destination, 2f))
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
        UpdateUnitGridIndex();
        UpdateUnitPositionBurst();
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<MoveToEvent>(SetDestination);
    }
}
