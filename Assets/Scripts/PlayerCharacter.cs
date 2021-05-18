using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCharacter : MonoBehaviour {
    public float Speed {
        get {
            return _moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? _sprintRatio : 1);
        }
    }
    public bool IsClimbing {
        get {
            return !_charaCtrl.isGrounded && _isOnClimbWall;
        }
    }
    float Radius {
        get { return _charaCtrl.radius + _charaCtrl.skinWidth + 0.05f; }
    }
    [SerializeField] Transform _bodyCenter, _model;
    [SerializeField] float _moveSpeed = 5, _sprintRatio = 1.5f, _climbSpeed = 2, _rotationSpeed = 20, _camRotateSpeed = 100, _jumpHeight = 5;
    [SerializeField] float _terminalVelocity = 10, _lerpMoveTime = 0.5f; // in sec
    [SerializeField] float _climbWallDistance = 1;
    CharacterController _charaCtrl;
    PlayerCamera _playerCam;
    Vector3 _movement = Vector3.zero, _wallDir;
    bool _wasClimbing = true, _isOnClimbWall = false, _hasClimbFloor = false;
    private void Awake() {
        _charaCtrl = GetComponent<CharacterController>();
        _playerCam = GetComponentInChildren<PlayerCamera>();
    }
    private void Update() {
        _isOnClimbWall = IsClimbWallForward();
        _hasClimbFloor = !_isOnClimbWall ? IsClimbFloorForwardDown() : false;
        Look();
        Move();

        _wasClimbing = _isOnClimbWall;
    }
    void Move() {
        Vector3 inputs = GetInputs();
        Vector3 motion;
        if(_isOnClimbWall) {
            if(!_wasClimbing) {
                _wallDir = FindClosestWallPoint(_bodyCenter, 90, 1) - _bodyCenter.position;
            }
            SlerpRotation(_model, _wallDir, _rotationSpeed);
            // movement
            _movement = new Vector3(inputs.x, inputs.z, 0) * _climbSpeed;
            if (_charaCtrl.isGrounded && inputs.z < 0) {
                _movement = new Vector3(inputs.x, -_terminalVelocity, inputs.z) * Speed;
            }
            motion = _model.TransformDirection(_movement);
        } else {
            Vector3 flatInputs = new Vector3(inputs.x, 0, inputs.z);
            if (flatInputs.magnitude > 0) SlerpRotation(_model, transform.TransformDirection(flatInputs), _rotationSpeed);
            // horizontal movements
            _movement = new Vector3(inputs.x * Speed, _movement.y, inputs.z * Speed);
            if(_charaCtrl.isGrounded) {
                _movement.y = inputs.y > 0 ? inputs.y : -0.5f;
            }
            _movement.y = Mathf.Clamp(_movement.y - 9.81f * Time.deltaTime, -_terminalVelocity, _terminalVelocity);
            motion = transform.TransformDirection(_movement);
        }
        //_movement.y += _charaCtrl.isGrounded ? 0 : -9.81f * Time.deltaTime;

        _charaCtrl.Move(motion * Time.deltaTime);
        Debug.Log(_charaCtrl.velocity);
    }
    void Look() {
        Vector2 inputs = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        _playerCam.RotateHorizontal(inputs.x * _camRotateSpeed);
        _playerCam.RotateVertical(inputs.y * _camRotateSpeed);
        /*/ apply camera rotation to character
        if (!_isOnClimbWall) {
            transform.localRotation = _playerCam.transform.localRotation;
            _playerCam.transform.localRotation = Quaternion.identity;
        }*/
    }
    bool IsClimbWallForward() {
        RaycastHit hit;
        Vector3 rayOrigin = _bodyCenter.position;
        Vector3 rayDir =  _model.TransformDirection(Vector3.forward) * Radius * 1.5f;
        int layerMaskClimbZone = 1 << LayerMask.NameToLayer("Environment");
        int layerMaskWall = 1 << LayerMask.NameToLayer("Terrain");
        // Find if in front of wall
        if (Physics.Raycast(rayOrigin, rayDir.normalized, out hit, rayDir.magnitude, layerMaskWall)) {
            if((hit.point - rayOrigin).magnitude > Radius) {
                Debug.DrawLine(rayOrigin, hit.point, Color.blue);
                return false;
            }
            Debug.DrawLine(rayOrigin, hit.point, Color.cyan);
            // Find if wall is climbZone
            if(Physics.Raycast(rayOrigin, rayDir.normalized, rayDir.magnitude, layerMaskClimbZone)) {
                return true;
            }
            return false;
        }
        Debug.DrawRay(rayOrigin, rayDir, Color.blue);
        return false;
    }

    bool IsClimbFloorForwardDown() {
        RaycastHit hit;
        Vector3 rayOrigin = _bodyCenter.position + _model.TransformDirection(Vector3.forward) * _climbWallDistance * 1.5f;
        Vector3 rayDir = _model.TransformDirection(Vector3.down) * _bodyCenter.localPosition.y * 1.5f;
        int layerMask = 1 << LayerMask.NameToLayer("Terrain");
        if(Physics.Raycast(rayOrigin, rayDir.normalized, out hit, rayDir.magnitude, layerMask)) {
            if ((hit.point - rayOrigin).magnitude < _bodyCenter.localPosition.y) {
                Debug.DrawLine(rayOrigin, hit.point, Color.red);
                return false;
            }
            Debug.DrawLine(rayOrigin, hit.point, Color.magenta);
            return true;
        }
        Debug.DrawRay(rayOrigin, rayDir, Color.red);
        return false;
    }
    /// <summary>
    /// Return inputs as such (x: horizontal, y: jumpStrength, z: vertical).
    /// </summary>
    /// <returns></returns>
    Vector3 GetInputs() {
        Vector2 inputs = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        inputs = Utils.CartToPolar(inputs);
        return new Vector3(inputs.x, Input.GetKeyDown(KeyCode.Space) ? _jumpHeight : 0, inputs.y);
    }
    void LerpMovement(Vector3 targetMovement, bool includeY = false) {
        if(!includeY) targetMovement.y = _movement.y;
        float deltaSpeed = Speed * Time.deltaTime / _lerpMoveTime;
        if((targetMovement - _movement).magnitude != 0) {
            float t = deltaSpeed / (targetMovement - _movement).magnitude;
            _movement = Vector3.Lerp(_movement, targetMovement, t);
        }
    }
    bool SlerpRotation(Transform obj, Vector3 newDirection, float rotateSpeed) {
        float vectorAngle = Vector3.Angle(obj.TransformDirection(Vector3.forward), newDirection);
        Quaternion newRotation = Quaternion.LookRotation(newDirection, obj.TransformDirection(Vector3.up));
        float t = rotateSpeed * Time.deltaTime / vectorAngle;
        obj.rotation = Quaternion.Slerp(obj.rotation, newRotation, t);
        return t >= 1;
    }
    Vector3 FindClosestWallPoint(Transform origin, float maxAngle = 90, float stepAngle = 1) {
        Vector3 centerDir = transform.TransformDirection(Vector3.forward) * _climbWallDistance;
        Vector3 hitPoint = origin.position + centerDir * 1.5f;
        int layerMask = 1 << LayerMask.NameToLayer("Terrain");
        for(float i = -maxAngle; i <= maxAngle; i += stepAngle) {
            Quaternion iRot = Quaternion.Euler(0, i, 0);
            if(Physics.Raycast(origin.position, iRot * centerDir.normalized, out RaycastHit hit, (hitPoint - origin.position).magnitude, layerMask)) {
                //Debug.DrawLine(origin.position, hit.point, Color.red, 5);
                hitPoint = hit.point;
            }
        }
        return hitPoint;
    }
}