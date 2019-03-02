using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public static class ViewFrustumCulling
{
    private static readonly Plane[] _planes = new Plane[6];

    [BurstCompile]
    private struct FilterViewFrustumCulling : IJobParallelForFilter
    {
        [ReadOnly, DeallocateOnJobCompletion] public NativeArray<float4> FrustumPlanes;
        [ReadOnly] public NativeArray<float3> Positions;
        [ReadOnly] public NativeArray<float3> Extents;

        public bool Execute(int i)
        {
            for (int j = 0; j < 6; j++)
            {
                float3 planeNormal = FrustumPlanes[j].xyz;

                float planeConstant = FrustumPlanes[j].w;

                if (math.dot(Extents[i], math.abs(planeNormal)) +
                    math.dot(planeNormal, Positions[i]) +
                    planeConstant <= 0f)
                    return false;
            }

            return true;
        }
    }

    [BurstCompile]
    private struct FilterViewFrustumSingleSizeCulling : IJobParallelForFilter
    {
        [ReadOnly, DeallocateOnJobCompletion] public NativeArray<float4> FrustumPlanes;
        [ReadOnly] public NativeArray<float3> Positions;

        public bool Execute(int i)
        {
            for (int j = 0; j < 6; j++)
            {
                float3 planeNormal = FrustumPlanes[j].xyz;

                float planeConstant = FrustumPlanes[j].w;

                if (math.dot(planeNormal, Positions[i]) +
                    planeConstant <= 0f)
                    return false;
            }

            return true;
        }
    }

    private static NativeArray<float4> _frustumPlanes;

#if UNITY_EDITOR
    private static bool _exitingPlayMode = false;
    private static void DisposeOnQuit(UnityEditor.PlayModeStateChange state)
    {
        if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode)
        {
            _exitingPlayMode = true;
            _frustumPlanes.Dispose();
            UnityEditor.EditorApplication.playModeStateChanged -= DisposeOnQuit;
        }
        else if (state == UnityEditor.PlayModeStateChange.ExitingEditMode)
        {
            _exitingPlayMode = false;
        }
    }
#endif

    public static void SetFrustumPlanes(float4x4 worldProjectionMatrix)
    {
        if (_frustumPlanes.IsCreated == false)
        {
#if UNITY_EDITOR
            if (_exitingPlayMode)
            {
                Debug.LogWarning("Trying to set frustum planes while exiting play mode?");
                return; // TODO :: warn?
            }

            UnityEditor.EditorApplication.playModeStateChanged += DisposeOnQuit;
#endif

            _frustumPlanes = new NativeArray<float4>(6, Allocator.Persistent);
        }

        GeometryUtility.CalculateFrustumPlanes(worldProjectionMatrix, _planes);

        for (int i = 0; i < 6; ++i)
        {
            _frustumPlanes[i] = new float4(_planes[i].normal, _planes[i].distance);
        }
    }

    public static JobHandle ScheduleCullingJob(float4x4 worldProjectionMatrix, NativeArray<float3> positions,
        NativeArray<float3> extents, NativeList<int> outIndices)
    {
        SetFrustumPlanes(worldProjectionMatrix);
        return ScheduleCullingJob(positions, extents, outIndices);
    }

    public static JobHandle ScheduleCullingJob(NativeArray<float3> positions,
        NativeArray<float3> extents, NativeList<int> outIndices)
    {
        if (!_frustumPlanes.IsCreated)
            return default(JobHandle);

        return new FilterViewFrustumCulling
        {
            FrustumPlanes = new NativeArray<float4>(_frustumPlanes, Allocator.TempJob),
            Positions = positions,
            Extents = extents
        }.ScheduleAppend(outIndices, positions.Length, 16);
    }

    public static JobHandle ScheduleCullingJob(float4x4 worldProjectionMatrix, NativeArray<float3> positions,
        float3 extents, NativeList<int> outIndices)
    {
        SetFrustumPlanes(worldProjectionMatrix);
        return ScheduleCullingJob(positions, extents, outIndices);
    }

    public static JobHandle ScheduleCullingJob(NativeArray<float3> positions,
        float3 extents, NativeList<int> outIndices)
    {
        if (!_frustumPlanes.IsCreated)
            return default(JobHandle);

        // embed the extents into plane constants
        var frustumPlanes = new NativeArray<float4>(_frustumPlanes, Allocator.TempJob);

        for (int i = 0; i < 6; i++)
        {
            frustumPlanes[i] = new float4(frustumPlanes[i].xyz,
                math.dot(extents, math.abs(frustumPlanes[i].xyz)) + frustumPlanes[i].w);
        }

        return new FilterViewFrustumSingleSizeCulling
        {
            FrustumPlanes = frustumPlanes,
            Positions = positions
        }.ScheduleAppend(outIndices, positions.Length, 16);
    }
}