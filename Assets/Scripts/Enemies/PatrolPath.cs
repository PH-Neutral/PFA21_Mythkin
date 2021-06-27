using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatrolPath : MonoBehaviour {
    [System.Serializable]
    public struct Waypoint {
        public Transform point;
        public bool sprintSpeed;
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

    public int Length {
        get {
            return wayPoints.Length;
        }
    }
    public Mode mode = Mode.BackAndForth;
    public Waypoint[] wayPoints = new Waypoint[0];
    [SerializeField] float _gizmoSphereRadius = 0.5f;

    public int GetNextIndex(int currentIndex, ref bool baseDirection) {
        int nextIndex = currentIndex;
        if(mode == Mode.BackAndForth) {
            if(currentIndex + 1 == Length) {
                baseDirection = false;
            } else if(currentIndex == 0) {
                baseDirection = true;
            }
        }
        nextIndex += (baseDirection ? 1 : -1); // next wayPoint index
        if(nextIndex < 0) nextIndex = Length - 1;
        else if(nextIndex >= Length) nextIndex = 0;
        return nextIndex;
    }
    public Waypoint GetWaypoint(int index) {
        return wayPoints[index];
    }
#if UNITY_EDITOR
    private void OnDrawGizmos() {
        Vector3 origin, target;
        for(int i = 0; i < wayPoints.Length; i++) {
            if(wayPoints[i].point == null) continue;
            target = wayPoints[i].point.position;
            if(i == 0) Gizmos.color = Color.red;
            else Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(target, _gizmoSphereRadius);
            if(i > 0) {
                if(wayPoints[i - 1].point == null) continue;
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
#endif
}