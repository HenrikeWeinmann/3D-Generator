using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeSpawner : MonoBehaviour {

    
    public GameObject treePrefab;
    public GameObject treePrefab2;
    public GameObject terrain;

    public GameObject trees;

    [Range(0, 5000)] 
    public int amountTrees;

    [Range(0,1)]
    public float maxHeight;

    private List<GameObject> treesList = new List<GameObject>();
    private System.Random random = new System.Random(1234);

    private float[] heightMap;
    private float[,] waterHeightMap;

    public GameObject water;
    
    public void SpawnTrees() {
        TerrainGenerator terrainGenerator = terrain.GetComponent<TerrainGenerator>();
        WaterGenerator waterGenerator = terrain.GetComponent<WaterGenerator>();
        heightMap = terrainGenerator.GetHeightMap();
        waterHeightMap = waterGenerator.GetWaterHeightMap();
        
        
        int terrainHeight = terrainGenerator.depth;
        int width = terrainGenerator.width;
        int height = terrainGenerator.height;

        for (int i = 0; i < amountTrees; i++) {
            int xPos; 
            int zPos;
            float yPos;
            do {
               xPos = random.Next(1, height - 1);
               zPos = random.Next(1, width - 1);
               yPos = heightMap[xPos * width + zPos];
            } while (!CanPlaceTree(xPos, zPos) || heightMap[xPos * width + zPos] > maxHeight);
            
            GameObject tree = InstantiateRandomTree();
            treesList.Add(tree);
            tree.transform.position = new Vector3(zPos, yPos*terrainHeight, xPos);
            tree.transform.parent = trees.transform;
            tree.transform.localScale = GetRandomScale(0.5f, 2f);
        }
        
    }

    private GameObject InstantiateRandomTree() {
        if (random.NextDouble() >= 0.5) {
            return Instantiate<GameObject>(treePrefab);
        }
        return Instantiate<GameObject>(treePrefab2);
    }

    private Vector3 GetRandomScale(float lower, float upper) {
        float scale = (float) random.NextDouble() * (upper - lower) + lower;
        return new Vector3(scale, scale, scale);
    }

    private bool CanPlaceTree(int xPos, int yPos) {
        return waterHeightMap[xPos, yPos] < 0.04;
    }

    public void DestroyTrees() {
        foreach (var tree in treesList) {
            DestroyImmediate(tree);
        }
        this.treesList.Clear();
    }
    

}
