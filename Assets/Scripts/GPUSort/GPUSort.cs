using UnityEngine;
using static UnityEngine.Mathf;

public class GPUSort
{
    private const int sortKernel = 0;
    private const int calculateOffsetsKernel = 1;
    
    private readonly ComputeShader sortCompute;
    private ComputeBuffer _computeBuffer;
    
    public GPUSort()
    {
        sortCompute = Resources.Load<ComputeShader>("BitonicMergeSort");
    }
    
    public void SetBuffers(ComputeBuffer indexBuffer, ComputeBuffer offsetBuffer)
    {
        this._computeBuffer = indexBuffer;

        sortCompute.SetBuffer(sortKernel, "Entries", indexBuffer);
        SetBuffer(sortCompute, offsetBuffer, "Offsets", calculateOffsetsKernel);
        SetBuffer(sortCompute, indexBuffer, "Entries", calculateOffsetsKernel);
    }
    
    public void Sort()
    {
        sortCompute.SetInt("numEntries", _computeBuffer.count);

        // Launch each step of the sorting algorithm (once the previous step is complete)
        // Number of steps = [log2(n) * (log2(n) + 1)] / 2
        // where n = nearest power of 2 that is greater or equal to the number of inputs
        int numStages = (int)Log(NextPowerOfTwo(_computeBuffer.count), 2);

        for (int stageIndex = 0; stageIndex < numStages; stageIndex++)
        {
            for (int stepIndex = 0; stepIndex < stageIndex + 1; stepIndex++)
            {
                // Calculate some pattern stuff
                int groupWidth = 1 << (stageIndex - stepIndex);
                int groupHeight = 2 * groupWidth - 1;
                sortCompute.SetInt("groupWidth", groupWidth);
                sortCompute.SetInt("groupHeight", groupHeight);
                sortCompute.SetInt("stepIndex", stepIndex);
                // Run the sorting step on the GPU
                ComputeDispatch(sortCompute, NextPowerOfTwo(_computeBuffer.count) / 2);
            }
        }
    }


    public void SortAndCalculateOffsets()
    {
        Sort();

        ComputeDispatch(sortCompute, _computeBuffer.count, kernelIndex: calculateOffsetsKernel);
    }

    private void ComputeDispatch(ComputeShader cs, int numIterationsX, int numIterationsY = 1, int numIterationsZ = 1,
        int kernelIndex = 0)
    {
        Vector3Int threadGroupSizes = GetThreadGroupSizes(cs, kernelIndex);
        int numGroupsX = Mathf.CeilToInt(numIterationsX / (float)threadGroupSizes.x);
        int numGroupsY = Mathf.CeilToInt(numIterationsY / (float)threadGroupSizes.y);
        int numGroupsZ = Mathf.CeilToInt(numIterationsZ / (float)threadGroupSizes.y);
        cs.Dispatch(kernelIndex, numGroupsX, numGroupsY, numGroupsZ);
    }
    
    private Vector3Int GetThreadGroupSizes(ComputeShader compute, int kernelIndex = 0)
    {
        uint x, y, z;
        compute.GetKernelThreadGroupSizes(kernelIndex, out x, out y, out z);
        return new Vector3Int((int)x, (int)y, (int)z);
    }
    
    private void SetBuffer(ComputeShader compute, ComputeBuffer buffer, string id, params int[] kernels)
    {
        for (int i = 0; i < kernels.Length; i++)
        {
            compute.SetBuffer(kernels[i], id, buffer);
        }
    }
}
