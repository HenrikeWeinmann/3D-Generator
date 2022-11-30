using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour {
    public int depth = 150;
    private int width = 512;
    private int height = 512;

    private Terrain terrain;
    private float[] heightMap;

    public PerlinNoiseParams perlinNoiseParams;
    public ErosionParams erosionParams;

    private System.Random random;

    private int counter = 0;

    void Start() {
        terrain = GetComponent<Terrain>();
        random = new System.Random(1234);
        terrain.terrainData = GenerateTerrain(terrain.terrainData);
    }

    void Update() {
        int erosionIterationCount = 400_000;
        int stepSize = 1_000;
        
        if (counter < erosionIterationCount) {
            for (int i = 0; i < stepSize; i++) {
                Erode();
            }
            terrain.terrainData.SetHeights(0, 0, ConvertTo2D(heightMap));
            counter += stepSize;
        }
    }

    TerrainData GenerateTerrain(TerrainData terrainData) {
        terrainData.heightmapResolution = width + 1;
        terrainData.size = new Vector3(height, depth, width);

        heightMap = GeneratePerlinNoiseMap();
        

        terrainData.SetHeights(0, 0, ConvertTo2D(heightMap));
        return terrainData;
    }

    void OnGUI() {
        //render the heightmap as texture onto ui element
        GUI.DrawTexture(new Rect(0, 0, 200, 200), GenerateHeightMapTexture());
    }

    private void Erode() {
        //create water at random spot
        float posX = random.Next(1, height - 1);
        float posY = random.Next(1, width - 1);

        float dirX = 0;
        float dirY = 0;

        float speed = erosionParams.initialSpeed;
        float water = erosionParams.initialWaterVolume;
        float sediment = 0;

        for (int lifetime = 0; lifetime < erosionParams.maxDropletLifetime; lifetime++) {
            int dropletIndex = (int)posX * width + (int)posY;
            float currentHeight = heightMap[dropletIndex];

            int nodeX = (int) posX;
            int nodeY = (int) posY;
            float cellOffsetX = posX - nodeX;
            float cellOffsetY = posY - nodeY;
            
            //calculate direction of gradient
            float[] heightAndGradient = CalculateHeightAndGradient((int)posX, (int)posY);
            float oldHeight = heightAndGradient[0];
            float gradientX = heightAndGradient[1];
            float gradientY = heightAndGradient[2];

            dirX = (dirX * erosionParams.inertia - gradientX * (1 - erosionParams.inertia));
            dirY = (dirY * erosionParams.inertia - gradientY * (1 - erosionParams.inertia));

            // Normalize direction
            float len = Mathf.Sqrt(dirX * dirX + dirY * dirY);
            if (len != 0) {
                dirX /= len;
                dirY /= len;
            }

            //step in direction
            posX += dirX;
            posY += dirY;

            // Stop simulating water if it's not moving or has flown over map edges
            if ((dirX == 0 && dirY == 0) || posX < 0 || posX >= width - 1 || posY < 0 || posY >= height - 1) {
                break;
            }

            // Calculate drop's new height and calculate the height difference
            float newHeight = CalculateHeightAndGradient((int)posX, (int)posY)[0];
            float deltaHeight = newHeight - oldHeight;

            // Calculate the drop sediment capacity (higher when moving fast down a slope and contains lots of water)
            float sedimentCapacity = Mathf.Max(-deltaHeight * speed * water * erosionParams.sedimentCapacityFactor,
                erosionParams.minSedimentCapacity);

            //decide whether to erode or deposit
            if (sediment > sedimentCapacity || deltaHeight > 0) {
                
                // If moving uphill (deltaHeight > 0) try fill up to the current height,
                // otherwise deposit a fraction of the excess sediment
                float amountToDeposit = (deltaHeight > 0)
                    ? Mathf.Min(deltaHeight, sediment)
                    : (sediment - sedimentCapacity) * erosionParams.depositSpeed;
                sediment -= amountToDeposit;

                //deposit around neighbour pixels
                heightMap[dropletIndex] += amountToDeposit * (1 - cellOffsetX) * (1 - cellOffsetY);
                heightMap[dropletIndex + 1] += amountToDeposit * cellOffsetX * (1 - cellOffsetY);
                heightMap[dropletIndex + width] += amountToDeposit * (1 - cellOffsetX) * cellOffsetY;
                heightMap[dropletIndex + width + 1] += amountToDeposit * cellOffsetX * cellOffsetY;
            }
            else {
                // Erode a fraction of the drop's current carry capacity
                // Clamp the erosion to the change in height so that it
                // doesn't dig a hole in the terrain behind the droplet
                float amountToErode = Mathf.Min((sedimentCapacity - sediment) * erosionParams.erodeSpeed, -deltaHeight);
                sediment += amountToErode;

                //TODO erode in a bilinear way around neighbour points inside a radius
                heightMap[dropletIndex] -= amountToErode;
            }

            speed = Mathf.Sqrt(speed * speed + deltaHeight * erosionParams.gravity);
            water *= (1 - erosionParams.evaporateSpeed);
        }

    }

    private float[] CalculateHeightAndGradient(int posX, int posY) {
        int coordX = (int)posX;
        int coordY = (int)posY;

        // Calculate droplet's offset inside the cell (0,0) = at NW node, (1,1) = at SE node
        float x = posX - coordX;
        float y = posY - coordY;

        int waterIndex = posX * width + posY;
        float current = heightMap[waterIndex];
        float south = heightMap[waterIndex + 1];
        float east = heightMap[waterIndex + width];
        float southEast = heightMap[waterIndex + width + 1];

        // Debug.Log($"Pixel-Value:{current}");
        // Debug.Log($"EAST-Value:{east}");
        // Debug.Log($"SOUTH-Value:{south}");
        // Debug.Log($"SOUTHEAST-Value:{southEast}");

        // Calculate droplet's direction of flow with bilinear interpolation of height difference along the edges
        float gradientX = (east - current) * (1 - y) + (southEast - south) * y;
        float gradientY = (south - current) * (1 - x) + (southEast - east) * x;

        // Calculate height with bilinear interpolation of the heights of the nodes of the cell
        float height = current * (1 - x) * (1 - y) + east * x * (1 - y) + south * (1 - x) * y + southEast * x * y;

        // Debug.Log($"Gradient: [{gradientX}/{gradientY}], height:{height}");
        return new float[] { height, gradientX, gradientY };
    }

    private float[] GeneratePerlinNoiseMap() {
        int octaves = perlinNoiseParams.octaves;
        float persistance = perlinNoiseParams.persistance;
        float lacunarity = perlinNoiseParams.lacunarity;

        float[] heights = new float[height * width];

        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

        for (int x = 0; x < height; x++) {
            for (int y = 0; y < width; y++) {
                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;

                for (int i = 0; i < octaves; i++) {
                    float xNormalized = (float)x / height * frequency;
                    float yNormalized = (float)y / width * frequency;

                    float perlinValue = Mathf.PerlinNoise(xNormalized, yNormalized) * 2 - 1;
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

                if (noiseHeight > maxNoiseHeight) {
                    maxNoiseHeight = noiseHeight;
                }
                else if (noiseHeight < minNoiseHeight) {
                    minNoiseHeight = noiseHeight;
                }

                heights[x * width + y] = noiseHeight;
            }
        }

        for (int x = 0; x < height; x++) {
            for (int y = 0; y < width; y++) {
                heights[x * height + y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, heights[x * width + y]);
            }
        }

        return heights;
    }


    private float[,] ConvertTo2D(float[] map) {
        float[,] map2d = new float[width, height];

        for (int x = 0; x < height; x++) {
            for (int y = 0; y < width; y++) {
                map2d[x, y] = map[y * width + x];
            }
        }

        return map2d;
    }

    private Texture2D GenerateHeightMapTexture() {
        Texture2D texture = new Texture2D(width, height);

        for (int x = 0; x < height; x++) {
            for (int y = 0; y < width; y++) {
                float v = heightMap[x * width + y];
                Color color = new Color(v, v, v);
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        return texture;
    }
}