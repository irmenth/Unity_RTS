using Unity.Collections;
using UnityEngine;

public class UnitRegister : MonoBehaviour
{
    public static UnitRegister instance;
    public NativeArray<UnitAgentData> unitRegistry;
    public int indexer = -1;

    private void ChangeID(int index, int id)
    {
        UnitAgentData data = unitRegistry[index];
        data.id = id;
        unitRegistry[index] = data;
    }

    public int Register(UnitAgentData data)
    {
        if (data.id <= indexer) return data.id;
        unitRegistry[++indexer] = data;
        ChangeID(indexer, indexer);
        return indexer;
    }

    public void Unregister(int id)
    {
        if (id > indexer) return;
        unitRegistry[id] = unitRegistry[indexer];
        ChangeID(id, id);
        EventBus.Publish(new UnitRemoveEvent(indexer, id));
        indexer--;
    }

    private void Awake()
    {
        instance = this;
        unitRegistry = new((int)6e3f, Allocator.Persistent);
    }

    private void OnDestroy()
    {
        unitRegistry.Dispose();
        instance = null;
    }
}
