using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class FluidSimulation : MonoBehaviour
{
    public event System.Action SimulationStepCompleted;

    [Header("Settings")]
    public float timeScale = 1;
    public bool fixedTimeStep;
    public int iterationsPerFrame;
    public float gravity = -10;
    [Range(0, 1)] public float collisionDamping = 0.05f;
    public float smoothingRadius = 0.2f;
    public float targetDensity;
    public float pressureMultiplier;
    public float nearPressureMultiplier;
    public float viscosityStrength;

    [Header("References")]
    public ComputeShader fluidShader;
    public Spawner spawnerObject;
    public FluidDisplay fluidDisplay;
    public Transform floorDisplay;
    
    public ComputeBuffer positionBuffer { get; private set; }
    public ComputeBuffer velocityBuffer { get; private set; }
    public ComputeBuffer densityBuffer { get; private set; }
    public ComputeBuffer futurePositionsBuffer;
    ComputeBuffer spatialIndices;
    ComputeBuffer spatialOffsets;

    private SpawnerData spawnerData;

    private GPUSort gpuSort;
    
    public bool isPaused;
    private bool pauseNextFrame;
    
    private const int externalForcesKernel = 0;
    private const int spatialHashKernel = 1;
    private const int densityKernel = 2;
    private const int pressureKernel = 3;
    private const int viscosityKernel = 4;
    private const int updatePositionsKernel = 5;

    private void Start()
    {
        float deltaTime = 1 / 60f;
        Time.fixedDeltaTime = deltaTime;
        
        spawnerData = spawnerObject.GetSpawnData();
        
        int numParticles = spawnerData.points.Length;
        positionBuffer = new ComputeBuffer(numParticles, System.Runtime.InteropServices.Marshal.SizeOf(typeof(float3)));
        futurePositionsBuffer = new ComputeBuffer(numParticles, System.Runtime.InteropServices.Marshal.SizeOf(typeof(float3)));
        velocityBuffer = new ComputeBuffer(numParticles, System.Runtime.InteropServices.Marshal.SizeOf(typeof(float3)));
        densityBuffer = new ComputeBuffer(numParticles, System.Runtime.InteropServices.Marshal.SizeOf(typeof(float2)));
        spatialIndices = new ComputeBuffer(numParticles, System.Runtime.InteropServices.Marshal.SizeOf(typeof(uint3)));
        spatialOffsets = new ComputeBuffer(numParticles, System.Runtime.InteropServices.Marshal.SizeOf(typeof(uint)));

        SetInitialBufferData(spawnerData);

        SetBuffer(fluidShader, positionBuffer, "Positions", externalForcesKernel, updatePositionsKernel);
        SetBuffer(fluidShader, futurePositionsBuffer, "PredictedPositions", externalForcesKernel, spatialHashKernel, densityKernel, pressureKernel, viscosityKernel, updatePositionsKernel);
        SetBuffer(fluidShader, spatialIndices, "SpatialIndices", spatialHashKernel, densityKernel, pressureKernel, viscosityKernel);
        SetBuffer(fluidShader, spatialOffsets, "SpatialOffsets", spatialHashKernel, densityKernel, pressureKernel, viscosityKernel);
        SetBuffer(fluidShader, densityBuffer, "Densities", densityKernel, pressureKernel, viscosityKernel);
        SetBuffer(fluidShader, velocityBuffer, "Velocities", externalForcesKernel, pressureKernel, viscosityKernel, updatePositionsKernel);

        fluidShader.SetInt("numParticles", positionBuffer.count);

        gpuSort = new();
        gpuSort.SetBuffers(spatialIndices, spatialOffsets);

        //Display
        fluidDisplay.Init(this);
    }

    private void FixedUpdate()
    {
        if (fixedTimeStep)
        {
            RunSimulationFrame(Time.fixedDeltaTime);
        }
    }

    private void Update()
    {
        if (!fixedTimeStep && Time.frameCount > 10)
        {
            RunSimulationFrame(Time.deltaTime);
        }

        if (pauseNextFrame)
        {
            isPaused = true;
            pauseNextFrame = false;
        }
        floorDisplay.transform.localScale = new Vector3(1, 1 / transform.localScale.y * 0.1f, 1);

        HandleInput();
    }
    
    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isPaused = !isPaused;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            isPaused = true;
            SetInitialBufferData(spawnerData);
        }
    }
    private void SetBuffer(ComputeShader compute, ComputeBuffer buffer, string id, params int[] kernels)
    {
        for (int i = 0; i < kernels.Length; i++)
        {
            compute.SetBuffer(kernels[i], id, buffer);
        }
    }

    private void SetInitialBufferData(SpawnerData spawnData)
    {
        float3[] allPoints = new float3[spawnData.points.Length];
        System.Array.Copy(spawnData.points, allPoints, spawnData.points.Length);

        positionBuffer.SetData(allPoints);
        futurePositionsBuffer.SetData(allPoints);
        velocityBuffer.SetData(spawnData.velocities);
    }
    
    private void RunSimulationFrame(float frameTime)
    {
        if (!isPaused)
        {
            float timeStep = frameTime / iterationsPerFrame * timeScale;

            UpdateSettings(timeStep);

            for (int i = 0; i < iterationsPerFrame; i++)
            {
                RunSimulationStep();
                SimulationStepCompleted?.Invoke();
            }
        }
    }
    
    private void UpdateSettings(float deltaTime)
    {
        Vector3 simBoundsSize = transform.localScale;
        Vector3 simBoundsCentre = transform.position;

        fluidShader.SetFloat("deltaTime", deltaTime);
        fluidShader.SetFloat("gravity", gravity);
        fluidShader.SetFloat("collisionDamping", collisionDamping);
        fluidShader.SetFloat("smoothingRadius", smoothingRadius);
        fluidShader.SetFloat("targetDensity", targetDensity);
        fluidShader.SetFloat("pressureMultiplier", pressureMultiplier);
        fluidShader.SetFloat("nearPressureMultiplier", nearPressureMultiplier);
        fluidShader.SetFloat("viscosityStrength", viscosityStrength);
        fluidShader.SetVector("boundsSize", simBoundsSize);
        fluidShader.SetVector("centre", simBoundsCentre);
        
        fluidShader.SetMatrix("localToWorld", transform.localToWorldMatrix);
        fluidShader.SetMatrix("worldToLocal", transform.worldToLocalMatrix);
    }
    
    void RunSimulationStep()
    {
        Dispatch(fluidShader, positionBuffer.count, kernelIndex: externalForcesKernel);
        Dispatch(fluidShader, positionBuffer.count, kernelIndex: spatialHashKernel);
        gpuSort.SortAndCalculateOffsets();
        Dispatch(fluidShader, positionBuffer.count, kernelIndex: densityKernel);
        Dispatch(fluidShader, positionBuffer.count, kernelIndex: pressureKernel);
        Dispatch(fluidShader, positionBuffer.count, kernelIndex: viscosityKernel);
        Dispatch(fluidShader, positionBuffer.count, kernelIndex: updatePositionsKernel);

    }

    private void Dispatch(ComputeShader cs, int numIterationsX, int numIterationsY = 1, int numIterationsZ = 1, int kernelIndex = 0)
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
    
    private void OnDisable()
    {
        positionBuffer.Release();
        positionBuffer.Dispose();
        futurePositionsBuffer.Release();
        futurePositionsBuffer.Dispose();
        velocityBuffer.Release();
        velocityBuffer.Dispose();
        densityBuffer.Release();
        densityBuffer.Dispose();
        spatialIndices.Release();
        spatialIndices.Dispose();
        spatialOffsets.Release();
        spatialOffsets.Dispose();
    }
    
    void OnDrawGizmos()
    {
        // Draw Bounds
        var m = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(0, 1, 1, 1f);
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        Gizmos.matrix = m;

    }
}

