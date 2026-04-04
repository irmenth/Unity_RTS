using Unity.Mathematics;
using UnityEngine;

public class ObstacleAgent : MonoBehaviour
{
    [SerializeField] private ObstacleType obstacleType;
    [SerializeField] private float circleRadius;
    [SerializeField] private float2 rectSize;

    [HideInInspector] public int id = int.MaxValue;

    private void ChangeID(ObstacleRemoveEvent evt)
    {
        if (id != evt.oldID) return;
        id = evt.newID;
    }

    private void Awake()
    {
        float2 pos = new(transform.position.x, transform.position.z);
        float2 right = new(transform.right.x, transform.right.z);
        float2 up = new(transform.forward.x, transform.forward.z);
        ObstacleData obstacle = new(obstacleType, new Circle(pos, circleRadius), new Rectangle(pos, rectSize, right, up));
        id = ObstacleRegister.instance.Register(obstacle);
    }

    private void OnEnable()
    {
        EventBus.Subscribe<ObstacleRemoveEvent>(ChangeID);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<ObstacleRemoveEvent>(ChangeID);

        if (ObstacleRegister.instance != null)
            ObstacleRegister.instance.Unregister(id);
    }
}
