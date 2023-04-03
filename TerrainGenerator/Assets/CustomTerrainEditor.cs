using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TerrainGenerator))]
public class CustomTerrainEditor : Editor
{
    public override void OnInspectorGUI() {
        DrawDefaultInspector();
        TerrainGenerator script = (TerrainGenerator)target;
        
        if (GUILayout.Button("Generate Noise Map")) {
            script.GenerateNoiseMap();
        }
        
        if (GUILayout.Button("Apply Height Curve")) {
            script.ApplyCurve();
        }
        
        if (GUILayout.Button("Erode Terrain")) {
            script.ErodeMap();
        }
        
        
        
        
    }
}
