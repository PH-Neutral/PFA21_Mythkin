using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Bezier {
	public enum ControlPointMode {
		Free,
		Aligned,
		Mirrored
	}

	public static Vector3Double GetPoint(Vector3 p0, Vector3 p1, Vector3 p2, double t)
	   => GetPoint(p0.ToVector3Double(), p1.ToVector3Double(), p2.ToVector3Double(), t);
	public static Vector3Double GetPoint(Vector3Double p0, Vector3Double p1, Vector3Double p2, double t) {
		t = Utils.Clamp01(t);
		double oneMinusT = 1d - t;
		return oneMinusT * oneMinusT * p0 + 2d * oneMinusT * t * p1 + t * t * p2;
	}
	public static Vector3Double GetFirstDerivative(Vector3 p0, Vector3 p1, Vector3 p2, double t)
	   => GetFirstDerivative(p0.ToVector3Double(), p1.ToVector3Double(), p2.ToVector3Double(), t);
	public static Vector3Double GetFirstDerivative(Vector3Double p0, Vector3Double p1, Vector3Double p2, double t) {
		return 2d * (1d - t) * (p1 - p0) + 2d * t * (p2 - p1);
	}
	public static Vector3Double GetPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, double t)
	   => GetPoint(p0.ToVector3Double(), p1.ToVector3Double(), p2.ToVector3Double(), p3.ToVector3Double(), t);
	public static Vector3Double GetPoint(Vector3Double p0, Vector3Double p1, Vector3Double p2, Vector3Double p3, double t) {
		t = Utils.Clamp01(t);
		double oneMinusT = 1d - t;
		return
			oneMinusT * oneMinusT * oneMinusT * p0 +
			3d * oneMinusT * oneMinusT * t * p1 +
			3d * oneMinusT * t * t * p2 +
			t * t * t * p3;
	}
	public static Vector3Double GetFirstDerivative(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, double t) 
		=> GetFirstDerivative(p0.ToVector3Double(), p1.ToVector3Double(), p2.ToVector3Double(), p3.ToVector3Double(), t);
	public static Vector3Double GetFirstDerivative(Vector3Double p0, Vector3Double p1, Vector3Double p2, Vector3Double p3, double t) {
		t = Utils.Clamp01(t);
		double oneMinusT = 1d - t;
		return
			3d * oneMinusT * oneMinusT * (p1 - p0) +
			6d * oneMinusT * t * (p2 - p1) +
			3d * t * t * (p3 - p2);
	}
}