using UnityEngine;

public class UnitAgent : MonoBehaviour
{
    [SerializeField] private GridController gridCC;
    [SerializeField] private float unitRadius;
    [SerializeField] private float moveSpeed;

    private Vector3 selfPos;
    private FlowField flowField;
    private DirCell[,] dg;
    private ObstacleCell[,] og;
    private Vector2Int unitDgPos;
    private Vector2Int unitOgPos;
    private float curMaxSpeed;

    private void InitVariables()
    {
        selfPos = transform.position;
        flowField = gridCC.CurFlowField;
        dg = flowField.DirGrid;
        og = flowField.ObstacleGrid;
        unitDgPos = flowField.WorldToDirGridPos(selfPos);
        unitOgPos = flowField.WorldToObstacleGridPos(selfPos);
        var cost = dg[unitDgPos.x, unitDgPos.y].cost;
        curMaxSpeed = float.IsInfinity(cost) ? moveSpeed : moveSpeed / cost;
    }

    private Vector2 acceleration;
    private bool arrived;

    private void FlowFieldAccelerate()
    {
        acceleration = Vector2.zero;

        if (unitDgPos == new Vector2Int(-1, -1))
        {
            velocity = Vector2.zero;
            return;
        }

        if (Vector2.SqrMagnitude(UsefulUtils.V3ToV2(selfPos) - destination) <= Mathf.Pow(unitRadius, 2))
        {
            arrived = true;
            acceleration = 8f * curMaxSpeed * -velocity.normalized;
        }
        else
        {
            var dir = dg[unitDgPos.x, unitDgPos.y].direction;
            if (dir != -Vector2.one)
                acceleration = 8f * curMaxSpeed * dir;
        }
    }

    private void SteeringAccelerationCorrection()
    {
        var predictPos = Time.deltaTime * velocity + UsefulUtils.V3ToV2(selfPos);
        var predictOgPos = flowField.WorldToObstacleGridPos(predictPos);
        if (predictOgPos == new Vector2Int(-1, -1))
        {
            velocity = Vector2.zero;
            return;
        }

        var selfPos2D = UsefulUtils.V3ToV2(selfPos);
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                var newPos2D = new Vector2Int(predictOgPos.x + dx, predictOgPos.y + dy);
                if (newPos2D.x < 0 || newPos2D.x >= flowField.ogWidth || newPos2D.y < 0 || newPos2D.y >= flowField.ogHeight) continue;

                foreach (var otherUnit in og[newPos2D.x, newPos2D.y].unitList)
                {
                    if (otherUnit.transform == transform) continue;

                    var otherPos = otherUnit.transform.position;
                    var otherPos2D = UsefulUtils.V3ToV2(otherPos);
                    if (Vector2.SqrMagnitude(otherPos2D - selfPos2D) <= Mathf.Pow(unitRadius + otherUnit.unitRadius, 2))
                    {
                        var dir = (selfPos2D - otherPos2D).normalized;
                        var mag = curMaxSpeed / Time.deltaTime;
                        acceleration += mag * dir;
                    }
                }
            }
        }
    }

    private Vector2 velocity;

    private void ApplyVelocity()
    {
        if (unitDgPos == new Vector2Int(-1, -1))
        {
            velocity = Vector2.zero;
            return;
        }

        if (arrived && velocity.sqrMagnitude <= 1e-4f)
        {
            Stopped = true;
            velocity = Vector2.zero;
        }
        else
        {
            velocity += acceleration * Time.deltaTime;
        }

        velocity = Vector2.ClampMagnitude(velocity, curMaxSpeed);
    }

    private void KenimaticVelocityCorrection()
    {
        var predictPos = Time.deltaTime * velocity + UsefulUtils.V3ToV2(selfPos);
        var predictOgPos = flowField.WorldToObstacleGridPos(predictPos);
        if (predictOgPos == new Vector2Int(-1, -1))
        {
            velocity = Vector2.zero;
            return;
        }

        var obstacleList = og[predictOgPos.x, predictOgPos.y].obstacleList;
        foreach (var obstacle in obstacleList)
        {
            switch (obstacle.type)
            {
                case ObstacleType.Circle:
                    if (UsefulUtils.HasCollideWithCircleObstacle(obstacle.circle, predictPos, unitRadius, out var negImpactDir))
                        velocity = UsefulUtils.ProjectOnLine(velocity, negImpactDir);
                    break;
                case ObstacleType.Rectangle:
                    if (UsefulUtils.HasCollideWithRectObstacle(obstacle.rect, predictPos, unitRadius, out negImpactDir))
                        velocity = UsefulUtils.ProjectOnLine(velocity, negImpactDir);
                    break;
            }
        }
    }

    private void KenimaticPositionCorrection()
    {
        if (unitOgPos == new Vector2Int(-1, -1))
        {
            velocity = Vector2.zero;
            return;
        }

        var obstacleList = og[unitOgPos.x, unitOgPos.y].obstacleList;
        foreach (var obstacle in obstacleList)
        {
            switch (obstacle.type)
            {
                case ObstacleType.Circle:
                    UsefulUtils.IfIntersectWithCircleObstacle(obstacle.circle, transform, unitRadius);
                    break;
                case ObstacleType.Rectangle:
                    UsefulUtils.IfIntersectWithRectObstacle(obstacle.rect, transform, unitRadius);
                    break;
            }
        }
    }

    private Vector2Int lastGridPos;

    private void UpdateUnitCurrentCell()
    {
        flowField = gridCC.CurFlowField;
        og = flowField.ObstacleGrid;

        if (unitOgPos == new Vector2Int(-1, -1)) return;

        if (unitOgPos != lastGridPos)
            og[unitOgPos.x, unitOgPos.y].unitList.Remove(this);

        if (!og[unitOgPos.x, unitOgPos.y].unitList.Contains(this))
            og[unitOgPos.x, unitOgPos.y].unitList.Add(this);

        lastGridPos = unitOgPos;
    }

    public bool Stopped { get; private set; } = true;
    private Vector2 destination;

    private void MarkIsMovingTo(MoveToEvent evt)
    {
        Stopped = false;
        arrived = false;
        destination = UsefulUtils.V3ToV2(evt.destination);
    }

    private void Awake()
    {
        EventBus.Subscribe<MoveToEvent>(MarkIsMovingTo);
    }

    private void Update()
    {
        InitVariables();

        if (!Stopped)
        {
            FlowFieldAccelerate();
            ApplyVelocity();

            SteeringAccelerationCorrection();
            ApplyVelocity();

            KenimaticVelocityCorrection();

            transform.position += UsefulUtils.V2ToV3(velocity) * Time.deltaTime;
        }

        KenimaticPositionCorrection();

        UpdateUnitCurrentCell();
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<MoveToEvent>(MarkIsMovingTo);
    }
}
