using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatrolPath : MonoBehaviour {
    [System.Serializable]
    public struct WayPoint {
        public Transform point;
        public float speedToPoint;
        public float waitingDuration;
        /*
        public WayPoint(Transform point, float speedToPoint, float waitingDuration) {
            this.point = point;
            this.speedToPoint = speedToPoint;
            this.waitingDuration = waitingDuration;
        }*/
    }
    [System.Serializable]
    public enum Mode {
        BackAndForth, Loop
    }

    public Mode mode = Mode.BackAndForth;
    public WayPoint[] wayPoints = new WayPoint[0];
    [SerializeField] float _gizmoSphereRadius = 0.5f;

    private void OnDrawGizmos() {
        Vector3 origin, target;
        for(int i = 0; i < wayPoints.Length; i++) {
            target = wayPoints[i].point.position;
            if(i == 0) Gizmos.color = Color.red;
            else Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(target, _gizmoSphereRadius);
            if(i > 0) {
                origin = wayPoints[i - 1].point.position;
                if(i == 1) Gizmos.color = Color.red;
                else Gizmos.color = Color.white;
                Gizmos.DrawLine(target, origin);
            }
        }
        if(wayPoints.Length > 2 && mode == Mode.Loop) {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(wayPoints[0].point.position, wayPoints[wayPoints.Length - 1].point.position);
        }
    }
}