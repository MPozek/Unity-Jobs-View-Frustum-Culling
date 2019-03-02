using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class IndirectRenderer
{
    private ComputeBuffer _argsBuffer;
    private ComputeBuffer _transformBuffer;
    
    // Projectile Details
    private readonly Mesh _mesh;
    private readonly Material _material;

    // DrawMeshInstancedIndirect requires an args buffer
    // [index count per instance, instance count, start index location, base vertex location, start instance location]
    private readonly uint[] _args = { 0, 0, 0, 0, 0 };

    private MaterialPropertyBlock _materialPropertyBlock = new MaterialPropertyBlock();

    public IndirectRenderer(int maxObjects, Material material, Mesh mesh)
    {
        _mesh = mesh;
        _material = material;

        _argsBuffer = new ComputeBuffer(1, _args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        InitializeBuffers(maxObjects);
    }

    void InitializeBuffers(int maxObjects)
    {
        ReleaseBuffers(false);

        // Create Compute Buffers
        _transformBuffer = new ComputeBuffer(maxObjects, sizeof(float) * 3);

        // Set the active buffers on the material
        _material.SetBuffer("positionBuffer", _transformBuffer);

        // Update argument buffer
        uint numIndices = (_mesh != null) ? (uint)_mesh.GetIndexCount(0) : 0;
        _args[0] = numIndices;
        _args[1] = (uint)maxObjects;
        _argsBuffer.SetData(_args);
    }

    public void Draw(int startIndex, int endIndex, NativeArray<float3> positionData)
    {
        // Update our compute buffers with latest data 
        _transformBuffer.SetData(positionData, startIndex, startIndex, endIndex - startIndex);

        _args[1] = (uint)(endIndex - startIndex);
        _argsBuffer.SetData(_args);

        // Instruct the GPU to draw
        Graphics.DrawMeshInstancedIndirect(_mesh, 0, _material, new Bounds(Vector3.zero, new Vector3(100.0f, 100.0f, 100.0f)), _argsBuffer, 0, _materialPropertyBlock, UnityEngine.Rendering.ShadowCastingMode.Off, false);
    }

    // Cleanup
    public void ReleaseBuffers(bool releaseArgs)
    {
        _transformBuffer?.Release();

        _transformBuffer = null;
        
        if (releaseArgs)
        {
            _argsBuffer?.Release();
            _argsBuffer = null;
        }
    }


}
