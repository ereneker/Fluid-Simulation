using System;
using UnityEngine;
using Unity.Mathematics;

[Serializable]
public struct SpawnerData 
{
    [field: SerializeField] public float3[] points { get; set; }
    [field: SerializeField] public float3[] velocities { get; set; }
}
