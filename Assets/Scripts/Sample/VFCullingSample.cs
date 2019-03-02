using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class VFCullingSample : MonoBehaviour
{
    public int NumObjects = 10000;

    public bool DrawOnlyCulled;

    public Camera TargetCamera;

    public Material Material;
    public Mesh Mesh;

    private NativeArray<float3> _positions;
    private readonly float3 _extents = new float3(0.25f, 0.25f, 0.25f);

    private NativeList<int> _filteredIndices;

    private IndirectRenderer _renderer;

    // Start is called before the first frame update
    private void Start()
    {
        _positions = new NativeArray<float3>(NumObjects, Allocator.Persistent);

        _filteredIndices = new NativeList<int>(NumObjects, Allocator.Persistent);

        Unity.Mathematics.Random rand = new Unity.Mathematics.Random(123);

        for (int i = 0; i < NumObjects; i++)
        {
            _positions[i] = rand.NextFloat3Direction() * rand.NextFloat(1f, 50f);
        }

        _renderer = new IndirectRenderer(NumObjects, Material, Mesh);
    }

    private void OnDestroy()
    {
        _positions.Dispose();
        _filteredIndices.Dispose();

        _renderer.ReleaseBuffers(true);
    }

    private JobHandle _cullJobHandle;

    // Update is called once per frame
    private void Update()
    {
        if (DrawOnlyCulled)
        {
            _filteredIndices.Clear();

            _cullJobHandle = ViewFrustrumCulling.ScheduleCullingJob(TargetCamera.cullingMatrix, _positions, _extents, _filteredIndices);
        }
    }

    private void LateUpdate()
    {
        // actually draw the meshes
        if (DrawOnlyCulled)
        {
            _cullJobHandle.Complete();

            using (var filteredPositions = new NativeArray<float3>(_filteredIndices.Length, Allocator.TempJob))
            {
                UtilityJobs.ScheduleParallelCopyIndices(_positions, _filteredIndices, filteredPositions).Complete();

                _renderer.Draw(0, filteredPositions.Length, filteredPositions);
            }
        }
        else
        {
            _renderer.Draw(0, _positions.Length, _positions);
        }
    }
}