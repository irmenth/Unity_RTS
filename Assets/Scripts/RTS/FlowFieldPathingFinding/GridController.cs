using System.Diagnostics;
using UnityEngine;
using UnityEngine.InputSystem;
using Debug = UnityEngine.Debug;

public class GridController : MonoBehaviour
{
    [Header("Game Logic")]
    [SerializeField] private Vector2Int gridSize;
    [SerializeField] private float cellRadius = 0.5f;
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

        // Draw grid
        Gizmos.color = Color.yellow;
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                Gizmos.DrawWireCube(CurFlowField.Grid[x, y].WorldPos, cellRadius * 2f * Vector3.one);
            }
        }

        // // Draw Cost Field
        // for (int x = 0; x < gridSize.x; x++)
        // {
        //     for (int y = 0; y < gridSize.y; y++)
        //     {
        //         var pos = CurFlowField.Grid[x, y].WorldPos + cellRadius * Vector3.left;
        //         UnityEditor.Handles.Label(pos, CurFlowField.Grid[x, y].cost.ToString());
        //     }
        // }

        // // Draw Heat Map
        // for (int x = 0; x < gridSize.x; x++)
        // {
        //     for (int y = 0; y < gridSize.y; y++)
        //     {
        //         var pos = CurFlowField.Grid[x, y].WorldPos + cellRadius * Vector3.left;
        //         UnityEditor.Handles.Label(pos, CurFlowField.Grid[x, y].heat.ToString("F1"));
        //     }
        // }

        // Draw Flow Field
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                var dir = CurFlowField.Grid[x, y].direction;
                var pos = CurFlowField.Grid[x, y].WorldPos + 10f * Vector3.up;

                Material dirIndictorMat = null;
                if (dir == new Vector3(-1, -1, -1))
                    dirIndictorMat = cross;
                else if (dir == Vector3.forward)
                    dirIndictorMat = upArrow;
                else if (dir == Vector3.back)
                    dirIndictorMat = downArrow;
                else if (dir == Vector3.left)
                    dirIndictorMat = leftArrow;
                else if (dir == Vector3.right)
                    dirIndictorMat = rightArrow;
                else if (dir == new Vector3(0.71f, 0, 0.71f))
                    dirIndictorMat = upRightArrow;
                else if (dir == new Vector3(-0.71f, 0, 0.71f))
                    dirIndictorMat = upLeftArrow;
                else if (dir == new Vector3(0.71f, 0, -0.71f))
                    dirIndictorMat = downRightArrow;
                else if (dir == new Vector3(-0.71f, 0, -0.71f))
                    dirIndictorMat = downLeftArrow;
                else if (dir == Vector3.zero)
                    dirIndictorMat = flag;

                if (dirIndictorMat != null && dirIndicatorMeshRenderers[x, y] != null)
                    dirIndicatorMeshRenderers[x, y].sharedMaterial = dirIndictorMat;
            }
        }
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
            var mouseGridPos = CurFlowField.WorldToGridPos(hit.point);
            if (mouseGridPos == new Vector2Int(-1, -1)) return;

            CurFlowField.GenerateHeatMap(mouseGridPos);
            CurFlowField.GenerateFlowField();
        }

#if UNITY_EDITOR
        sw.Stop();
        Debug.Log($"[GridController] heat map & flow field generation: {sw.ElapsedMilliseconds}ms");
#endif
    }

#if UNITY_EDITOR
    private MeshRenderer[,] dirIndicatorMeshRenderers;
#endif
    private int impassibleLayer, roughLayer;

    private void Awake()
    {
        impassibleLayer = UsefulUtils.GetLayer(impassibleLayerMask);
        roughLayer = UsefulUtils.GetLayer(roughLayerMask);
    }

    private void Start()
    {
        CurFlowField = new FlowField(gridSize.x, gridSize.y, cellRadius);
        CurFlowField.GenerateGrid();
        CurFlowField.GenerateCostField(costLayerMask, impassibleLayer, roughLayer);

#if UNITY_EDITOR
        dirIndicatorMeshRenderers = new MeshRenderer[gridSize.x, gridSize.y];

        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                var pos = CurFlowField.Grid[x, y].WorldPos + 10 * Vector3.up;
                dirIndicatorMeshRenderers[x, y] = Instantiate(dirIndictorPrefab, pos, Quaternion.Euler(90, 0, 0)).GetComponent<MeshRenderer>();
            }
        }
#endif

        InputActionsManager.RTSSetDestination.started += SetDestination;
    }

    private void OnDestroy()
    {
        InputActionsManager.RTSSetDestination.started -= SetDestination;
    }
}
