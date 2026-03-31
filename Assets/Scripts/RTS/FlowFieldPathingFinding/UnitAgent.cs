using UnityEngine;

public class UnitAgent : MonoBehaviour
{
    public GridController gridCC;
    public float unitRadius;
    [SerializeField] private float moveSpeed;

    private Vector3 position;
    private FlowField flowField;
    private DirCell[,] dg;
    private ObstacleCell[,] og;
    private Vector2Int unitDgPos;
    private Vector2Int unitOgPos;
    private float curMaxSpeed;
    private Vector2 acceleration;

    private void InitVariables()
    {
        position = transform.position;
        flowField = gridCC.CurFlowField;
        dg = flowField.DirGrid;
        og = flowField.ObstacleGrid;
        unitDgPos = flowField.WorldToDirGridPos(position);
        unitOgPos = flowField.WorldToObstacleGridPos(position);
        var cost = dg[unitDgPos.x, unitDgPos.y].cost;
        curMaxSpeed = float.IsInfinity(cost) ? moveSpeed : moveSpeed / cost;
        acceleration = Vector2.zero;
    }

    private void FlowFieldAccelerate()
    {
        if (Vector2.SqrMagnitude(UsefulUtils.V3ToV2(position) - destination) > Mathf.Pow(unitRadius, 2))
        {
            var dir = dg[unitDgPos.x, unitDgPos.y].direction;
            if (dir != -Vector2.one)
                acceleration = 8f * curMaxSpeed * dir;
        }
    }

    private Vector2 velocity;

    private void ApplyAcceleration()
    {
        if (velocity.sqrMagnitude <= 1e-4f)
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

    private void BoidsVelocityCorrection()
    {
        var offsetSum = Vector2.zero;
        var count = 0;
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                var newPos = new Vector2Int(unitOgPos.x + dx, unitOgPos.y + dy);
                if (newPos.x < 0 || newPos.x >= flowField.ogWidth || newPos.y < 0 || newPos.y >= flowField.ogHeight) continue;

                foreach (var other in og[newPos.x, newPos.y].unitList)
                {
                    if (other == this) continue;

                    var diff = UsefulUtils.V3ToV2(other.position - position);
                    if (diff.sqrMagnitude <= Mathf.Pow(unitRadius + other.unitRadius, 2))
                    {
                        offsetSum += 1 / diff.magnitude * diff.normalized;
                        count++;
                    }
                }
            }
        }

        if (count > 0)
        {
            velocity -= offsetSum / count;
        }
    }

    private void KenimaticVelocityCorrection()
    {
        var predictPos = Time.deltaTime * velocity + UsefulUtils.V3ToV2(position);
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
        foreach (var obstacle in og[unitOgPos.x, unitOgPos.y].obstacleList)
        {
            switch (obstacle.type)
            {
                case ObstacleType.Circle:
                    position = UsefulUtils.IfIntersectWithCircleObstacle(obstacle.circle, position, unitRadius);
                    break;
                case ObstacleType.Rectangle:
                    position = UsefulUtils.IfIntersectWithRectObstacle(obstacle.rect, position, unitRadius);
                    break;
            }
        }
    }

    private Vector2Int lastGridPos;

    private void UpdateUnitCurrentCell()
    {
        flowField = gridCC.CurFlowField;
        og = flowField.ObstacleGrid;

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
        destination = UsefulUtils.V3ToV2(evt.destination);
    }

    private void Awake()
    {
        EventBus.Subscribe<MoveToEvent>(MarkIsMovingTo);
    }

    private void FixedUpdate()
    {
        InitVariables();
        if (unitDgPos == new Vector2Int(-1, -1) || unitOgPos == new Vector2Int(-1, -1))
        {
            Debug.LogError($"[UnitAgent] unit is out of grid: {gameObject.name}");
            velocity = Vector2.zero;
            return;
        }

        velocity *= 0.95f;
        if (!Stopped)
        {
            FlowFieldAccelerate();
            ApplyAcceleration();
        }
        BoidsVelocityCorrection();
        KenimaticVelocityCorrection();

        position += UsefulUtils.V2ToV3(velocity) * Time.deltaTime;
        KenimaticPositionCorrection();
        transform.SetPositionAndRotation(position, transform.rotation);

        UpdateUnitCurrentCell();
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<MoveToEvent>(MarkIsMovingTo);
    }
}
