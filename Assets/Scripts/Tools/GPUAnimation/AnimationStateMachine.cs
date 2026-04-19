using System;
using System.Collections.Generic;

public enum AnimationState
{
    Idle,
    Walk,
    Attack,
}

public class AnimationStateMachine
{
    public AnimationState curState;
    private readonly List<(AnimationState from, AnimationState to)> stateTable = new();
    private readonly List<Func<bool>> stateConditions = new();
    private readonly Action<AnimationState, bool> onStateStart;
    private readonly Action<AnimationState> onStateUpdate;

    public AnimationStateMachine(AnimationState curState, Action<AnimationState, bool> onStateStart, Action<AnimationState> onStateUpdate)
    {
        this.curState = curState;
        this.onStateStart = onStateStart;
        this.onStateUpdate = onStateUpdate;

        onStateStart?.Invoke(curState, false);
    }

    public void AddTransition(AnimationState from, AnimationState to, Func<bool> condition)
    {
        if (stateTable.Contains((from, to))) return;

        stateTable.Add((from, to));
        stateConditions.Add(condition);
    }

    public void Update()
    {
        for (int i = 0; i < stateTable.Count; i++)
        {
            if (stateTable[i].from == curState && stateConditions[i]())
            {
                onStateStart?.Invoke(stateTable[i].to, true);
                break;
            }
        }
        onStateUpdate?.Invoke(curState);
    }
}
