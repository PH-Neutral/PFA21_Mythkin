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
    [SerializeField] float _zoomNear, _zoomFar, _rotationLowest, _rotationHighest;
    [SerializeField] bool invertY = true;
    Transform _reference;
    float _currentRotation = 0;
    private void Awake()
    {
        _currentRotation = _swivel.transform.localRotation.eulerAngles.x;
    }
    private void LateUpdate() {
        SetPositionToRef();
    }
    public void RotateVertical(float speed)
    {
        _currentRotation = Mathf.Clamp(_currentRotation + (invertY ? -1 : 1) * speed * Time.deltaTime, _rotationLowest, _rotationHighest);
        _swivel.transform.localRotation = Quaternion.Euler(_currentRotation, 0, 0);
    }
    public void RotateHorizontal(float speed)
    {
        transform.localRotation = Quaternion.Euler(0, speed * Time.deltaTime, 0) * transform.localRotation;
    }
    public void Zoom(float speed)
    {

    }
    void SetPositionToRef() {
        transform.position = Reference.position;
    }
}
