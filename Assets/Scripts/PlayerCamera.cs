using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    public Quaternion xRotation {
        get {
            return _swivel.localRotation;
        }
    }
    [HideInInspector] public Camera cam;
    public Transform playerHead;
    public float maxZoomRatio = 1;

    [SerializeField] Transform _swivel, _stick;
    [SerializeField] float _rotationLowest, _rotationHighest, _rotationSpeed = 100;
    [SerializeField] float _preferedZoom = 2, _fpsModeThreshold = 1;
    [SerializeField] bool _invertVertical = true, _invertHorizontal = false;
    Transform _camRef, _headRef;
    float _currentRotation = 0, _fpsModeRatio = 1;
    private void Awake() {
        cam = GetComponentInChildren<Camera>();
        _currentRotation = _swivel.transform.localRotation.eulerAngles.x;
    }
    private void LateUpdate() {
        AdjustCamera();
    }
    public void SetReferences(Transform head, Transform cameraPivot) {
        _headRef = head;
        _camRef = cameraPivot;
        AdjustCamera();
    }
    public void RotateVertical(float speedRatio)
    {
        if(speedRatio == 0) return;
        _currentRotation = Mathf.Clamp(_currentRotation + (_invertVertical ? -1 : 1) * speedRatio * _rotationSpeed * Time.deltaTime, _rotationLowest, _rotationHighest);
        _swivel.transform.localRotation = Quaternion.Euler(_currentRotation, 0, 0);
    }
    public void RotateHorizontal(float speedRatio) {
        if(speedRatio == 0) return;
        transform.localRotation = Quaternion.Euler(0, (_invertHorizontal ? -1 : 1) * speedRatio * _rotationSpeed * Time.deltaTime, 0) * transform.localRotation;
    }
    public void LerpZoom(float t) {
        _stick.localPosition = Vector3.Lerp(Vector3.zero, Vector3.back * _preferedZoom, t);
        _fpsModeRatio = Mathf.Clamp01(t * _preferedZoom / _fpsModeThreshold);
    }
    void AdjustCamera() {
        float zoomRatio = maxZoomRatio;
        Vector3 rayOrigin = transform.position;
        Vector3 rayDir = -transform.forward * _preferedZoom * maxZoomRatio;
        int layerMask = Utils.layer_Terrain.ToLayerMask() | Utils.layer_Environment.ToLayerMask() | Utils.layer_Enemies.ToLayerMask();
        if (Physics.Raycast(rayOrigin, rayDir, out RaycastHit hit, rayDir.magnitude, layerMask)) {
            zoomRatio = Vector3.Distance(rayOrigin, hit.point) / _preferedZoom;
        }
        LerpZoom(zoomRatio);
        AdjustPosition();
    }
    void AdjustPosition() {
        transform.position = Vector3.Lerp(_headRef.position, _camRef.position, _fpsModeRatio);
    }
}
