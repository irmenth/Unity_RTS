using Unity.Mathematics;
using UnityEngine;

public class UnitAgent : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float unitRadius;
    [SerializeField] private float clipLength;
    [SerializeField] private int clipFrame;
    [Header("Render")]
    [SerializeField] private Material sharedMaterial;
    [SerializeField] private Mesh sharedMesh;

    [HideInInspector] public int id;

    private void ChangeID(UnitRemoveEvent evt)
    {
        if (id != evt.oldID) return;
        id = evt.newID;
    }

    Quaternion rot = Quaternion.identity;
    float2 lastPos;
    float lastPosUpdateTimer;

    private void UpdateTransform()
    {
        UnitAgentData data = UnitRegister.instance.unitRegistry[id];
        float2 pos = data.position;
        float2 posToLast = pos - lastPos;
        if (math.lengthsq(posToLast) > 1e-4f)
        {
            if (lastPosUpdateTimer > 0.1f)
            {
                // 0.8f 只是一个参数，控制判断阈值
                if (math.lengthsq(posToLast) > 0.8f * 0.1f * data.curMaxSpeed) lastPos = pos;
                lastPosUpdateTimer = 0;
            }
            else
            {
                lastPosUpdateTimer += Time.deltaTime;
            }

            Quaternion desiredRot = Quaternion.LookRotation(new(posToLast.x, 0, posToLast.y), Vector3.up);
            rot = Quaternion.Slerp(tr.rotation, desiredRot, 4f * Time.deltaTime);
        }

        tr.SetPositionAndRotation(new(pos.x, tr.position.y, pos.y), rot);
    }

    private bool isMoving;

    private void UpdateAnimationState()
    {
        UnitAgentData data = UnitRegister.instance.unitRegistry[id];

        isMoving = math.lengthsq(data.velocity) >= 1;
    }

    private int curStateFrame, lerpFrame;
    private bool shouldLerp;
    private int frameOffset;
    private float speed;

    private void OnStateStart(AnimationState state, bool usingJudge)
    {
        if (usingJudge && stateChangeTimer < 0.3f * clipLength) return;
        if (usingJudge) animStateMachine.curState = state;

        lerpFrame = curStateFrame;
        animationTimer = 0;
        stateChangeTimer = 0;
        shouldLerp = true;

        switch (state)
        {
            case AnimationState.Idle:
                frameOffset = 0;
                speed = 2f;
                break;
            case AnimationState.Walk:
                frameOffset = 90;
                speed = 2.5f;
                break;
        }
    }

    private void OnStateUpdate(AnimationState state)
    {
        curStateFrame = (int)math.round(math.frac(animationTimer * speed / clipLength) * clipFrame + frameOffset);
        shouldLerp = shouldLerp && animationTimer < 0.25f * clipLength;
        float t = shouldLerp ? 1 - math.saturate(animationTimer * speed / (0.25f * clipLength)) : 0;

        InstancedAniManager.instance.SubmitInstance(
            sharedMesh,
            sharedMaterial,
            new InstancedAniManager.InstanceData(
                transform.localToWorldMatrix,
                curStateFrame,
                lerpFrame,
                t
            )
        );
    }

    private Transform tr;
    private AnimationStateMachine animStateMachine;

    private void Awake()
    {
        tr = transform;
        float2 pos = new(tr.position.x, tr.position.z);
        lastPos = pos;
        UnitAgentData data = new(unitRadius, moveSpeed, pos);
        id = UnitRegister.instance.Register(data);

        animStateMachine = new(AnimationState.Idle, OnStateStart, OnStateUpdate);

        animStateMachine.AddTransition(AnimationState.Idle, AnimationState.Walk, () => isMoving);
        animStateMachine.AddTransition(AnimationState.Walk, AnimationState.Idle, () => !isMoving);
    }

    private void OnEnable()
    {
        EventBus.Subscribe<UnitRemoveEvent>(ChangeID);
    }

    private float animationTimer;
    private float stateChangeTimer;

    private void Update()
    {
        animationTimer += Time.deltaTime;
        stateChangeTimer += Time.deltaTime;

        UpdateTransform();
        UpdateAnimationState();
        animStateMachine.Update();
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<UnitRemoveEvent>(ChangeID);

        if (UnitRegister.instance != null)
            UnitRegister.instance.Unregister(id);
    }
}
