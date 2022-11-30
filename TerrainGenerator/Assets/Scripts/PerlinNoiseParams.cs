using UnityEngine;
[System.Serializable]
public class PerlinNoiseParams
{
    [Range(1, 15)]
    public int octaves = 9;
    
    [Range(0, 1)]
    public float persistance = 0.37f;
    
    [Range(0, 10)]
    public float lacunarity = 3.09f;
}