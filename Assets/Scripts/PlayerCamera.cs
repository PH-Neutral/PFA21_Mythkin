using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [SerializeField] Transform _swivel, _stick;
    Camera _camera;
    [SerializeField] float _zoomNear, _zoomFar, _rotationLowest, _rotationHighest;
    float _currentRotation = 0;
    [SerializeField] bool invertY = true;
    private void Awake()
    {
        _camera = GetComponentInChildren<Camera>();
        _currentRotation = _swivel.transform.localRotation.eulerAngles.x;
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
}
