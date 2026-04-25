using System.Diagnostics;
using Unity.Mathematics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class GridController : MonoBehaviour
{
    public static GridController instance;

    [Header("In Game")]
    [SerializeField] private GameObject unitPrefab;
    [SerializeField] private int2 directionGridSize;
    [SerializeField] private float directionCellRadius = 0.2f;
    [SerializeField] private int2 obstacleGridSize;
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

    public FlowField flowField;

    // Gizmos for debug in editor
    private void OnDrawGizmos()
    {
#if UNITY_EDITOR
        if (flowField == null) return;

        // Draw direction grid
        Gizmos.color = Color.yellow;
        for (int x = 0; x < directionGridSize.x; x++)
        {
            for (int y = 0; y < directionGridSize.y; y++)
            {
                int index = x * directionGridSize.y + y;
                Vector3 cubeWS = new(flowField.directionGrid[index].worldPos.x, -directionCellRadius, flowField.directionGrid[index].worldPos.y);
                Gizmos.DrawWireCube(cubeWS, directionCellRadius * 2f * Vector3.one);
            }
        }

        // // Draw obstacle grid
        // Gizmos.color = Color.green;
        // for (int x = 0; x < obstacleGridSize.x; x++)
        // {
        //     for (int y = 0; y < obstacleGridSize.y; y++)
        //     {
        //         int index = x * obstacleGridSize.y + y;
        //         Vector3 cubeWS = new(flowField.obstacleGrid[index].worldPos.x, -obstacleCellRadius, flowField.obstacleGrid[index].worldPos.y);
        //         Gizmos.DrawWireCube(cubeWS, obstacleCellRadius * 2f * Vector3.one);
        //     }
        // }

        // Draw Cost Field
        for (int x = 0; x < directionGridSize.x; x++)
        {
            for (int y = 0; y < directionGridSize.y; y++)
            {
                int index = x * directionGridSize.y + y;
                Vector3 labelWS = new(flowField.directionGrid[index].worldPos.x - flowField.dcRadius, 0, flowField.directionGrid[index].worldPos.y);
                UnityEditor.Handles.Label(labelWS, flowField.costMap[index].ToString("F1"));
            }
        }

        // // Draw Heat Map
        // for (int x = 0; x < directionGridSize.x; x++)
        // {
        //     for (int y = 0; y < directionGridSize.y; y++)
        //     {
        //         int index = x * directionGridSize.y + y;
        //         Vector3 labelWS = new(flowField.directionGrid[index].worldPos.x - flowField.dcRadius, 0, flowField.directionGrid[index].worldPos.y);
        //         UnityEditor.Handles.Label(labelWS, flowField.directionGrid[index].heat.ToString("F1"));
        //     }
        // }

        // // Draw Obstacle Count
        // for (int x = 0; x < obstacleGridSize.x; x++)
        // {
        //     for (int y = 0; y < obstacleGridSize.y; y++)
        //     {
        //         int index = x * obstacleGridSize.y + y;
        //         Vector3 labelWS = new(flowField.obstacleGrid[index].worldPos.x, 0, flowField.obstacleGrid[index].worldPos.y);
        //         UnityEditor.Handles.Label(labelWS, flowField.cellToObstacle.CountValuesForKey(index).ToString());
        //     }
        // }

        // // Draw Flow Field
        // for (int x = 0; x < directionGridSize.x; x++)
        // {
        //     for (int y = 0; y < directionGridSize.y; y++)
        //     {
        //         int index = x * directionGridSize.y + y;
        //         float2 dir = flowField.directionGrid[index].direction;

        //         Material dirIndictorMat = null;
        //         if (math.isinf(dir.x) && math.isinf(dir.y))
        //             dirIndictorMat = cross;
        //         else if (UsefulUtils.Approximately(dir, new float2(0, 1)))
        //             dirIndictorMat = upArrow;
        //         else if (UsefulUtils.Approximately(dir, new float2(0, -1)))
        //             dirIndictorMat = downArrow;
        //         else if (UsefulUtils.Approximately(dir, new float2(-1, 0)))
        //             dirIndictorMat = leftArrow;
        //         else if (UsefulUtils.Approximately(dir, new float2(1, 0)))
        //             dirIndictorMat = rightArrow;
        //         else if (UsefulUtils.Approximately(dir, math.normalize(new float2(1, 1))))
        //             dirIndictorMat = upRightArrow;
        //         else if (UsefulUtils.Approximately(dir, math.normalize(new float2(-1, 1))))
        //             dirIndictorMat = upLeftArrow;
        //         else if (UsefulUtils.Approximately(dir, math.normalize(new float2(1, -1))))
        //             dirIndictorMat = downRightArrow;
        //         else if (UsefulUtils.Approximately(dir, math.normalize(new float2(-1, -1))))
        //             dirIndictorMat = downLeftArrow;
        //         else if (UsefulUtils.Approximately(dir, new float2(0, 0)))
        //             dirIndictorMat = flag;

        //         if (dirIndictorMat != null && dirIndicatorMeshRenderers[x, y] != null)
        //             dirIndicatorMeshRenderers[x, y].sharedMaterial = dirIndictorMat;
        //     }
        // }
#endif
    }

    //     private void SetDestination(InputAction.CallbackContext ctx)
    //     {
    // #if UNITY_EDITOR
    //         Stopwatch sw = new();
    //         sw.Start();
    // #endif

    //         Vector2 mousePos = Pointer.current.position.ReadValue();
    //         Ray ray = Camera.main.ScreenPointToRay(mousePos);
    //         if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayerMask))
    //         {
    //             float2 hitPoint = new(hit.point.x, hit.point.z);
    //             int mouseGridIndex = flowField.WorldToDGIndex(hitPoint);
    //             if (mouseGridIndex == -1) return;

    //             float2 dest = flowField.GenerateHeatMapBurst(mouseGridIndex);
    //             flowField.GenerateFlowFieldBurst();

    //             EventBus.Publish(new MoveToEvent(UsefulUtils.Approximately(dest, new(-1, -1)) ? hitPoint : dest));
    //         }

    // #if UNITY_EDITOR
    //         sw.Stop();
    //         Debug.Log($"[GridController] heat map & flow field generation: {sw.ElapsedMilliseconds}ms");
    // #endif
    //     }

    // #if UNITY_EDITOR
    //     private MeshRenderer[,] dirIndicatorMeshRenderers;
    // #endif
    private int impassibleLayer, roughLayer;

    private void Awake()
    {
        if (instance != null) Destroy(instance.gameObject);
        instance = this;

        impassibleLayer = UsefulUtils.GetLayer(impassibleLayerMask);
        roughLayer = UsefulUtils.GetLayer(roughLayerMask);
    }

    private void Start()
    {
#if UNITY_EDITOR
        Stopwatch sw = new();
        sw.Start();
#endif
        flowField = new(directionGridSize, directionCellRadius, obstacleGridSize, obstacleCellRadius);
        flowField.GenerateGridBurst();
        flowField.GenerateCostField(costLayerMask, impassibleLayer, roughLayer);
        flowField.GenerateObstacleMap(impassibleLayer);
#if UNITY_EDITOR
        sw.Stop();
        Debug.Log($"[GridController] cost field & obstacle map generation: {sw.ElapsedMilliseconds}ms");
#endif

        // #if UNITY_EDITOR
        //         dirIndicatorMeshRenderers = new MeshRenderer[directionGridSize.x, directionGridSize.y];

        //         for (int x = 0; x < directionGridSize.x; x++)
        //         {
        //             for (int y = 0; y < directionGridSize.y; y++)
        //             {
        //                 int index = x * directionGridSize.y + y;
        //                 Vector3 indictorWS = new(flowField.directionGrid[index].worldPos.x, 0.1f, flowField.directionGrid[index].worldPos.y);
        //                 dirIndicatorMeshRenderers[x, y] = Instantiate(dirIndictorPrefab, indictorWS, Quaternion.Euler(90, 0, 0)).GetComponent<MeshRenderer>();
        //             }
        //         }
        // #endif
    }

    private void OnDestroy()
    {
        flowField.Dispose();

        instance = null;
    }
}
