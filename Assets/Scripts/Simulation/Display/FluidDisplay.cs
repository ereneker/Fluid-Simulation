using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

public class FluidDisplay : MonoBehaviour
{
    public Shader shader;
    public float scale;
    public Color color;
    
    private Mesh _fluidMesh;
    private Material _fluidMat;
    private ComputeBuffer _argsBuffer;
    private Bounds _fluidBounds;
    
    public int gradientResolution;
    public float velocityDisplayMax;
    bool needsUpdate;

    public int meshResolution;
    public int debug_MeshTriCount;
    
    public void Init(FluidSimulation sim)
    {
        _fluidMat = new Material(shader);
        _fluidMat.SetBuffer("Positions", sim.positionBuffer);
        _fluidMat.SetBuffer("Velocities", sim.velocityBuffer);

        _fluidMesh = SphereGenerator.GenerateSphereMesh(meshResolution);
        debug_MeshTriCount = _fluidMesh.triangles.Length / 3;
        _argsBuffer = ComputeBetter.CreateArgsBuffer(_fluidMesh, sim.positionBuffer.count);
        _fluidBounds = new Bounds(Vector3.zero, Vector3.one * 10000);
        
    }
    
    private void LateUpdate()
    {

        UpdateSettings();
        Graphics.DrawMeshInstancedIndirect(_fluidMesh, 0, _fluidMat, _fluidBounds, _argsBuffer);
    }

    private void OnValidate()
    {
        needsUpdate = true;
    }

    private void OnDestroy()
    {
        Release(_argsBuffer);
    }
    
    private void Release(params ComputeBuffer[] buffers)
    {
        for (int i = 0; i < buffers.Length; i++)
        {
            if (buffers[i] != null)
            {
                buffers[i].Release();
            }
        }
    }
    
    private void UpdateSettings()
    {
        if (needsUpdate)
        {
            needsUpdate = false;
        }
        _fluidMat.SetFloat("scale", scale);
        _fluidMat.SetColor("colour", color);
        _fluidMat.SetFloat("velocityMax", velocityDisplayMax);

        Vector3 s = transform.localScale;
        var localToWorld = transform.localToWorldMatrix;
        transform.localScale = s;

        _fluidMat.SetMatrix("localToWorld", localToWorld);
    }
}
