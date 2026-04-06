using Unity.Mathematics;
using UnityEngine;

public class UnitAgent : MonoBehaviour
{
    [SerializeField] private float moveSpeed;
    [SerializeField] private float unitRadius;

    [HideInInspector] public int id;

    private void ChangeID(UnitRemoveEvent evt)
    {
        if (id != evt.oldID) return;
        id = evt.newID;
    }

    private void Awake()
    {
        float2 pos = new(transform.position.x, transform.position.z);
        lastPos = pos;
        UnitAgentData data = new(unitRadius, moveSpeed, pos);
        id = UnitRegister.instance.Register(data);
    }

    private void OnEnable()
    {
        EventBus.Subscribe<UnitRemoveEvent>(ChangeID);
    }

    Quaternion rot = Quaternion.identity;
    float2 lastPos;
    float lastPosUpdateTimer;

    private void Update()
    {
        UnitAgentData data = UnitRegister.instance.unitRegistry[id];
        float2 pos = data.position;
        float2 posToLast = pos - lastPos;
        if (!UsefulUtils.Approximately(posToLast, float2.zero))
        {
            if (lastPosUpdateTimer > 0.1f)
            {
                if (math.lengthsq(posToLast) > 0.1f * 0.8f * data.curMaxSpeed) lastPos = pos;
                lastPosUpdateTimer = 0;
            }
            else
            {
                lastPosUpdateTimer += Time.deltaTime;
            }

            if (!UnitBus.arrived)
            {
                Quaternion desiredRot = Quaternion.LookRotation(new(posToLast.x, 0, posToLast.y), Vector3.up);
                rot = Quaternion.Slerp(transform.rotation, desiredRot, 2f * Time.deltaTime);
            }
        }

        transform.SetPositionAndRotation(new(pos.x, transform.position.y, pos.y), rot);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<UnitRemoveEvent>(ChangeID);

        if (UnitRegister.instance != null)
            UnitRegister.instance.Unregister(id);
    }
}
