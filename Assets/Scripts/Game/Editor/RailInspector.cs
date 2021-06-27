using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Rail))]
public class RailInspector : Editor {
    const int lineSteps = 10;

    Rail rail;
    Transform handleTransform;
    Quaternion handleRotation;

    private void OnSceneGUI() {
        rail = target as Rail;
        handleTransform = rail.transform;
        handleRotation = Tools.pivotRotation == PivotRotation.Local ? handleTransform.rotation : Quaternion.identity;

        Vector3 p0 = ShowPoint(0);
        Vector3 p1 = ShowPoint(1);
        Vector3 p2 = ShowPoint(2);

        Handles.color = Color.grey;
        Handles.DrawLine(p0, p1);
        Handles.DrawLine(p1, p2);

        Handles.color = Color.white;
        Vector3 lineStart = rail.GetPoint(0f), lineEnd;
        Handles.color = Color.green;
        Handles.DrawLine(lineStart, lineStart + rail.GetDirection(0f));
        for(int i = 1; i <= lineSteps; i++) {
            lineEnd = rail.GetPoint(i / (float)lineSteps);
            Handles.color = Color.white;
            Handles.DrawLine(lineStart, lineEnd);
            Handles.color = Color.green;
            Handles.DrawLine(lineEnd, lineEnd + rail.GetDirection(i / (float)lineSteps));
            lineStart = lineEnd;
        }
    }
    Vector3 ShowPoint(int index) {
        Vector3 point = handleTransform.TransformPoint(rail.points[index]);
        EditorGUI.BeginChangeCheck();
        point = Handles.DoPositionHandle(point, handleRotation);
        if(EditorGUI.EndChangeCheck()) {
            Undo.RecordObject(rail, "Move Point");
            EditorUtility.SetDirty(rail);
            rail.points[index] = handleTransform.InverseTransformPoint(point);
        }
        return point;
    }
}