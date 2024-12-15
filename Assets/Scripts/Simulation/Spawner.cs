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
        
        for (int i = 0; i < halfCount; i++)
        {
            float3 jitter = UnityEngine.Random.insideUnitSphere * jitterStrength;
            points[i] = (float3)centre + jitter;  // Spawn around 'centre'
            velocities[i] = initialVel;
        }
        
        
        for (int i = halfCount; i < numPoints; i++)
        {
            float3 jitter = UnityEngine.Random.insideUnitSphere * jitterStrength;
            points[i] = (float3)secondCentre + jitter; // Spawn around 'secondCentre'
            velocities[i] = initialVel;
        }

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
