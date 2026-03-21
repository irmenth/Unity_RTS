using UnityEngine;

public class UnitPathFinder : MonoBehaviour
{
    [SerializeField] private GridController gridCC;
    [SerializeField] private float moveSpeed;

    private Vector3 lastDir;
    private float lastCost;

    private void MoveByFlowField()
    {
        Vector2Int unitGridPos = gridCC.CurFlowField.WorldToGridPos(transform.position);
        if (unitGridPos == new Vector2Int(-1, -1)) return;

        var cost = gridCC.CurFlowField.Grid[unitGridPos.x, unitGridPos.y].cost;
        var dir = gridCC.CurFlowField.Grid[unitGridPos.x, unitGridPos.y].direction;
        if (dir == Vector3.zero) return;
        if (dir == -1 * Vector3.one)
        {
            dir = lastDir;
            cost = lastCost;
        }

        transform.position = Time.deltaTime * moveSpeed / cost * dir + transform.position;
        lastDir = dir;
        lastCost = cost;
    }

    private void Update()
    {
        MoveByFlowField();
    }
}
