using System;
using System.Collections.Generic;
using System.Buffers;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

public class InstancedAniManager : MonoBehaviour
{
    public static InstancedAniManager instance;

    public struct InstanceData
    {
        public Matrix4x4 matrix;
        public float frame1, frame2, t;

        public InstanceData(Matrix4x4 matrix, float frame1, float frame2, float t)
        {
            this.matrix = matrix;
            this.frame1 = frame1;
            this.frame2 = frame2;
            this.t = t;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct UnitAnimDataGPU
    {
        public float frame1;
        public float frame2;
        public float t;
        public float _;
        public static int Size => sizeof(float) * 4;
    }

    private int lastClearedFrame = -1;
    private readonly Dictionary<(Mesh, Material), RenderBatch> batches = new();
    private static readonly int BufferPropID = Shader.PropertyToID("_AnimateDataBuffer");

    private void Awake()
    {
        instance = this;
        Application.onBeforeRender += RenderFrame;
    }

    private void OnDestroy()
    {
        Application.onBeforeRender -= RenderFrame;
        foreach (var batch in batches.Values) batch.Release();
        batches.Clear();
        instance = null;
    }

    public void SubmitInstance(Mesh mesh, Material material, in InstanceData data)
    {
        if (Time.frameCount != lastClearedFrame)
        {
            Clear();
            lastClearedFrame = Time.frameCount;
        }

        var key = (mesh, material);
        if (!batches.TryGetValue(key, out var batch))
        {
            batch = new RenderBatch(mesh, material, (int)1.5e4f);
            batches[key] = batch;
        }
        batch.Add(data);
    }

    public void Clear()
    {
        foreach (var batch in batches.Values) batch.Clear();
    }

    private void RenderFrame()
    {
        foreach (var batch in batches.Values)
        {
            if (batch.Count > 0) batch.Draw();
        }
    }

    private class RenderBatch
    {
        public Mesh mesh;
        public Material material;
        public ComputeBuffer buffer;
        public MaterialPropertyBlock mpb;

        private readonly List<InstanceData> instances = new();
        private readonly int capacity;

        public RenderBatch(Mesh mesh, Material material, int capacity)
        {
            this.mesh = mesh;
            this.material = material;
            this.capacity = capacity;
            buffer = new(capacity, UnitAnimDataGPU.Size, ComputeBufferType.Default);
            mpb = new();
            mpb.SetBuffer(BufferPropID, buffer);
        }

        public void Add(in InstanceData data)
        {
            if (instances.Count >= capacity) return;
            instances.Add(data);
        }

        public void Clear() => instances.Clear();
        public int Count => instances.Count;

        public void Draw()
        {
            int count = Count;
            if (count == 0) return;

            const int MaxPerDrawCall = 400;

            for (int offset = 0; offset < count; offset += MaxPerDrawCall)
            {
                int drawCount = Math.Min(count - offset, MaxPerDrawCall);

                var gpuChunk = ArrayPool<UnitAnimDataGPU>.Shared.Rent(drawCount);
                var matChunk = ArrayPool<Matrix4x4>.Shared.Rent(drawCount);

                try
                {
                    for (int i = 0; i < drawCount; i++)
                    {
                        InstanceData src = instances[offset + i];
                        gpuChunk[i] = new UnitAnimDataGPU
                        {
                            frame1 = src.frame1,
                            frame2 = src.frame2,
                            t = src.t,
                            _ = 0
                        };
                        matChunk[i] = src.matrix;
                    }

                    buffer.SetData(gpuChunk, 0, 0, drawCount);
                    Graphics.DrawMeshInstanced(mesh, 0, material, matChunk, drawCount, mpb, ShadowCastingMode.On, true, 0);
                }
                finally
                {
                    ArrayPool<UnitAnimDataGPU>.Shared.Return(gpuChunk);
                    ArrayPool<Matrix4x4>.Shared.Return(matChunk);
                }
            }
        }

        public void Release() => buffer?.Release();
    }
}