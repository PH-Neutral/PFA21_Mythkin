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
    [HideInInspector] public Camera cam;

    [SerializeField] Transform _swivel, _stick;
    [SerializeField] float _rotationLowest, _rotationHighest, rotationPower = 1;
    //[SerializeField] float _preferedZoom = 2, _fpsModeThreshold = 1;
    [SerializeField] bool _invertVertical = true, _invertHorizontal = false;
    Cinemachine.CinemachineVirtualCamera cmCam;
    PlayerCharacter _player;
    Transform _camRef, _headRef;
    private void Awake() {
        cam = GetComponentInChildren<Camera>();
        cmCam = GetComponentInChildren<Cinemachine.CinemachineVirtualCamera>();
        cmCam.transform.SetParent(null);
    }
    private void LateUpdate() {
        AdjustPosition();
    }
    public void SetReferences(PlayerCharacter player, Transform head, Transform cameraPivot) {
        _player = player;
        _headRef = head;
        _camRef = cameraPivot;
        transform.SetParent(null);
        AdjustPosition();
    }
    public void RotateVertical(float speedRatio)
    {
        if(speedRatio == 0) return;
        _swivel.localRotation *= Quaternion.AngleAxis((_invertVertical ? -1 : 1) * speedRatio * rotationPower, Vector3.right);
        Vector3 angles = _swivel.localEulerAngles;
        angles.y = angles.z = 0;
        if(angles.x >= 180 && angles.x < 360 + _rotationLowest) {
            angles.x = 360 + _rotationLowest;
        } else if(angles.x <= 180 && angles.x > _rotationHighest) {
            angles.x = _rotationHighest;
        }
        _swivel.localEulerAngles = angles;
    }
    public void RotateHorizontal(float speedRatio) {
        if(speedRatio == 0) return;
        //transform.localRotation = Quaternion.Euler(0, (_invertHorizontal ? -1 : 1) * speedRatio * RotationSpeedHori * Time.deltaTime, 0) * transform.localRotation;
        //transform.SlerpRotation(_rotHori, RotationSpeedHori * speedRatio, Space.Self);
        transform.rotation *= Quaternion.AngleAxis((_invertHorizontal ? -1 : 1) * speedRatio * rotationPower, Vector3.up);
    }
    void AdjustPosition() {
        transform.position = _camRef.position;
    }
}
