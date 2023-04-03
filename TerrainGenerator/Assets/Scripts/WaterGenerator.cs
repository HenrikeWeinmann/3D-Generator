using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Random = System.Random;

public class WaterGenerator : MonoBehaviour {
    public Terrain terrain;
    public Terrain waterTerrain;

    public GameObject waterMapPlane;

    private TerrainGenerator terrainGenerator;
    private System.Random random ;

    private float[,] waterHeightMap;

    private int width;
    private int height;

    
    [Range(1,100000)] public int seed = 12345;
    [Range(0, 1)] public float seaLevel = 0.4f;
    [Range(0, 1)] public float maxRiverHeight = 0.8f;

    [Range(1, 100)] public int amountRivers;


    [Range(1, 10)] public int riverWidth = 1;


    public void Reset() {
        terrainGenerator = terrain.GetComponent<TerrainGenerator>();
        float[,] map = ConvertTo2D(terrainGenerator.GetHeightMap());
        height = terrainGenerator.GetHeight();
        width = terrainGenerator.GetWidth();
        
        float[,] water = new float[height, width];
        
        // for (int x = 0; x < height; x++) {
        //     for (int y = 0; y < width; y++) {
        //         water[x, y] = 0;
        //     }
        // }
        waterHeightMap = water;
        SetWaterTerrain(water);
    }

    public void GenerateRiver() {
        random = new Random(seed);
        terrainGenerator = terrain.GetComponent<TerrainGenerator>();
        float[] heightMap = terrainGenerator.GetHeightMap();
        height = terrainGenerator.GetHeight();
        width = terrainGenerator.GetWidth();

        if (maxRiverHeight <= seaLevel) {
            Debug.Log("River max height must be above sea level");
            return;
        }

        float[,] map = ConvertTo2D(heightMap);
        // int[] topCoords = RandomCoordinateAboveThreshold(map, 0.6f);
        // int[] lowCoords = RandomCoordinateBelowThreshold(map, 0.4f);
        // float[,] shortestPath = GenerateWaterMap();
        // float[,] shortestPath = ShortestPath.FindShortestPath(map, topCoords, lowCoords);
        // shortestPath = MakeBigger(shortestPath, 4);

        // shortestPath= BilateralFilter(shortestPath, 5,100, 30);
        float[,] water = new float[height, width];
        for (int i = 0; i < amountRivers; i++) {
            DrawRandomRiver(water, map);
        }

        if (riverWidth > 1) {
            water = MakeBigger(water, riverWidth);
        }
        // GaussianBlur(water, 3, 5);

        for (int x = 0; x < map.GetLength(0); x++) {
            for (int y = 0; y < map.GetLength(1); y++) {
                if (map[x, y] < seaLevel) {
                    water[x, y] = seaLevel;
                }
                else if (water[x, y] > 0) {
                    water[x, y] = map[x, y];
                    map[x, y] -= 0.01f * water[x, y];
                    if (map[x, y] < 0) {
                        map[x, y] = 0;
                    }
                }
            }
        }

        waterHeightMap = water;
        SetWaterTerrain(water);
        terrainGenerator.SetTerrainData(map);
    }


    private void DrawRandomRiver(float[,] water, float[,] heightMap) {
        int[] point = RandomCoordinateBetweenThresholds(heightMap, seaLevel, maxRiverHeight);
        CreateRiver(water, heightMap, point);
    }

    private void CreateRiver(float[,] water, float[,] heightMap, int[] from) 
    {
        int x = from[0];
        int y = from[1];
        float currentHeight = heightMap[x, y];
        water[x, y] = 1;
        int iteration = 0;
        while (currentHeight > seaLevel && iteration < 100000) {
            iteration++;
            float maxSlope = float.MinValue;
            int[] next = {x, y};
            bool foundNext = false;
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if (i == 0 && j == 0) continue;
                    int xx = x + i;
                    int yy = y + j;
                    if (xx < 0 || xx >= heightMap.GetLength(0) || yy < 0 || yy >= heightMap.GetLength(1)) continue;
                    float slope = heightMap[xx, yy] - currentHeight;
                    if (slope > maxSlope)
                    {
                        maxSlope = slope;
                        next[0] = xx;
                        next[1] = yy;
                        foundNext = true;
                    }
                }
            }
            if (!foundNext) break;
            x = next[0];
            y = next[1];
            currentHeight = heightMap[x, y];
            water[x, y] = 1;
        }
    }

    

    public int[] RandomCoordinateBetweenThresholds(float[,] map, float min, float max) {
        List<int[]> coordinates = new List<int[]>();
        int rows = map.GetLength(0);
        int columns = map.GetLength(1);
        for (int i = 0; i < rows; i++) {
            for (int j = 0; j < columns; j++) {
                if (map[i, j] > min && map[i,j] < max) {
                    coordinates.Add(new int[] { i, j });
                }
            }
        }

        if (coordinates.Count == 0) {
            return null;
        }

        int randomIndex = random.Next(coordinates.Count);
        return coordinates[randomIndex];
    }


    private Texture2D GenerateWaterMapTexture(float[,] map) {
        int height = map.GetLength(0);
        int width = map.GetLength(1);
        Texture2D texture = new Texture2D(width, height);

        for (int x = 0; x < height; x++) {
            for (int y = 0; y < width; y++) {
                float v = map[x, y];
                Color color = new Color(v, v, v);
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        return texture;
    }

    private float[,] ConvertTo2D(float[] map) {
        float[,] map2d = new float[height, width];
        int index = 0;
        for (int x = 0; x < height; x++) {
            for (int y = 0; y < width; y++) {
                map2d[x, y] = map[index];
                index++;
            }
        }

        return map2d;
    }

    private void SetWaterTerrain(float[,] heights) {
        int depth = terrainGenerator.GetDepth();
        var terrainData = waterTerrain.terrainData;
        terrainData.heightmapResolution = width + 1;
        terrainData.size = new Vector3(height, depth, width);
        terrainData.SetHeights(0, 0, heights);
        Texture2D texture = GenerateWaterMapTexture(heights);
        this.waterMapPlane.GetComponent<Renderer>().material.SetTexture("_BaseMap", texture);
    }

    private float[,] MakeBigger(float[,] map, int size = 2) {
        int rows = map.GetLength(0);
        int cols = map.GetLength(1);

        float[,] newMap = new float[map.GetLength(0), map.GetLength(1)];
        for (int x = 0; x < rows; x++) {
            for (int y = 0; y < cols; y++) {
                if (map[x, y] < 1)
                    continue;
                for (int i = -size; i <= size; i++) {
                    for (int j = -size; j <= size; j++) {
                        int nX = x + i;
                        int nY = y + j;
                        if (nX < 0 || nX >= rows || nY < 0 || nY >= cols)
                            continue;
                        newMap[nX, nY] = 1;
                    }
                }
            }
        }

        return newMap;
    }


    public float[,] GetWaterHeightMap() {
        return waterHeightMap;
    }

    public void GaussianBlur(float[,] map, int kernelWidth = 3, float sigma = 1f) {
        float[,] kernel = GaussianKernel(kernelWidth, sigma);
        // Debug.Log($"{heightMap.Length} Länge");
        int radius = (int)Mathf.Floor(kernelWidth / 2f);

        for (int x = 0; x < height; x++) {
            for (int y = 0; y < width; y++) {
                int x0 = x - radius;
                int y0 = y - radius;
                int xStart = Math.Max(0, x0);
                int yStart = Math.Max(0, y0);
                int xEnd = Math.Min(height, x0 + kernelWidth);
                int yEnd = Math.Min(width, y0 + kernelWidth);

                float sum = 0;
                for (int i = xStart; i < xEnd; i++) {
                    for (int j = yStart; j < yEnd; j++) {
                        // Debug.Log($"{i} * width + {j}\t[{i - x + 1},{j - y + 1}]");
                        sum += map[i, j] * kernel[i - x + 1, j - y + 1];
                    }
                }


                map[x, y] = sum;
            }
        }
    }

    private static float[,] GaussianKernel(int width, float weight) {
        float[,] kernel = new float[width, width];
        float kernelSum = 0;
        int foff = (width - 1) / 2;
        float distance = 0;
        float constant = (float)(1f / (2 * Math.PI * weight * weight));
        for (int y = -foff; y <= foff; y++) {
            for (int x = -foff; x <= foff; x++) {
                distance = ((y * y) + (x * x)) / (2 * weight * weight);
                kernel[y + foff, x + foff] = (float)(constant * Math.Exp(-distance));
                kernelSum += kernel[y + foff, x + foff];
            }
        }

        for (int y = 0; y < width; y++) {
            for (int x = 0; x < width; x++) {
                kernel[y, x] = kernel[y, x] * 1f / kernelSum;
            }
        }

        return kernel;
    }
}