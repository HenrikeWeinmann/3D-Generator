using UnityEngine;
[System.Serializable]
public class PerlinNoiseParams
{
    [Range(1, 15)]
    public int octaves = 5;
    
    [Range(0, 1)]
    public float persistance = 0.411f;
    
    [Range(0, 10)]
    public float lacunarity = 2.51f;
}