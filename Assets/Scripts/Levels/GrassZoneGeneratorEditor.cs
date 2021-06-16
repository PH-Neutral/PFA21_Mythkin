using System.Collections;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GrassZoneGenerator))]
public class GrassZoneGeneratorEditor : Editor {
    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        GrassZoneGenerator myScript = (GrassZoneGenerator)target;
        if(GUILayout.Button("Populate")) {
            myScript.Populate();
        }
        if(GUILayout.Button("Clear")) {
            myScript.Clear();
        }
    }
}