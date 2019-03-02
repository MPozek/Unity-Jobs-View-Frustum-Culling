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

                if (
                    math.dot(Extents[i], math.abs(planeNormal)) + 
                    math.dot(planeNormal, Positions[i]) + 
                    planeConstant <= 0f)
                    return false;
            }

            return true;
        }
    }

    public static JobHandle ScheduleCullingJob(float4x4 worldProjectionMatrix, NativeArray<float3> positions, NativeArray<float3> extents, NativeList<int> outIndices)
    {
        NativeArray<float4> frustrumPlanes = new NativeArray<float4>(6, Allocator.TempJob);
        
        GeometryUtility.CalculateFrustumPlanes(worldProjectionMatrix, _planes);

        for (int i = 0; i < 6; ++i)
        {
            frustrumPlanes[i] = new float4(_planes[i].normal, _planes[i].distance);
        }

        return new FilterViewFrustrumCulling
        {
            FrustrumPlanes = frustrumPlanes,
            Positions = positions,
            Extents = extents
        }.ScheduleAppend(outIndices, positions.Length, 16);
    }
}
