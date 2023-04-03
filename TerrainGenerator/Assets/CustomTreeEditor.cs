using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TreeSpawner))]
public class CustomTreeEditor : Editor
{
    public override void OnInspectorGUI() {
        DrawDefaultInspector();
        TreeSpawner script = (TreeSpawner)target;
        
        if (GUILayout.Button("Spawn Trees")) {
            script.SpawnTrees();
        }
        
        if (GUILayout.Button("Destroy Trees")) {
            script.DestroyTrees();
        }
        
        
        
    }
}
