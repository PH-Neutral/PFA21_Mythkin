using System.Collections;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(OutlineHelper))]
public class OutlineHelperEditor : Editor {
    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        OutlineHelper myScript = (OutlineHelper)target;
        if(GUILayout.Button("Create Outline Object")) {
            myScript.CreateOutlineObject(null, true);
        }
        if(GUILayout.Button("Delete Outline")) {
            myScript.ResetScript(true);
        }
    }
}