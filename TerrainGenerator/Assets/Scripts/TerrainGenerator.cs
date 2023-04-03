using System;
using System.Collections;
using System.Collections.Generic;
using DefaultNamespace;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour {
    [Range(1, 999)] 
    public int seed = 1234;
    public int depth = 150;

    public int width = 512;
    public int height = 512;

    private Terrain terrain;
    private float[] heightMap;

    public PerlinNoiseParams perlinNoiseParams;
    public ErosionParams erosionParams;

    private static System.Random random;

    private int counter = 0;

    public AnimationCurve curve;

    static int kernelWidth = 7;

    float[,] kernel = CreateKernel(kernelWidth);

    public GameObject heightMapPlane;

    void Start() {
        GenerateNoiseMap();
    }

    void Update() {
        int stepSize = 50_000;

        if (counter == -1)
            return;

        if (counter < erosionParams.erosionIterationCount) {
            for (int i = 0; i < stepSize; i++) {
                Erode();
            }

            terrain.terrainData.SetHeights(0, 0, ConvertTo2D(heightMap));
            counter += stepSize;
        }
        else {
            GaussianBlur();
            counter = -1;
        }
    }

    public void GenerateNoiseMap() {
        random = new System.Random(seed);
        terrain = GetComponent<Terrain>();
        terrain.terrainData = GenerateTerrain(terrain.terrainData);
    }

    public void ErodeMap() {
        // GenerateNoiseMap();

        // int erosionIterationCount = 400_000;
        for (int i = 0; i < erosionParams.erosionIterationCount; i++) {
            Erode();
        }

        GaussianBlur(3, .5f);
        terrain.terrainData.SetHeights(0, 0, ConvertTo2D(heightMap));

        Texture heightMapTexture = GenerateHeightMapTexture();
        this.heightMapPlane.GetComponent<Renderer>().material.SetTexture("_BaseMap", heightMapTexture);
    }


    TerrainData GenerateTerrain(TerrainData terrainData) {
        terrainData.heightmapResolution = width + 1;
        terrainData.size = new Vector3(height, depth, width);

        heightMap = GeneratePerlinNoiseMap();
        // ApplyCurve();
        Texture heightMapTexture = GenerateHeightMapTexture();
        this.heightMapPlane.GetComponent<Renderer>().material.SetTexture("_BaseMap", heightMapTexture);
        terrainData.SetHeights(0, 0, ConvertTo2D(heightMap));
        return terrainData;
    }

    public void ApplyCurve() {
        for (int x = 0; x < height; x++) {
            for (int y = 0; y < width; y++) {
                heightMap[x * width + y] = curve.Evaluate(heightMap[x * width + y]);
            }
        }

        terrain.terrainData.SetHeights(0, 0, ConvertTo2D(heightMap));
        Texture heightMapTexture = GenerateHeightMapTexture();
        this.heightMapPlane.GetComponent<Renderer>().material.SetTexture("_BaseMap", heightMapTexture);
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

            //calculate gradient
            float[] heightAndGradient = CalculateHeightAndGradient((int)posX, (int)posY);
            float oldHeight = heightAndGradient[0];
            float gradientX = heightAndGradient[1];
            float gradientY = heightAndGradient[2];

            dirX = dirX * erosionParams.inertia - gradientX * (1 - erosionParams.inertia);
            dirY = dirY * erosionParams.inertia - gradientY * (1 - erosionParams.inertia);

            // Normalize direction
            float len = Mathf.Sqrt(dirX * dirX + dirY * dirY);
            if (len != 0) {
                dirX /= len;
                dirY /= len;
            }

            //step in direction of gradient
            posX += dirX;
            posY += dirY;

            // Stop simulating water if drop not moving or moved over the map edge
            if ((dirX == 0 && dirY == 0) || posX < 0 || posX >= width - 1 || posY < 0 || posY >= height - 1) {
                break;
            }

            // Find the drops new height and calculate the height difference
            float newHeight = CalculateHeightAndGradient((int)posX, (int)posY)[0];
            float deltaHeight = newHeight - oldHeight;

            // Calculate the drops sediment capacity
            // The capacity is higher when if the drop is big or moving fast 
            float sedimentCapacity = Mathf.Max(-deltaHeight * speed * water * erosionParams.sedimentCapacityFactor,
                erosionParams.minSedimentCapacity);

            //decide whether to erode or deposit
            if (sediment > sedimentCapacity || deltaHeight > 0) {
                // If moving uphill or more sediment than capacity deposit a fraction of the sediment
                float amountToDeposit = (deltaHeight > 0) ? Mathf.Min(deltaHeight, sediment) : (sediment - sedimentCapacity) * erosionParams.depositSpeed;
                sediment -= amountToDeposit;
                heightMap[dropletIndex] += amountToDeposit;
            } else {
                // Erode a fraction of the drops current capacity
                float amountToErode = Mathf.Min((sedimentCapacity - sediment) * erosionParams.erodeSpeed, -deltaHeight);
                sediment += amountToErode;
                heightMap[dropletIndex] -= amountToErode;
            }
            
            //updating the size and speed of the drop
            speed = Mathf.Sqrt(speed * speed + deltaHeight * erosionParams.gravity);
            water *= (1 - erosionParams.evaporateSpeed);
        }
    }

    private void LocalDeposit(int posX, int posY, float amountToDeposit) {
        int radius = (int)Mathf.Floor(kernelWidth / 2f);
        int x0 = posX - radius;
        int y0 = posY - radius;
        int xStart = Math.Max(0, x0);
        int yStart = Math.Max(0, y0);
        int xEnd = Math.Min(height, x0 + kernelWidth);
        int yEnd = Math.Min(width, y0 + kernelWidth);

        for (int i = xStart; i < xEnd; i++) {
            for (int j = yStart; j < yEnd; j++) {
                heightMap[i * width + j] += amountToDeposit * kernel[i - posX + 1, j - posY + 1];
            }
        }
    }

    private void LocalErode(int posX, int posY, float amountToErode) {
        int radius = (int)Mathf.Floor(kernelWidth / 2f);
        int x0 = posX - radius;
        int y0 = posY - radius;
        int xStart = Math.Max(0, x0);
        int yStart = Math.Max(0, y0);
        int xEnd = Math.Min(height, x0 + kernelWidth);
        int yEnd = Math.Min(width, y0 + kernelWidth);

        for (int i = xStart; i < xEnd; i++) {
            for (int j = yStart; j < yEnd; j++) {
                heightMap[i * width + j] -= amountToErode * kernel[i - posX + 1, j - posY + 1];
            }
        }
    }


    private static float[,] CreateKernel(int width) {
        float[,] kernel = new float[width, width];
        for (int i = 0; i < width; i++) {
            for (int j = 0; j < width; j++) {
                kernel[i, j] = 1f / (width * width);
            }
        }

        return kernel;
    }


    public static float[,] GaussianKernel(int width, float weight) {
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


    private float[] CalculateHeightAndGradient(int posX, int posY) {
        int coordX = posX;
        int coordY = posY;

        // Calculate drops offset inside the cell neighbourhood
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

        // Calculate drops direction by bilinear interpolating the height differences
        float gradientX = (east - current) * (1 - y) + (southEast - south) * y;
        float gradientY = (south - current) * (1 - x) + (southEast - east) * x;

        // Calculate height with bilinear interpolation
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

        float offsetX = random.Next(-10000, 1000);
        float offsetY = random.Next(-10000, 1000);

        for (int x = 0; x < height; x++) {
            for (int y = 0; y < width; y++) {
                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;

                for (int i = 0; i < octaves; i++) {
                    float xNormalized = (float)x / height * frequency + offsetX;
                    float yNormalized = (float)y / width * frequency + offsetY;
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

    public float[,] GetHeightMap2D() {
        return ConvertTo2D(heightMap);
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

    public void GaussianBlur(int kernelWidth = 3, float sigma = 1f) {
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
                        sum += heightMap[i * width + j] * kernel[i - x + 1, j - y + 1];
                    }
                }


                heightMap[x * width + y] = sum;
            }
        }
    }


    public float[] GetHeightMap() {
        return heightMap;
    }

    public int GetDepth() {
        return depth;
    }


    public void SetTerrainData(float[,] map) {
        this.heightMapPlane.GetComponent<Renderer>().material.SetTexture("_BaseMap", GenerateHeightMapTexture());
        terrain.terrainData.SetHeights(0, 0, map);
    }

    public int GetHeight() {
        return height;
    }

    public int GetWidth() {
        return width;
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
}