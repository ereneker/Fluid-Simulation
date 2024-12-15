using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ComputeBetter
{
    public static ComputeBuffer CreateArgsBuffer(Mesh mesh, ComputeBuffer appendBuffer)
    {
        var buffer = CreateArgsBuffer(mesh, 0);
        ComputeBuffer.CopyCount(appendBuffer, buffer, sizeof(uint));
        return buffer;
    }

    public static ComputeBuffer CreateArgsBuffer(Mesh mesh, int numInstances)
    {
        const int subMeshIndex = 0;
        uint[] args = new uint[5];
        args[0] = (uint)mesh.GetIndexCount(subMeshIndex);
        args[1] = (uint)numInstances;
        args[2] = (uint)mesh.GetIndexStart(subMeshIndex);
        args[3] = (uint)mesh.GetBaseVertex(subMeshIndex);
        args[4] = 0; // offset

        ComputeBuffer argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);
        return argsBuffer;
    }
}
