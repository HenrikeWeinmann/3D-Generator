using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise
{
    public enum NormalizeMode
    {
        Local,
        Global
    };
    public static float[,] GenerateNoiseMap(
        int mapWidth, 
        int mapHeight, 
        int seed, 
        float scale, 
        int octaves, 
        float persistance, 
        float lacunarity, 
        Vector2 offset,
        NormalizeMode normalizeMode) 
    {
        float[,] noiseMap = new float[mapWidth, mapHeight];

        System.Random prng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];

        float maxPossibleHeight = 0;
        float amplitude = 1;
        float frequency = 1;
        
        for (var i = 0; i < octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000) - offset.x;
            float offsetY = prng.Next(-100000, 100000) - offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);

            maxPossibleHeight += amplitude;
            amplitude *= persistance;
        }

        // Prevent division by zero
        if (scale <= 0)
        {
            scale = 0.00001f;
        }

        float maxLocalNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue;

        float halfWidth = mapWidth / 2f;
        float halfHeight = mapHeight / 2f;

        for (var y = 0; y < mapHeight; y++) 
        {
            for (var x = 0; x < mapWidth; x++) 
            {
                amplitude = 1;
                frequency = 1;
                float noiseHeight = 0;

                // combine octaves of different frequency and amplitude
                for (var i = 0; i < octaves; i++)
                {
                    float sampleX = (x - halfWidth + octaveOffsets[i].x) / scale * frequency;
                    float sampleY = (y - halfHeight + octaveOffsets[i].y) / scale * frequency;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    noiseHeight += perlinValue * amplitude;

                    // decrease amplitude with increasing octave given a persistance in [0, 1]
                    amplitude *= persistance;
                    // increase frequency with increasing octave given a lacunarity > 1
                    frequency *= lacunarity;
                }

                maxLocalNoiseHeight = noiseHeight > maxLocalNoiseHeight ? noiseHeight : maxLocalNoiseHeight;
                minLocalNoiseHeight = noiseHeight < minLocalNoiseHeight ? noiseHeight : minLocalNoiseHeight;

                noiseMap[x, y] = noiseHeight;
            }
        }

        // normalize noise values to [0, 1]
        for (var y = 0; y < mapHeight; y++) 
        {
            for (var x = 0; x < mapWidth; x++) 
            {
                if (normalizeMode == NormalizeMode.Local)
                {
                    noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
                }
                else
                {
                    float normalizedHeight = (noiseMap[x, y] + 1) / (2f * maxPossibleHeight);
                    noiseMap[x, y] = normalizedHeight;
                }
            }
        }

        return noiseMap;
    }
}
