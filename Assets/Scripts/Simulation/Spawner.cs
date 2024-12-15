using UnityEngine;
using Unity.Mathematics;

public class Spawner : MonoBehaviour
{
    public int numParticlesPerAxis;
    public Vector3 centre;
    public Vector3 secondCentre;
    public float size;
    public float3 initialVel;
    public float jitterStrength;
    public bool showSpawnBounds;
    
    [Header("Info")]
    public int debug_numParticles;

    public SpawnerData GetSpawnData()
    {
        int numPoints = numParticlesPerAxis * numParticlesPerAxis * numParticlesPerAxis;
        int halfCount = numPoints / 2;
        
        float3[] points = new float3[numPoints];
        float3[] velocities = new float3[numPoints];

        //int i = 0;
        
        for (int i = 0; i < halfCount; i++)
        {
            float3 jitter = UnityEngine.Random.insideUnitSphere * jitterStrength;
            points[i] = (float3)centre + jitter;  // Spawn around 'centre'
            velocities[i] = initialVel;
        }
        
        // Second half at centre2
        for (int i = halfCount; i < numPoints; i++)
        {
            float3 jitter = UnityEngine.Random.insideUnitSphere * jitterStrength;
            points[i] = (float3)secondCentre + jitter; // Spawn around 'secondCentre'
            velocities[i] = initialVel;
        }

        //for (int x = 0; x < numParticlesPerAxis; x++)
        //{
        //    for (int y = 0; y < numParticlesPerAxis; y++)
        //    {
        //        for (int z = 0; z < numParticlesPerAxis; z++)
        //        {
        //            float tx = x / (numParticlesPerAxis - 1f);
        //            float ty = y / (numParticlesPerAxis - 1f);
        //            float tz = z / (numParticlesPerAxis - 1f);
        //
        //            float px = (tx - 0.5f) * size + centre.x;
        //            float py = (ty - 0.5f) * size + centre.y;
        //            float pz = (tz - 0.5f) * size + centre.z;
        //            float3 jitter = UnityEngine.Random.insideUnitSphere * jitterStrength;
        //            points[i] = new float3(px, py, pz) + jitter;
        //            velocities[i] = initialVel;
        //            i++;
        //        }
        //    }
        //}

        return new SpawnerData() { points = points, velocities = velocities };
    }
    
    
    private void OnValidate()
    {
        debug_numParticles = numParticlesPerAxis * numParticlesPerAxis * numParticlesPerAxis;
    }

    private void OnDrawGizmos()
    {
        if (showSpawnBounds && !Application.isPlaying)
        {
            Gizmos.color = new Color(1, 1, 0, 0.5f);
            Gizmos.DrawWireSphere(centre, size);
            Gizmos.DrawWireSphere(secondCentre, size);
        }
    }
}
