using UnityEngine;

public class UnitPathFinder : MonoBehaviour
{
    [SerializeField] private GridController gridCC;
    [SerializeField] private float unitRadius;
    [SerializeField] private float moveSpeed;

    private Vector3 SteeringGetVelocity(FlowField flowField, Vector2Int unitGridPos)
    {
        var dir = flowField.Grid[unitGridPos.x, unitGridPos.y].direction;
        var cost = flowField.Grid[unitGridPos.x, unitGridPos.y].cost;
        var obstacleCellRadius = flowField.cellRadius * 1.4f + 0.2f;
        var curUnit2DPos = new Vector3(transform.position.x, 0, transform.position.z);
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                if (i == 0 && j == 0) continue;

                var curGridPos = new Vector2Int(unitGridPos.x + i, unitGridPos.y + j);
                if (curGridPos.x < 0 || curGridPos.x >= flowField.gridWidth || curGridPos.y < 0 || curGridPos.y >= flowField.gridHeight) continue;

                if (float.IsInfinity(flowField.Grid[curGridPos.x, curGridPos.y].cost))
                {
                    if (Vector3.Distance(curUnit2DPos, flowField.Grid[curGridPos.x, curGridPos.y].WorldPos) > obstacleCellRadius + unitRadius) continue;
                    dir = (curUnit2DPos - flowField.Grid[curGridPos.x, curGridPos.y].WorldPos).normalized;
                }
            }
        }

        var velocity = moveSpeed / cost * dir;
        return velocity;
    }

    private void MoveByFlowField()
    {
        Vector2Int unitGridPos = gridCC.CurFlowField.WorldToGridPos(transform.position);
        if (unitGridPos == new Vector2Int(-1, -1)) return;

        var velocity = SteeringGetVelocity(gridCC.CurFlowField, unitGridPos);

        transform.position = Time.deltaTime * velocity + transform.position;
    }

    private void Update()
    {
        MoveByFlowField();
    }
}
