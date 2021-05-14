using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatrolPath : MonoBehaviour {
    [System.Serializable]
    public struct WayPoint {
        public Transform point;
        public float speedToPoint;
        public float waitingDuration;

        public WayPoint(Transform point, float speedToPoint, float waitingDuration) {
            this.point = point;
            this.speedToPoint = speedToPoint;
            this.waitingDuration = waitingDuration;
        }
    }
    [System.Serializable]
    public enum Mode {
        BackAndForth, Loop
    }

    public Mode mode = Mode.BackAndForth;
    public WayPoint[] wayPoints = new WayPoint[0];
}