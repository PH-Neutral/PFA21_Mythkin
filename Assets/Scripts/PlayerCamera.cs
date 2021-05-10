using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [SerializeField] Transform _swivel, _stick;
    Camera _camera;
    [SerializeField] float _zoomNear, _zoomFar, _rotationLowest, _rotationHighest;
    private void Awake()
    {
        _camera = GetComponentInChildren<Camera>();
    }
    public void RotateVertical(float speed)
    {

    }
    public void RotateHorizontal(float speed)
    {

    }
    public void Zoom(float speed)
    {

    }
}
