using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public static class ViewFrustrumCulling
{
    private static readonly Plane[] _planes = new Plane[6];

    [BurstCompile]
    private struct FilterViewFrustrumCulling : IJobParallelForFilter
    {
        [ReadOnly, DeallocateOnJobCompletion] public NativeArray<float4> FrustrumPlanes;
        [ReadOnly] public NativeArray<float3> Positions;
        [ReadOnly] public NativeArray<float3> Extents;

        public bool Execute(int i)
        {
            for (int j = 0; j < 6; j++)
            {
                float3 planeNormal = FrustrumPlanes[j].xyz;

                float planeConstant = FrustrumPlanes[j].w;

                if (math.dot(Extents[i], math.abs(planeNormal)) +
                    math.dot(planeNormal, Positions[i]) +
                    planeConstant <= 0f)
                    return false;
            }

            return true;
        }
    }

    [BurstCompile]
    private struct FilterViewFrustrumSingleSizeCulling : IJobParallelForFilter
    {
        [ReadOnly, DeallocateOnJobCompletion] public NativeArray<float4> FrustrumPlanes;
        [ReadOnly] public NativeArray<float3> Positions;

        public bool Execute(int i)
        {
            for (int j = 0; j < 6; j++)
            {
                float3 planeNormal = FrustrumPlanes[j].xyz;

                float planeConstant = FrustrumPlanes[j].w;

                if (math.dot(planeNormal, Positions[i]) +
                    planeConstant <= 0f)
                    return false;
            }

            return true;
        }
    }

    private static NativeArray<float4> _frustrumPlanes;

#if UNITY_EDITOR
    private static bool _exitingPlayMode = false;
    private static void DisposeOnQuit(UnityEditor.PlayModeStateChange state)
    {
        if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode)
        {
            _exitingPlayMode = true;
            _frustrumPlanes.Dispose();
            UnityEditor.EditorApplication.playModeStateChanged -= DisposeOnQuit;
        }
        else if (state == UnityEditor.PlayModeStateChange.ExitingEditMode)
        {
            _exitingPlayMode = false;
        }
    }
#endif

    public static void SetFrustrumPlanes(float4x4 worldProjectionMatrix)
    {
        if (_frustrumPlanes.IsCreated == false)
        {
#if UNITY_EDITOR
            if (_exitingPlayMode)
            {
                Debug.LogWarning("Trying to set frustrum planes while exiting play mode?");
                return; // TODO :: warn?
            }

            UnityEditor.EditorApplication.playModeStateChanged += DisposeOnQuit;
#endif

            _frustrumPlanes = new NativeArray<float4>(6, Allocator.Persistent);
        }

        GeometryUtility.CalculateFrustumPlanes(worldProjectionMatrix, _planes);

        for (int i = 0; i < 6; ++i)
        {
            _frustrumPlanes[i] = new float4(_planes[i].normal, _planes[i].distance);
        }
    }

    public static JobHandle ScheduleCullingJob(float4x4 worldProjectionMatrix, NativeArray<float3> positions,
        NativeArray<float3> extents, NativeList<int> outIndices)
    {
        SetFrustrumPlanes(worldProjectionMatrix);
        return ScheduleCullingJob(positions, extents, outIndices);
    }

    public static JobHandle ScheduleCullingJob(NativeArray<float3> positions,
        NativeArray<float3> extents, NativeList<int> outIndices)
    {
        if (!_frustrumPlanes.IsCreated)
            return default(JobHandle);

        return new FilterViewFrustrumCulling
        {
            FrustrumPlanes = new NativeArray<float4>(_frustrumPlanes, Allocator.TempJob),
            Positions = positions,
            Extents = extents
        }.ScheduleAppend(outIndices, positions.Length, 16);
    }

    public static JobHandle ScheduleCullingJob(float4x4 worldProjectionMatrix, NativeArray<float3> positions,
        float3 extents, NativeList<int> outIndices)
    {
        SetFrustrumPlanes(worldProjectionMatrix);
        return ScheduleCullingJob(positions, extents, outIndices);
    }

    public static JobHandle ScheduleCullingJob(NativeArray<float3> positions,
        float3 extents, NativeList<int> outIndices)
    {
        if (!_frustrumPlanes.IsCreated)
            return default(JobHandle);

        // embed the extents into plane constants
        var frustrumPlanes = new NativeArray<float4>(_frustrumPlanes, Allocator.TempJob);

        for (int i = 0; i < 6; i++)
        {
            frustrumPlanes[i] = new float4(frustrumPlanes[i].xyz,
                math.dot(extents, math.abs(frustrumPlanes[i].xyz)) + frustrumPlanes[i].w);
        }

        return new FilterViewFrustrumSingleSizeCulling
        {
            FrustrumPlanes = frustrumPlanes,
            Positions = positions
        }.ScheduleAppend(outIndices, positions.Length, 16);
    }
}