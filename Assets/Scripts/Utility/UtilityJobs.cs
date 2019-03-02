using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;

public static class UtilityJobs
{
    private struct CopyIndicesJob<T> : IJobParallelFor where T : struct
    {
        [ReadOnly] public NativeArray<T> From;
        [ReadOnly] public NativeList<int> Indices;
        [WriteOnly] public NativeArray<T> To;

        public void Execute(int i)
        {
            int fromIdx = Indices[i];
            To[i] = From[fromIdx];
        }
    }

    public static JobHandle ScheduleParallelCopyIndices<T>(NativeArray<T> from, NativeList<int> indices, NativeArray<T> to) where T : struct
    {
        return new CopyIndicesJob<T>
        {
            From = from,
            To = to,
            Indices = indices
        }.Schedule(indices.Length, 64);
    }

}