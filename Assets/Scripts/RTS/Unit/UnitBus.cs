using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

public class UnitBus : MonoBehaviour
{
    public static UnitBus instance;

    [SerializeField] private float moveSpeed = 16;
    [SerializeField] private GameObject orangeUnitPrefab;
    [SerializeField] private GameObject blueUnitPrefab;
    [SerializeField] private GameObject whiteUnitPrefab;

    private void UpdateUnitGridIndexBurst()
    {
        UpdateUnitGridIndexJob job = new(
            FF.dgSize,
            FF.ogSize,
            FF.dcDiameter,
            FF.ocDiameter,
            UnitRegister.instance.positions,
            UnitRegister.instance.dgIndices,
            UnitRegister.instance.ogIndices
        );
        job.Schedule(UnitRegister.instance.indexer + 1, 64).Complete();
    }

    private void UpdateCellToUnitBurst()
    {
        FF.cellToUnit.Clear();
        UpdateCellToUnitJob job = new(
            FF.ogSize,
            FF.ocRadius,
            FF.cellToUnit.AsParallelWriter(),
            UnitRegister.instance.positions,
            UnitRegister.instance.radii
        );
        job.Schedule(UnitRegister.instance.indexer + 1, 64).Complete();
    }

    private void UpdateUnitCurMaxSpeedBurst()
    {
        UpdateUnitCurMaxSpeed job = new(
            FF.costMap,
            UnitRegister.instance.dgIndices,
            UnitRegister.instance.speeds,
            UnitRegister.instance.curMaxSpeeds
        );
        job.Schedule(UnitRegister.instance.indexer + 1, 64).Complete();
    }

    private readonly List<(int unitCount, NativeArray<float2> dirMap, ulong dirMapID, float2 destination, float destRadius, float existTimer)> moveList = new();

    private void UpdateUnitDirAccBurst()
    {
        for (int i = 0; i < moveList.Count; i++)
        {
            if (moveList[i].existTimer > 120)
            {
                moveList[i].dirMap.Dispose();
                moveList.RemoveAtSwapBack(i);
                i--;
                continue;
            }

            var dirMap = moveList[i].dirMap;
            var dirMapID = moveList[i].dirMapID;
            var destination = moveList[i].destination;
            var destRadius = moveList[i].destRadius;
            var existTimer = moveList[i].existTimer;

            UpdateUnitDirAccJob job = new(
                dirMap,
                UnitRegister.instance.dirMapIndices,
                dirMapID,
                UnitRegister.instance.dgIndices,
                UnitRegister.instance.dirAccs,
                UnitRegister.instance.curMaxSpeeds,
                FF.dgSize,
                destination,
                destRadius,
                UnitRegister.instance.positions,
                UnitRegister.instance.arrived
            );
            job.Schedule(UnitRegister.instance.indexer + 1, 64).Complete();

            moveList[i] = (UnitRegister.instance.selectedList.Length, dirMap, dirMapID, destination, destRadius, existTimer + Time.deltaTime);
        }
    }

    private void UpdateUnitBoidsAccBurst()
    {
        UpdateUnitBoidsAccJob job = new(
            UnitRegister.instance.radii,
            UnitRegister.instance.positions,
            UnitRegister.instance.ogIndices,
            UnitRegister.instance.curMaxSpeeds,
            UnitRegister.instance.dirAccs,
            FF.ogSize,
            FF.ocRadius,
            FF.cellToUnit,
            UnitRegister.instance.boidsAccs,
            UnitRegister.instance.dirAccRatios
        );
        job.Schedule(UnitRegister.instance.indexer + 1, 64).Complete();
    }

    private void UpdateUnitVelocitiesBurst()
    {
        UpdateUnitVelocitiesJob job = new(
            UnitRegister.instance.radii,
            UnitRegister.instance.ogIndices,
            UnitRegister.instance.dirAccs,
            UnitRegister.instance.boidsAccs,
            UnitRegister.instance.dirAccRatios,
            UnitRegister.instance.curMaxSpeeds,
            UnitRegister.instance.positions,
            FF.cellToObstacle,
            ObstacleRegister.instance.obstacleRegistry,
            UnitRegister.instance.velocities,
            FF.ocRadius,
            FF.ogSize,
            Time.deltaTime
        );
        job.Schedule(UnitRegister.instance.indexer + 1, 64).Complete();
    }

    private void UpdateUnitPositionBurst()
    {
        UpdateUnitPositionJob job = new(
            UnitRegister.instance.radii,
            UnitRegister.instance.ogIndices,
            UnitRegister.instance.positions,
            UnitRegister.instance.velocities,
            FF.cellToObstacle,
            ObstacleRegister.instance.obstacleRegistry,
            FF.ocRadius,
            FF.ogSize,
            Time.deltaTime
        );
        job.Schedule(UnitRegister.instance.indexer + 1, 64).Complete();
    }

    private void UpdateUnitTransformBurst()
    {
        UpdateUnitTransformJob job = new(
            UnitRegister.instance.positions,
            UnitRegister.instance.lastPositions,
            UnitRegister.instance.rotations,
            UnitRegister.instance.curMaxSpeeds,
            Time.deltaTime
            );
        job.Schedule(UnitRegister.instance.unitTrans).Complete();
    }

    public void InstantiateUnit(GenerateCommand cmd)
    {
        GameObject unit = cmd.unitType switch
        {
            UnitType.OrangeSmall => orangeUnitPrefab,
            UnitType.BlueSmall => blueUnitPrefab,
            UnitType.White => whiteUnitPrefab,
            _ => null,
        };
        float radius = cmd.unitType switch
        {
            UnitType.OrangeSmall => 0.6f,
            UnitType.BlueSmall => 0.6f,
            UnitType.White => 2f,
            _ => 0,
        };

        for (int i = 0; i < cmd.count; i++)
        {
            Vector3 generationPos = new(cmd.pos.x, 0, cmd.pos.y);
            GameObject go = UnitPool.instance.Instantiate(unit, generationPos, Quaternion.identity);
            UnitAgent agent = go.GetComponent<UnitAgent>();
            if (!agent)
            {
                Debug.LogError("[UnitBus] unitAgent not found");
                return;
            }
            agent.id = UnitRegister.instance.Register(go.transform, radius, moveSpeed, cmd.pos);
        }
    }

    private readonly static float sqrt2 = math.sqrt(2);
    private ulong curDirMapID = 0;

    public void SetDestination(MoveCommand cmd)
    {
        if (UnitRegister.instance.indexer + 1 <= 0) return;
        int count = UnitRegister.instance.selectedList.Length;
        if (count <= 0) return;

        float2 destination = cmd.pos;
        int destIndex = FF.WorldToDGIndex(destination);

        AssignDirMapIndexJob job = new(
            UnitRegister.instance.selectedMap,
            UnitRegister.instance.dirMapIndices,
            UnitRegister.instance.arrived,
            curDirMapID
        );
        job.Schedule(UnitRegister.instance.indexer + 1, 64).Complete();

        var dirMap = FF.GenerateFlowFieldBurst(FF.GenerateHeatMapBurst(ref destIndex));
        destination = FF.directionGrid[destIndex].worldPos;
        float averageRadius = 0;
        for (int i = 0; i < count; i++)
        {
            int index = UnitRegister.instance.selectedList[i];
            averageRadius += UnitRegister.instance.radii[index];
        }
        averageRadius /= count;
        float destRadius = averageRadius * sqrt2 * math.ceil(math.sqrt(count));

        moveList.Add((count, dirMap, curDirMapID, destination, destRadius, 0));
        curDirMapID++;
    }

    public void Delete()
    {
        IndicatorBatchManager.instance.Clear();
        InstancedAniManager.instance.Clear();

        for (int i = UnitRegister.instance.selectedList.Length - 1; i >= 0; i--)
        {
            int index = UnitRegister.instance.selectedList[i];
            UnitPool.instance.Destroy(UnitRegister.instance.unitTrans[index].gameObject);
            UnitRegister.instance.Unregister(index);
        }
    }

    private void Awake()
    {
        if (instance != null) Destroy(instance.gameObject);
        instance = this;
    }

    private FlowField FF => GridController.instance.flowField;

    private void Update()
    {
        if (UnitRegister.instance.indexer + 1 <= 0) return;

        UpdateCellToUnitBurst();
        UpdateUnitGridIndexBurst();

        UpdateUnitCurMaxSpeedBurst();
        UpdateUnitDirAccBurst();
        UpdateUnitBoidsAccBurst();
        UpdateUnitVelocitiesBurst();
        UpdateUnitPositionBurst();

        UpdateUnitTransformBurst();
    }

    private void OnDestroy()
    {
        for (int i = 0; i < moveList.Count; i++)
        {
            moveList[i].dirMap.Dispose();
        }

        instance = null;
    }
}
