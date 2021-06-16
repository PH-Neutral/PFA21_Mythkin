using System.Collections;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GrassZoneGenerator))]
public class GrassZoneGeneratorEditor : Editor {
    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        GrassZoneGenerator myScript = (GrassZoneGenerator)target;
        if(GUILayout.Button("Populate All")) {
            myScript.PopulateAll(true);

        }
        if(GUILayout.Button("Clear All")) {
            myScript.ClearAll(true);
        }
    }
}