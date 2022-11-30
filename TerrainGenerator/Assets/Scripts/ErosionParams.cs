using UnityEngine;

[System.Serializable]
public class ErosionParams
{
    [Range(2, 8)] 
    public int erosionRadius = 3;

    [Range(0, 1)] 
    public float inertia = .05f; // At zero, water will instantly change direction to flow downhill. At 1, water will never change direction. 

    public float sedimentCapacityFactor = 4; // Multiplier for how much sediment a droplet can carry

    public float minSedimentCapacity = .01f; // Used to prevent carry capacity getting too close to zero on flatter terrain

    [Range(0, 1)] 
    public float erodeSpeed = .3f;
    
    [Range(0, 1)]
    public float depositSpeed = .3f;
    
    [Range(0, 1)] 
    public float evaporateSpeed = .01f;
    
    public float gravity = 4;
    public int maxDropletLifetime = 30;

    public float initialWaterVolume = 1;
    public float initialSpeed = 1;
}