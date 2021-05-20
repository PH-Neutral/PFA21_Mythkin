using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TunnelEntrance : MonoBehaviour {
    public enum Direction {
        xPositive, xNegative, yPositive, yNegative, zPositive, zNegative
    }
    [SerializeField] Direction enterDirection;
    BoxCollider _coll;
    private void Awake() {
        _coll = GetComponent<BoxCollider>();
    }
    public float GetCamLerpRatio(Vector3 worldPos, bool invert) {
        Vector3 exteriorPoint = GetIntersectPoint(-enterDirection.GetVector(), worldPos);
        Vector3 interiorPoint = GetIntersectPoint(enterDirection.GetVector(), worldPos);
        Debug.DrawLine(worldPos, exteriorPoint, Color.red);
        Debug.DrawLine(worldPos, interiorPoint, Color.green);
        float lerpRatio = Vector3.Distance(exteriorPoint, worldPos) / Vector3.Distance(exteriorPoint, interiorPoint);
        return invert ? 1 - lerpRatio : lerpRatio;
    }
    Vector3 GetIntersectPoint(Vector3 localDir, Vector3 refPos) {
        Vector3 pointOnPlane = transform.TransformPoint(_coll.center + _coll.size.Multiply(localDir)), planeNormal = transform.TransformDirection(localDir);
        Vector3 pointOnLine = refPos, lineDir = planeNormal;
        Utils.LinePlaneIntersection(out Vector3 intersection, pointOnPlane, planeNormal, pointOnLine, lineDir);
        return intersection;
    }
}