using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WaterGenerator))]
public class CustomWaterEditor : Editor
{
    public override void OnInspectorGUI() {
        DrawDefaultInspector();
        WaterGenerator script = (WaterGenerator)target;
        
        if (GUILayout.Button("reset")) {
            script.Reset();
        }
        
        if (GUILayout.Button("Generate River")) {
            script.GenerateRiver();
        }
        
    }
}

