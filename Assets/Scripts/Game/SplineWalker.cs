using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SplineWalker : MonoBehaviour {
	public BezierSpline spline;
	public float duration;
	[SerializeField] bool forceRestart = false;

	Action _onReachEnd;
	double _progress;
	bool _hasFinished = true, _canMove = false;

    private void Start() {
		transform.position = spline.GetPoint(0);
	}
    private void Update() {
		if(forceRestart) {
			forceRestart = false;
			BeginMoving();
        }
		if(_canMove) {
			_progress = Utils.Clamp01(_progress + Time.unscaledDeltaTime / duration);
			transform.position = spline.GetPoint(_progress);
			if(_progress >= 1) {
				_hasFinished = true;
				_canMove = false;
				_onReachEnd?.Invoke();
			}
		}
	}

	public void BeginMoving() => BeginMoving(() => { });
	public void BeginMoving(Action onReachEnd) {
		_onReachEnd = onReachEnd;
		_progress = 0;
		_hasFinished = false;
		_canMove = true;
	}
	public void ResumeMoving() {
		if(!_hasFinished) _canMove = true;
    }
	public void StopMoving() {
		_canMove = false;
    }
}