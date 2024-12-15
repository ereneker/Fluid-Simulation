using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FluidRaymarching : MonoBehaviour
{
    public FluidSimulation FluidSimulation;
    
    public ComputeShader RaymarchingShader;
    public Light lightSource;
    
    public Camera cam;
    private RenderTexture target;
    
    public float viewRadius = 0.05f;
    public float blendStrength = 0.5f;
    public Color waterColor = Color.blue;
    public Color ambientLight = Color.white;
    
    private bool initialized = false;

    private void Init()
    {
        if (target == null || target.width != cam.pixelWidth || target.height != cam.pixelHeight)
        {
            if (target != null)
            {
                target.Release();
            }

            cam.depthTextureMode = DepthTextureMode.Depth;
            target = new RenderTexture(cam.pixelWidth, cam.pixelHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            target.enableRandomWrite = true;
            target.Create();
        }
    }
    
    public void Begin()
    {
        if (FluidSimulation == null || FluidSimulation.positionBuffer == null)
        {
            Debug.LogWarning("No fluid simulation or position buffer assigned.");
            return;
        }
        
        Init();

        // Set the particle buffer
        RaymarchingShader.SetBuffer(0, "particles", FluidSimulation.positionBuffer);
        RaymarchingShader.SetInt("numParticles", FluidSimulation.positionBuffer.count);

        // Set fluid appearance parameters
        RaymarchingShader.SetFloat("particleRadius", viewRadius);
        RaymarchingShader.SetFloat("blendStrength", blendStrength);
        RaymarchingShader.SetVector("waterColor", waterColor);
        RaymarchingShader.SetVector("_AmbientLight", ambientLight);

        // Optional: Set camera depth texture if your compute uses depth. If not, remove this line.
        RaymarchingShader.SetTextureFromGlobal(0, "_DepthTexture", "_CameraDepthTexture");

        initialized = true;
    }
    
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {

        if (!initialized)
        {
            Begin();
        }

        if (initialized)
        {
            // Update lighting and camera data each frame
            if (lightSource != null)
            {
                RaymarchingShader.SetVector("_Light", lightSource.transform.forward);
            }

            RaymarchingShader.SetTexture(0, "Source", source);
            RaymarchingShader.SetTexture(0, "Destination", target);
            RaymarchingShader.SetVector("_CameraPos", cam.transform.position);
            RaymarchingShader.SetMatrix("_CameraToWorld", cam.cameraToWorldMatrix);
            RaymarchingShader.SetMatrix("_CameraInverseProjection", cam.projectionMatrix.inverse);

            int threadGroupsX = Mathf.CeilToInt(cam.pixelWidth / 8.0f);
            int threadGroupsY = Mathf.CeilToInt(cam.pixelHeight / 8.0f);
            RaymarchingShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

            Graphics.Blit(target, destination);
        }
        else
        {
            // If not initialized, just pass through original image
            Graphics.Blit(source, destination);
        }
    }
}
