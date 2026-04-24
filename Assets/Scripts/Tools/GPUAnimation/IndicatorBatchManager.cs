using System;
using System.Collections.Generic;
using System.Buffers;
using UnityEngine;
using UnityEngine.Rendering;

public class IndicatorBatchManager : MonoBehaviour
{
    public static IndicatorBatchManager instance;

    private readonly Dictionary<(Mesh Mesh, Material Mat), List<Matrix4x4>> batches = new();
    private MaterialPropertyBlock emptyMPB;
    private int lastFrame = -1;

    public void Submit(Mesh mesh, Material material, Matrix4x4 worldMatrix)
    {
        if (Time.frameCount != instance.lastFrame)
        {
            foreach (var l in instance.batches.Values) l.Clear();
            instance.lastFrame = Time.frameCount;
        }

        var key = (mesh, material);
        if (!instance.batches.TryGetValue(key, out var list))
        {
            list = new();
            instance.batches[key] = list;
        }
        list.Add(worldMatrix);
    }

    public void Clear(Mesh mesh, Material material, Matrix4x4 worldMatrix)
    {
        var key = (mesh, material);
        if (instance.batches.TryGetValue(key, out var list))
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (UsefulUtils.Approximately(list[i], worldMatrix))
                {
                    list.RemoveAt(i);
                    break;
                }
            }
        }
    }

    private void RenderBatches()
    {
        foreach (var kvp in batches)
        {
            int count = kvp.Value.Count;
            if (count == 0) continue;

            const int MaxPerDrawCall = 400;

            for (int i = 0; i < count; i += MaxPerDrawCall)
            {
                int drawCount = Math.Min(count - i, MaxPerDrawCall);
                var arr = ArrayPool<Matrix4x4>.Shared.Rent(drawCount);
                try
                {
                    kvp.Value.CopyTo(i, arr, 0, drawCount);
                    Graphics.DrawMeshInstanced(kvp.Key.Mesh, 0, kvp.Key.Mat, arr, drawCount, emptyMPB, ShadowCastingMode.Off, false, 0);
                }
                finally
                {
                    ArrayPool<Matrix4x4>.Shared.Return(arr);
                }
            }
        }
    }

    void Awake()
    {
        instance = this;
        emptyMPB = new();
        Application.onBeforeRender += RenderBatches;
    }

    void OnDestroy()
    {
        Application.onBeforeRender -= RenderBatches;
        instance = null;
    }
}