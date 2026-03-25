using System;
using System.Collections.Generic;
using UnityEngine;

public static class EventBus
{
    public static readonly Dictionary<Type, Delegate> eventTable = new();

    public static void Subscribe<T>(Action<T> action)
    {
        if (action == null) return;

        Type type = typeof(T);
        if (eventTable.TryGetValue(type, out Delegate existingDelegate))
            eventTable[type] = Delegate.Combine(existingDelegate, action);
        else
            eventTable[type] = action;
    }

    public static void Unsubscribe<T>(Action<T> action)
    {
        if (action == null) return;

        Type type = typeof(T);
        if (eventTable.TryGetValue(type, out Delegate existingDelegate))
        {
            Delegate newDelegate = Delegate.Remove(existingDelegate, action);

            if (newDelegate == null)
                eventTable.Remove(type);
            else
                eventTable[type] = newDelegate;
        }
    }

    public static void Publish<T>(T arg)
    {
        Type type = typeof(T);
        if (eventTable.TryGetValue(type, out Delegate handlers))
        {
            foreach (Delegate handler in handlers.GetInvocationList())
            {
                try
                {
                    ((Action<T>)handler).Invoke(arg);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[EventBus] ({typeof(T).Name}): {e.Message}");
                }
            }
        }
    }

    public static void Clear()
    {
        eventTable.Clear();
    }
}
