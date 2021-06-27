using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BezierSpline : MonoBehaviour {
	public int CurveCount {
		get {
			return (points.Length - 1) / 3;
		}
	}
	public int ControlPointCount {
		get {
			return points.Length;
		}
	}
	[SerializeField] Vector3[] points;
	[SerializeField] Bezier.ControlPointMode[] modes;
	public void AddCurve() {
		Vector3 point = points[points.Length - 1];
		Array.Resize(ref points, points.Length + 3);
		point.x += 1f;
		points[points.Length - 3] = point;
		point.x += 1f;
		points[points.Length - 2] = point;
		point.x += 1f;
		points[points.Length - 1] = point;

		Array.Resize(ref modes, modes.Length + 1);
		modes[modes.Length - 1] = modes[modes.Length - 2];
		EnforceMode(points.Length - 4);
	}
	public Vector3 GetPoint(double t) {
		int i;
		if(t >= 1f) {
			t = 1f;
			i = points.Length - 4;
		} else {
			t = Utils.Clamp01(t) * CurveCount;
			i = (int)t;
			t -= i;
			i *= 3;
		}
		return transform.TransformPoint(Bezier.GetPoint(
			points[i], points[i + 1], points[i + 2], points[i + 3], t).ToVector3());
	}

	public Vector3 GetVelocity(double t) {
		int i;
		if(t >= 1f) {
			t = 1f;
			i = points.Length - 4;
		} else {
			t = Utils.Clamp01(t) * CurveCount;
			i = (int)t;
			t -= i;
			i *= 3;
		}
		return transform.TransformPoint(Bezier.GetFirstDerivative(
			points[i], points[i + 1], points[i + 2], points[i + 3], t).ToVector3()) - transform.position;
	}
	public Vector3 GetDirection(double t) {
		return GetVelocity(t).normalized;
	}
	public Vector3 GetControlPoint(int index) {
		return points[index];
	}
	public void SetControlPoint(int index, Vector3 point) {
		if(index % 3 == 0) {
			Vector3 delta = point - points[index];
			if(index > 0) {
				points[index - 1] += delta;
			}
			if(index + 1 < points.Length) {
				points[index + 1] += delta;
			}
		}
		points[index] = point;
		EnforceMode(index);
	}
	public Bezier.ControlPointMode GetControlPointMode(int index) {
		return modes[(index + 1) / 3];
	}
	public void SetControlPointMode(int index, Bezier.ControlPointMode mode) {
		modes[(index + 1) / 3] = mode;
		EnforceMode(index);
	}
	private void EnforceMode(int index) {
		int modeIndex = (index + 1) / 3;
		Bezier.ControlPointMode mode = modes[modeIndex];
		if(mode == Bezier.ControlPointMode.Free || modeIndex == 0 || modeIndex == modes.Length - 1) {
			return;
		}
		int middleIndex = modeIndex * 3;
		int fixedIndex, enforcedIndex;
		if(index <= middleIndex) {
			fixedIndex = middleIndex - 1;
			enforcedIndex = middleIndex + 1;
		} else {
			fixedIndex = middleIndex + 1;
			enforcedIndex = middleIndex - 1;
		}
		Vector3 middle = points[middleIndex];
		Vector3 enforcedTangent = middle - points[fixedIndex];
		if(mode == Bezier.ControlPointMode.Aligned) {
			enforcedTangent = enforcedTangent.normalized * Vector3.Distance(middle, points[enforcedIndex]);
		}
		points[enforcedIndex] = middle + enforcedTangent;
	}
	public void Reset() {
		points = new Vector3[] {
			new Vector3(1f, 0f, 0f),
			new Vector3(2f, 0f, 0f),
			new Vector3(3f, 0f, 0f),
			new Vector3(4f, 0f, 0f)
		};
		modes = new Bezier.ControlPointMode[] {
			Bezier.ControlPointMode.Free,
			Bezier.ControlPointMode.Free
		};
	}
}