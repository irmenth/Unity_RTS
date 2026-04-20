using Unity.Collections;
using UnityEngine;

public class ObstacleRegister : MonoBehaviour
{
    public static ObstacleRegister instance;
    public NativeArray<ObstacleData> obstacleRegistry;
    [HideInInspector] public int indexer = -1;

    private void ChangeID(int index, int id)
    {
        ObstacleData data = obstacleRegistry[index];
        data.id = id;
        obstacleRegistry[index] = data;
    }

    public int Register(ObstacleData data)
    {
        if (data.id <= indexer) return data.id;
        obstacleRegistry[++indexer] = data;
        ChangeID(indexer, indexer);
        return indexer;
    }

    public void Unregister(int id)
    {
        if (id > indexer) return;
        obstacleRegistry[id] = obstacleRegistry[indexer];
        ChangeID(id, id);
        EventBus.Publish(new ObstacleRemoveEvent(id, indexer));
        indexer--;
    }

    private void Awake()
    {
        instance = this;
        obstacleRegistry = new(500, Allocator.Persistent);
    }

    private void OnDestroy()
    {
        obstacleRegistry.Dispose();
        instance = null;
    }
}
