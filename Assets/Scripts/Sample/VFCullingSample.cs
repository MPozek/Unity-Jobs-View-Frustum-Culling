using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class VFCullingSample : MonoBehaviour
{
    public int NumObjects = 10000;

    public bool DrawOnlyCulled;

    public Camera TargetCamera;

    private NativeArray<float3> _positions;
    private NativeArray<float3> _extents;

    private NativeList<int> _filteredIndices;
    
    // Start is called before the first frame update
    private void Start()
    {
        _positions = new NativeArray<float3>(NumObjects, Allocator.Persistent);
        _extents = new NativeArray<float3>(NumObjects, Allocator.Persistent);

        _filteredIndices = new NativeList<int>(NumObjects, Allocator.Persistent);

        Unity.Mathematics.Random rand = new Unity.Mathematics.Random(123);

        for (int i = 0; i < NumObjects; i++)
        {
            _positions[i] = rand.NextFloat3Direction() * rand.NextFloat(1f, 50f);
            _extents[i] = new float3(0.5f, 0.5f, 0.5f);
        }
    }

    private void OnDestroy()
    {
        _positions.Dispose();
        _extents.Dispose();
        _filteredIndices.Dispose();
    }

    // Update is called once per frame
    private void Update()
    {
        _filteredIndices.Clear();

        VFCulling.ScheduleCullingJob(TargetCamera.cullingMatrix, _positions, _extents, _filteredIndices).Complete();

        
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            if (DrawOnlyCulled)
            {
                for (int i = 0; i < _filteredIndices.Length; i++)
                {
                    int idx = _filteredIndices[i];
                    Gizmos.DrawWireSphere(_positions[idx], 1f);
                }
            }
            else
            {
                for (int i = 0; i < NumObjects; i++)
                {
                    Gizmos.DrawWireSphere(_positions[i], 1f);
                }
            }
        }
    }
}
