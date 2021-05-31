using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

public class PlayerCamera : MonoBehaviour
{
    public Quaternion xRotation {
        get {
            return _swivel.localRotation;
        }
    }
    public Vector3 CamForward {
        get {
            return _swivel.forward;
        }
    }
    [SerializeField] public Camera cam;
    public Transform playerHead;
    public float maxZoomRatio = 1;

    [SerializeField] Transform _swivel, _stick;
    [SerializeField] float _rotationLowest, _rotationHighest, rotationPower = 1;
    [SerializeField] float _preferedZoom = 2, _fpsModeThreshold = 1;
    [SerializeField] bool _invertVertical = true, _invertHorizontal = false;
    PlayerCharacter _player;
    Transform _camRef, _headRef;
    Quaternion _rotVert, _rotHori;
    float _currentRotVertX = 0, _fpsModeRatio = 1;
    float _camSphereRadius;
    private void Awake() {
        cam = GetComponentInChildren<Camera>();
        _camSphereRadius = 0.1f;//CalculateCamSphereRadius();
        //Debug.Log("camSphereRadius = " + _camSphereRadius);
        _currentRotVertX = _swivel.transform.localRotation.eulerAngles.x;
    }
    private void LateUpdate() {
        AdjustCamera();
    }
    public void SetReferences(PlayerCharacter player, Transform head, Transform cameraPivot) {
        _player = player;
        _headRef = head;
        _camRef = cameraPivot;
        AdjustCamera();
    }
    public void RotateVertical(float speedRatio)
    {
        if(speedRatio == 0) return;
        //_currentRotVertX = Mathf.Clamp(_currentRotVertX + (_invertVertical ? -1 : 1) * speedRatio * RotationSpeedVert * Time.deltaTime, _rotationLowest, _rotationHighest);
        //_swivel.localRotation = Quaternion.Euler(_currentRotVertX, 0, 0);
        //_swivel.SlerpRotation(_rotVert, RotationSpeedVert * speedRatio, Space.Self);
        _swivel.localRotation *= Quaternion.AngleAxis((_invertVertical ? -1 : 1) * speedRatio * rotationPower, Vector3.right);
    }
    public void RotateHorizontal(float speedRatio) {
        if(speedRatio == 0) return;
        //transform.localRotation = Quaternion.Euler(0, (_invertHorizontal ? -1 : 1) * speedRatio * RotationSpeedHori * Time.deltaTime, 0) * transform.localRotation;
        //transform.SlerpRotation(_rotHori, RotationSpeedHori * speedRatio, Space.Self);
        transform.localRotation *= Quaternion.AngleAxis((_invertHorizontal ? -1 : 1) * speedRatio * rotationPower, Vector3.up);
    }
    public void LerpZoom(float t) {
        _stick.localPosition = Vector3.Lerp(Vector3.zero, Vector3.back * _preferedZoom, t);
        _fpsModeRatio = Mathf.Clamp01(t * _preferedZoom / _fpsModeThreshold);
    }
    void AdjustCamera() {
        float zoomRatio = maxZoomRatio;
        Vector3 rayOrigin = transform.position;
        Vector3 rayDir = -transform.forward * _preferedZoom * maxZoomRatio;
        int layerMask = Utils.layer_Terrain.ToLayerMask() | Utils.layer_Enemies.ToLayerMask();
        if (Physics.SphereCast(rayOrigin, _camSphereRadius, rayDir, out RaycastHit hit, rayDir.magnitude, layerMask)) {
            zoomRatio = Vector3.Distance(rayOrigin, hit.point) / _preferedZoom;
        }
        LerpZoom(zoomRatio);
        AdjustPosition();
        _player.HideMeshes(_fpsModeRatio < 1);
    }
    void AdjustPosition() {
        transform.position = Vector3.Lerp(_headRef.position, _camRef.position, _fpsModeRatio);
    }

    float CalculateCamSphereRadius() {
        /*float h = cam.nearClipPlane * Mathf.Tan(0.5f * cam.fieldOfView * Mathf.Deg2Rad);
        float l = h * cam.aspect;
        float d = Mathf.Sqrt(Mathf.Pow(h, 2) + Mathf.Pow(l, 2));
        float r = Mathf.Sqrt(Mathf.Pow(d, 2) + Mathf.Pow(cam.nearClipPlane, 2));*/
        return Mathf.Sqrt(Mathf.Pow(cam.nearClipPlane, 4) * Mathf.Pow(Mathf.Tan(0.5f * cam.fieldOfView * Mathf.Deg2Rad), 4) * Mathf.Pow(1 + cam.aspect, 2) 
            + Mathf.Pow(cam.nearClipPlane, 2));
    }
}
