using Unity.Mathematics;
using UnityEngine;

public class UnitAgent : MonoBehaviour
{
    [SerializeField] private float moveSpeed;
    [SerializeField] private float unitRadius;

    public int id;

    private void ChangeID(UnitRemoveEvent evt)
    {
        if (id != evt.oldID) return;
        id = evt.newID;
    }

    private void Awake()
    {
        float2 pos = new(transform.position.x, transform.position.z);
        UnitAgentData data = new(unitRadius, moveSpeed, pos);
        id = UnitRegister.instance.Register(data);
    }

    private void OnEnable()
    {
        EventBus.Subscribe<UnitRemoveEvent>(ChangeID);
    }

    private void Update()
    {
        var pos = UnitRegister.instance.unitRegistry[id].position;
        transform.position = new Vector3(pos.x, 0, pos.y);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<UnitRemoveEvent>(ChangeID);

        if (UnitRegister.instance != null)
            UnitRegister.instance.Unregister(id);
    }
    // public int unitID;
    // public GridController gridCC;
    // public float unitRadius;
    // [SerializeField] private float moveSpeed;

    // public UnitAgentData unitData;
    // private Vector3 position;
    // private FlowField flowField;
    // private DirCell[,] dg;
    // private ObstacleCell[,] og;
    // private Vector2Int unitDgPos;
    // private Vector2Int unitOgPos;
    // private float curMaxSpeed;

    // private void InitVariables()
    // {
    //     position = transform.position;
    //     flowField = gridCC.CurFlowField;
    //     dg = flowField.DirGrid;
    //     og = flowField.ObstacleGrid;
    //     unitDgPos = flowField.WorldToDirGridPos(position);
    //     unitOgPos = flowField.WorldToObstacleGridPos(position);
    //     var cost = dg[unitDgPos.x, unitDgPos.y].cost;
    //     curMaxSpeed = float.IsInfinity(cost) ? moveSpeed : moveSpeed / cost;
    // }

    // private Vector2 velocity;

    // private void FlowFieldVelocity()
    // {
    //     if (Vector2.SqrMagnitude(UsefulUtils.V3ToV2(position) - destination) > Mathf.Pow(unitRadius, 2))
    //     {
    //         var dir = dg[unitDgPos.x, unitDgPos.y].direction;
    //         if (dir != -Vector2.one)
    //         {
    //             velocity += 4f * curMaxSpeed * Time.deltaTime * dir;
    //             velocity = curMaxSpeed * velocity.normalized;
    //         }
    //     }
    //     else
    //     {
    //         // Stopped = true;
    //     }
    // }

    // private void BoidsVelocityCorrection()
    // {
    //     var offsetSum = Vector2.zero;
    //     var count = 0;
    //     for (int dx = -1; dx <= 1; dx++)
    //     {
    //         for (int dy = -1; dy <= 1; dy++)
    //         {
    //             var newPos = new Vector2Int(unitOgPos.x + dx, unitOgPos.y + dy);
    //             if (newPos.x < 0 || newPos.x >= flowField.ogWidth || newPos.y < 0 || newPos.y >= flowField.ogHeight) continue;

    //             foreach (var other in og[newPos.x, newPos.y].unitList)
    //             {
    //                 if (other == unitData) continue;

    //                 var diff = UsefulUtils.V3ToV2(other.position - position);
    //                 if (diff == Vector2.zero)
    //                     diff = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f));
    //                 if (diff.sqrMagnitude < Mathf.Pow(unitRadius + other.unitRadius, 2))
    //                 {
    //                     offsetSum += (unitRadius + other.unitRadius) * diff.normalized - diff;
    //                     count++;
    //                 }
    //             }
    //         }
    //     }

    //     if (count > 0)
    //     {
    //         if (velocity.sqrMagnitude > offsetSum.sqrMagnitude)
    //             velocity -= velocity.magnitude * offsetSum.normalized;
    //         else
    //             velocity -= offsetSum;
    //         velocity = Vector2.ClampMagnitude(velocity, curMaxSpeed);
    //     }
    // }

    // private void KenimaticVelocityCorrection()
    // {
    //     var predictPos = Time.deltaTime * velocity + UsefulUtils.V3ToV2(position);
    //     var predictOgPos = flowField.WorldToObstacleGridPos(predictPos);
    //     if (predictOgPos == new Vector2Int(-1, -1))
    //     {
    //         velocity = Vector2.zero;
    //         return;
    //     }

    //     foreach (var obstacle in og[predictOgPos.x, predictOgPos.y].obstacleList)
    //     {
    //         switch (obstacle.type)
    //         {
    //             case ObstacleType.Circle:
    //                 if (UsefulUtils.HasCollideWithCircleObstacle(obstacle.circle, predictPos, unitRadius, out var negImpactDir))
    //                     velocity = UsefulUtils.ProjectOnLine(velocity, negImpactDir);
    //                 break;
    //             case ObstacleType.Rectangle:
    //                 if (UsefulUtils.HasCollideWithRectObstacle(obstacle.rect, predictPos, unitRadius, out negImpactDir))
    //                     velocity = UsefulUtils.ProjectOnLine(velocity, negImpactDir);
    //                 break;
    //         }
    //     }
    // }

    // private void KenimaticPositionCorrection()
    // {
    //     foreach (var obstacle in og[unitOgPos.x, unitOgPos.y].obstacleList)
    //     {
    //         switch (obstacle.type)
    //         {
    //             case ObstacleType.Circle:
    //                 position = UsefulUtils.IfIntersectWithCircleObstacle(obstacle.circle, position, unitRadius);
    //                 break;
    //             case ObstacleType.Rectangle:
    //                 position = UsefulUtils.IfIntersectWithRectObstacle(obstacle.rect, position, unitRadius);
    //                 break;
    //         }
    //     }
    // }

    // private Vector2Int lastGridPos;

    // private void UpdateUnitCurrentCell()
    // {
    //     flowField = gridCC.CurFlowField;
    //     og = flowField.ObstacleGrid;

    //     if (unitOgPos != lastGridPos)
    //         og[unitOgPos.x, unitOgPos.y].unitList.Remove(this);

    //     if (!og[unitOgPos.x, unitOgPos.y].unitList.Contains(this))
    //         og[unitOgPos.x, unitOgPos.y].unitList.Add(this);

    //     lastGridPos = unitOgPos;
    // }

    // public static bool Stopped { get; private set; } = true;
    // private Vector2 destination;

    // private void MarkIsMovingTo(MoveToEvent evt)
    // {
    //     Stopped = false;
    //     destination = UsefulUtils.V3ToV2(evt.destination);
    // }

    // private void Awake()
    // {
    //     unitID = UnitRegister.Register(unitData);
    //     EventBus.Subscribe<MoveToEvent>(MarkIsMovingTo);
    // }

    // private void FixedUpdate()
    // {
    //     InitVariables();
    //     if (unitDgPos == new Vector2Int(-1, -1) || unitOgPos == new Vector2Int(-1, -1))
    //     {
    //         Debug.LogError($"[UnitAgent] unit is out of grid: {gameObject.name}");
    //         velocity = Vector2.zero;
    //         return;
    //     }

    //     velocity *= Mathf.Exp(-8f * Time.deltaTime);
    //     if (!Stopped)
    //         FlowFieldVelocity();
    //     else if (velocity.sqrMagnitude < 1e-12f)
    //         velocity = Vector2.zero;
    //     BoidsVelocityCorrection();
    //     KenimaticVelocityCorrection();

    //     position += UsefulUtils.V2ToV3(velocity) * Time.deltaTime;
    //     KenimaticPositionCorrection();
    //     transform.SetPositionAndRotation(position, transform.rotation);

    //     UpdateUnitCurrentCell();
    // }

    // private void OnDestroy()
    // {
    //     UnitRegister.Unregister(unitID);
    //     EventBus.Unsubscribe<MoveToEvent>(MarkIsMovingTo);
    // }
}
