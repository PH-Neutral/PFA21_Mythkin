using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    public Transform Reference {
        get { return _reference; }
        set {
            _reference = value;
            SetPositionToRef();
        }
    }

    [SerializeField] Transform _swivel, _stick;
    [SerializeField] float _rotationLowest, _rotationHighest, _rotationSpeed = 100;
    [SerializeField] float _zoomNear, _zoomFar, _zoomSpeed = 1;
    [SerializeField] bool invertY = true, invertZoom = false;
    Transform _reference;
    Vector3 _targetZoom = Vector3.zero;
    float _currentRotation = 0;
    private void Awake()
    {
        _currentRotation = _swivel.transform.localRotation.eulerAngles.x;
        _targetZoom = _stick.localPosition;
    }
    private void Update() {
        
    }
    private void LateUpdate() {
        LerpZoom();
        SetPositionToRef();
    }
    public void RotateVertical(float speedRatio)
    {
        if(speedRatio == 0) return;
        _currentRotation = Mathf.Clamp(_currentRotation + (invertY ? -1 : 1) * speedRatio * _rotationSpeed * Time.deltaTime, _rotationLowest, _rotationHighest);
        _swivel.transform.localRotation = Quaternion.Euler(_currentRotation, 0, 0);
    }
    public void RotateHorizontal(float speedRatio) {
        if(speedRatio == 0) return;
        transform.localRotation = Quaternion.Euler(0, speedRatio * _rotationSpeed * Time.deltaTime, 0) * transform.localRotation;
    }
    public void Zoom(float speedRatio) {
        if(speedRatio == 0) return;
        _targetZoom.z = Mathf.Clamp(_stick.localPosition.z + speedRatio * _zoomSpeed, _zoomFar, _zoomNear);
    }
    void LerpZoom() {
        float t = _zoomSpeed * 4 * Time.deltaTime / Vector3.Distance(_targetZoom, _stick.localPosition);
        if(t >= 1) return;
        _stick.localPosition = Vector3.Lerp(_stick.localPosition, _targetZoom, t);
    }
    void SetPositionToRef() {
        transform.position = Reference.position;
    }
}
