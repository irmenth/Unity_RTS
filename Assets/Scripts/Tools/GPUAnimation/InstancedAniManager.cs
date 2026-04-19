using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class InstancedAniManager : MonoBehaviour
{
    [SerializeField] private Mesh unitMesh;
    [SerializeField] private Material sharedMaterial;
    [SerializeField] private int maxInstances;

    private static ComputeBuffer animBuffer;
    private static MaterialPropertyBlock sharedMPB;
    private static readonly int bufferPropID = Shader.PropertyToID("_UnitAnimBuffer");

    private struct InstanceInfo
    {
        public Matrix4x4 matrix;
        public float frame1, frame2, t;
    }
    private readonly List<InstanceInfo> instances = new();

    [StructLayout(LayoutKind.Sequential)]
    public struct UnitAnimDataGPU
    {
        public float frame1;
        public float frame2;
        public float t;
        public float _;

        public static int Size => sizeof(float) * 4;
    }

    private void UpdateBuffer()
    {
        if (instances.Count == 0) return;

        var gpuData = new UnitAnimDataGPU[instances.Count];
        for (int i = 0; i < instances.Count; i++)
        {
            gpuData[i] = new UnitAnimDataGPU
            {
                frame1 = instances[i].frame1,
                frame2 = instances[i].frame2,
                t = instances[i].t,
                _ = 0
            };
        }

        animBuffer.SetData(gpuData);
    }

    public void AddInstance(Matrix4x4 matrix, float frame1, float frame2, float t)
    {
        instances.Add(new InstanceInfo
        {
            matrix = matrix,
            frame1 = frame1,
            frame2 = frame2,
            t = t
        });

        UpdateBuffer();
    }

    private void Awake()
    {
        if (sharedMPB == null)
        {
            sharedMPB = new();
            animBuffer = new ComputeBuffer(maxInstances, UnitAnimDataGPU.Size);
            sharedMPB.SetBuffer(bufferPropID, animBuffer);
        }
    }

    private void OnRenderObject()
    {
        if (instances.Count == 0) return;

        var matrices = new Matrix4x4[instances.Count];
        for (int i = 0; i < instances.Count; i++)
        {
            matrices[i] = instances[i].matrix;
        }

        Graphics.DrawMeshInstanced(
            unitMesh,
            0,
            sharedMaterial,
            matrices,
            matrices.Length,
            sharedMPB
        );
    }

    void OnDestroy()
    {
        animBuffer?.Release();
        animBuffer = null;
    }
}
