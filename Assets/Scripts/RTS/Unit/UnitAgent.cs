using Unity.Mathematics;
using UnityEngine;

public class UnitAgent : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private float clipLength;
    [SerializeField] private int clipFrame;
    [Header("Render")]
    [SerializeField] private Material sharedMaterial;
    [SerializeField] private Mesh sharedMesh;
    [SerializeField] private Material indicatorSharedMaterial;
    [SerializeField] private Mesh indicatorSharedMesh;
    [Header("Indicator")]
    [SerializeField] private GameObject indicator;
    [HideInInspector] public int id;

    private void ChangeID(UnitRemoveEvent evt)
    {
        if (id != evt.oldID) return;
        id = evt.newID;
    }

    private bool isMoving;

    private void UpdateAnimationState()
    {
        isMoving = math.lengthsq(UnitRegister.instance.velocities[id]) >= 1;
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

    private float t;

    private void OnStateUpdate(AnimationState state)
    {
        curStateFrame = (int)math.round(math.frac(animationTimer * speed / clipLength) * clipFrame + frameOffset);
        shouldLerp = shouldLerp && animationTimer < 0.25f * clipLength;
        t = shouldLerp ? 1 - math.saturate(animationTimer * speed / (0.25f * clipLength)) : 0;

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

    private AnimationStateMachine animStateMachine;

    private void Awake()
    {
        indicator.SetActive(false);

        animStateMachine = new(AnimationState.Idle, OnStateStart, OnStateUpdate);

        animStateMachine.AddTransition(AnimationState.Idle, AnimationState.Walk, () => isMoving);
        animStateMachine.AddTransition(AnimationState.Walk, AnimationState.Idle, () => !isMoving);

        EventBus.Subscribe<UnitRemoveEvent>(ChangeID);
    }

    private float animationTimer;
    private float stateChangeTimer;
    private bool lastEnableStatus;

    private void Update()
    {
        animationTimer += Time.deltaTime;
        stateChangeTimer += Time.deltaTime;

        UpdateAnimationState();
        animStateMachine.Update();

        indicator.SetActive(UnitRegister.instance.enableMap[id]);
        if (lastEnableStatus != indicator.activeInHierarchy)
        {
            IndicatorBatchManager.instance.Clear(
                indicatorSharedMesh,
                indicatorSharedMaterial,
                indicator.transform.localToWorldMatrix
            );
            lastEnableStatus = indicator.activeInHierarchy;
        }
        if (indicator.activeInHierarchy)
        {
            IndicatorBatchManager.instance.Submit(
                indicatorSharedMesh,
                indicatorSharedMaterial,
                indicator.transform.localToWorldMatrix
            );
        }
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<UnitRemoveEvent>(ChangeID);
    }
}
