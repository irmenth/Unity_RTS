using System.Diagnostics;
using UnityEngine;
using UnityEngine.InputSystem;
using Debug = UnityEngine.Debug;

public class GridController : MonoBehaviour
{
    [Header("In Game")]
    [SerializeField] private Vector2Int directionGridSize;
    [SerializeField] private float directionCellRadius = 0.2f;
    [SerializeField] private Vector2Int obstacleGridSize;
    [SerializeField] private float obstacleCellRadius = 0.5f;
    [SerializeField] private LayerMask costLayerMask;
    [SerializeField] private LayerMask groundLayerMask;
    [SerializeField] private LayerMask impassibleLayerMask;
    [SerializeField] private LayerMask roughLayerMask;
    [Header("Debug Gizmos")]
    [SerializeField] private GameObject dirIndictorPrefab;
    [SerializeField] private Material upArrow;
    [SerializeField] private Material downArrow;
    [SerializeField] private Material leftArrow;
    [SerializeField] private Material rightArrow;
    [SerializeField] private Material upLeftArrow;
    [SerializeField] private Material upRightArrow;
    [SerializeField] private Material downLeftArrow;
    [SerializeField] private Material downRightArrow;
    [SerializeField] private Material cross;
    [SerializeField] private Material flag;

    public FlowField CurFlowField { get; private set; }

    // Gizmos for debug in editor
    private void OnDrawGizmos()
    {
#if UNITY_EDITOR
        if (CurFlowField == null) return;

        // Draw direction grid
        Gizmos.color = Color.yellow;
        for (int x = 0; x < directionGridSize.x; x++)
        {
            for (int y = 0; y < directionGridSize.y; y++)
            {
                Gizmos.DrawWireCube(CurFlowField.DirGrid[x, y].GetWorldPos(), directionCellRadius * 2f * Vector3.one);
            }
        }

        // // Draw Cost Field
        // for (int x = 0; x < gridSize.x; x++)
        // {
        //     for (int y = 0; y < gridSize.y; y++)
        //     {
        //         var pos = CurFlowField.DirGrid[x, y].GetWorldPos() + cellRadius * Vector3.left;
        //         UnityEditor.Handles.Label(pos, CurFlowField.DirGrid[x, y].cost.ToString("F1"));
        //     }
        // }

        // // Draw Heat Map
        // for (int x = 0; x < gridSize.x; x++)
        // {
        //     for (int y = 0; y < gridSize.y; y++)
        //     {
        //         var pos = CurFlowField.DirGrid[x, y].GetWorldPos() + cellRadius * Vector3.left;
        //         UnityEditor.Handles.Label(pos, CurFlowField.DirGrid[x, y].heat.ToString("F1"));
        //     }
        // }

        // // Draw Obstacle Count
        // for (int x = 0; x < gridSize.x; x++)
        // {
        //     for (int y = 0; y < gridSize.y; y++)
        //     {
        //         var pos = CurFlowField.ObstacleGrid[x, y].GetWorldPos() + cellRadius * Vector3.left;
        //         UnityEditor.Handles.Label(pos, CurFlowField.ObstacleGrid[x, y].obstacleList.Count.ToString());
        //     }
        // }

        // // Draw Flow Field
        // for (int x = 0; x < directionGridSize.x; x++)
        // {
        //     for (int y = 0; y < directionGridSize.y; y++)
        //     {
        //         var dir = CurFlowField.DirGrid[x, y].direction;
        //         var pos = CurFlowField.DirGrid[x, y].GetWorldPos() + 10f * Vector3.up;

        //         Material dirIndictorMat = null;
        //         if (dir == -Vector2.one)
        //             dirIndictorMat = cross;
        //         else if (dir == Vector2.up)
        //             dirIndictorMat = upArrow;
        //         else if (dir == Vector2.down)
        //             dirIndictorMat = downArrow;
        //         else if (dir == Vector2.left)
        //             dirIndictorMat = leftArrow;
        //         else if (dir == Vector2.right)
        //             dirIndictorMat = rightArrow;
        //         else if (dir == new Vector2(0.71f, 0.71f))
        //             dirIndictorMat = upRightArrow;
        //         else if (dir == new Vector2(-0.71f, 0.71f))
        //             dirIndictorMat = upLeftArrow;
        //         else if (dir == new Vector2(0.71f, -0.71f))
        //             dirIndictorMat = downRightArrow;
        //         else if (dir == new Vector2(-0.71f, -0.71f))
        //             dirIndictorMat = downLeftArrow;
        //         else if (dir == Vector2.zero)
        //             dirIndictorMat = flag;

        //         if (dirIndictorMat != null && dirIndicatorMeshRenderers[x, y] != null)
        //             dirIndicatorMeshRenderers[x, y].sharedMaterial = dirIndictorMat;
        //     }
        // }
#endif
    }

    private void SetDestination(InputAction.CallbackContext ctx)
    {
#if UNITY_EDITOR
        Stopwatch sw = new();
        sw.Start();
#endif

        var mousePos = Pointer.current.position.ReadValue();
        var ray = Camera.main.ScreenPointToRay(mousePos);
        if (Physics.Raycast(ray, out var hit, 1000f, groundLayerMask))
        {
            var mouseGridPos = CurFlowField.WorldToDirGridPos(hit.point);
            if (mouseGridPos == -Vector2Int.one) return;

            CurFlowField.GenerateHeatMap(mouseGridPos);
            CurFlowField.GenerateFlowFieldBurst();

            EventBus.Publish(new MoveToEvent(hit.point));
        }

#if UNITY_EDITOR
        sw.Stop();
        Debug.Log($"[GridController] heat map & flow field generation: {sw.ElapsedMilliseconds}ms");
#endif
    }

#if UNITY_EDITOR
    // private MeshRenderer[,] dirIndicatorMeshRenderers;
#endif
    private int impassibleLayer, roughLayer;

    private void Awake()
    {
        impassibleLayer = UsefulUtils.GetLayer(impassibleLayerMask);
        roughLayer = UsefulUtils.GetLayer(roughLayerMask);
    }

    private void Start()
    {
        CurFlowField = new FlowField(directionGridSize.x, directionGridSize.y, directionCellRadius, obstacleGridSize.x, obstacleGridSize.y, obstacleCellRadius);
        CurFlowField.GenerateGrid();
        CurFlowField.GenerateCostField(costLayerMask, impassibleLayer, roughLayer);
        CurFlowField.GenerateObstacleMap(impassibleLayer);

#if UNITY_EDITOR
        // dirIndicatorMeshRenderers = new MeshRenderer[directionGridSize.x, directionGridSize.y];

        // for (int x = 0; x < directionGridSize.x; x++)
        // {
        //     for (int y = 0; y < directionGridSize.y; y++)
        //     {
        //         var pos = CurFlowField.DirGrid[x, y].GetWorldPos() + 10 * Vector3.up;
        //         dirIndicatorMeshRenderers[x, y] = Instantiate(dirIndictorPrefab, pos, Quaternion.Euler(90, 0, 0)).GetComponent<MeshRenderer>();
        //     }
        // }
#endif

        InputActionsManager.RTSSetDestination.started += SetDestination;
    }

    private void OnDestroy()
    {
        InputActionsManager.RTSSetDestination.started -= SetDestination;
    }
}
