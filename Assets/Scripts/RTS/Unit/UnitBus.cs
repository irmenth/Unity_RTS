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

    private void UpdateArrivedBurst()
    {
        if (math.abs(destRadius) < 1e-6f || arrived) return;

        NativeArray<bool> tarrived = new(1, Allocator.TempJob);

        UpdateArrivedJob job = new(
            tarrived,
            UnitRegister.instance.positions,
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
            flowField.dgSize,
            flowField.ogSize,
            flowField.dcDiameter,
            flowField.ocDiameter,
            UnitRegister.instance.positions,
            UnitRegister.instance.dgIndices,
            UnitRegister.instance.ogIndices
            );
        job.Schedule(UnitRegister.instance.indexer + 1, 64).Complete();
    }

    private void UpdateCellToUnitBurst()
    {
        flowField.cellToUnit.Clear();
        UpdateCellToUnitJob job = new(
            flowField.ogSize,
            flowField.ocRadius,
            flowField.cellToUnit.AsParallelWriter(),
            UnitRegister.instance.positions,
            UnitRegister.instance.radii
            );
        job.Schedule(UnitRegister.instance.indexer + 1, 64).Complete();
    }

    private void UpdateUnitCurMaxSpeedBurst()
    {
        UpdateUnitCurMaxSpeed job = new(
            flowField.costMap,
            UnitRegister.instance.dgIndices,
            UnitRegister.instance.speeds,
            UnitRegister.instance.curMaxSpeeds
            );
        job.Schedule(UnitRegister.instance.indexer + 1, 64).Complete();
    }

    private void UpdateUnitDirAccBurst(NativeArray<float2> dirMap)
    {
        UpdateUnitDirAccJob job = new(
            dirMap,
            UnitRegister.instance.dgIndices,
            UnitRegister.instance.dirAccs,
            UnitRegister.instance.curMaxSpeeds,
            flowField.dgSize
            );
        job.Schedule(UnitRegister.instance.indexer + 1, 64).Complete();
    }

    private void UpdateUnitBoidsAccBurst()
    {
        UpdateUnitBoidsAccJob job = new(
            UnitRegister.instance.radii,
            UnitRegister.instance.positions,
            UnitRegister.instance.ogIndices,
            UnitRegister.instance.curMaxSpeeds,
            UnitRegister.instance.dirAccs,
            flowField.ogSize,
            flowField.ocRadius,
            flowField.cellToUnit,
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
            flowField.cellToObstacle,
            ObstacleRegister.instance.obstacleRegistry,
            UnitRegister.instance.velocities,
            flowField.ocRadius,
            flowField.ogSize,
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
            flowField.cellToObstacle,
            ObstacleRegister.instance.obstacleRegistry,
            flowField.ocRadius,
            flowField.ogSize,
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
            averageRadius += UnitRegister.instance.radii[i];
        }
        averageRadius /= UnitRegister.instance.indexer + 1;
        destRadius = 0.8f * averageRadius * sqrt2 * math.ceil(math.sqrt(UnitRegister.instance.indexer + 1));
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
            UnitType.OrangeSmall => 0.8f,
            UnitType.BlueSmall => 0.8f,
            UnitType.White => 2f,
            _ => 0,
        };

        for (int i = 0; i < cmd.count; i++)
        {
            Vector3 generationPos = new(cmd.pos.x, 0, cmd.pos.y);
            GameObject go = Instantiate(unit, generationPos, Quaternion.identity);
            UnitAgent agent = go.GetComponent<UnitAgent>();
            if (!agent)
            {
                Debug.LogError("[UnitBus] unitAgent not found");
                return;
            }
            agent.id = UnitRegister.instance.Register(go.transform, radius, moveSpeed, cmd.pos);
        }
    }

    private void Awake()
    {
        if (instance != null) Destroy(instance.gameObject);
        instance = this;

        EventBus.Subscribe<MoveToEvent>(SetDestination);
    }

    private FlowField flowField;

    private void Start()
    {
        flowField = GridController.instance.flowField;
    }

    private void Update()
    {
        if (UnitRegister.instance.indexer + 1 <= 0) return;

        UpdateArrivedBurst();
        UpdateCellToUnitBurst();
        UpdateUnitGridIndexBurst();

        UpdateUnitCurMaxSpeedBurst();
        // UpdateUnitDirAccBurst();
        UpdateUnitBoidsAccBurst();
        UpdateUnitVelocitiesBurst();
        // UpdateUnitPositionBurst();

        UpdateUnitTransformBurst();
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<MoveToEvent>(SetDestination);

        instance = null;
    }
}
